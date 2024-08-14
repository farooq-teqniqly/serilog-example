namespace BackgroundTaskApp
{
    using System.Net;
    using System.Net.Http.Headers;
    using Microsoft.Extensions.Options;
    using Serilog;
    using Serilog.Context;

    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Host.UseSerilog((context, config) => config
                    .WriteTo
                    .Console()
                    .ReadFrom
                    .Configuration(context.Configuration));

            builder.Services.Configure<ExampleBackgroundServiceOptions>(
                builder.Configuration.GetSection(ExampleBackgroundServiceOptions.ExampleBackgroundService));

            builder.Services.AddHttpClient(
                "ado-http-client",
                c =>
                {
                    c.BaseAddress = new Uri(builder.Configuration["AzureDevOps:BaseUrl"]!);

                    var pat = builder.Configuration["AzureDevOps:Pat"]!;

                    var authToken = ":" + pat;
                    var base64Token = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(authToken));

                    c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                        "Basic",
                        base64Token);
                });

            builder.Services.AddHostedService<ExampleBackgroundService>();

            var app = builder.Build();

            app.Run();
        }
    }

    public class ExampleBackgroundService : BackgroundService
    {
        private readonly ExampleBackgroundServiceOptions _options;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ExampleBackgroundService> _logger;

        public ExampleBackgroundService(
            IOptions<ExampleBackgroundServiceOptions> options,
            IHttpClientFactory httpClientFactory,
            ILogger<ExampleBackgroundService> logger)
        {
            _options = options.Value;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (LogContext.PushProperty("ExecutionId", Guid.NewGuid()))
                {
                    var httpClient = _httpClientFactory.CreateClient("ado-http-client");
                    var organizationName = "ablcode";
                    var uri = $"{organizationName}/_apis/projects?api-version=7.1-preview.1";

                    using (httpClient)
                    {
                        _logger.LogInformation(
                            "Pulling Azure Dev Ops projects for {Organization}",
                            organizationName);

                        var response = await httpClient.GetAsync(uri, stoppingToken);
                        var contentLength = response.Content.Headers.ContentLength;

                        var shouldShutDown = false;

                        switch (response.StatusCode)
                        {
                            case HttpStatusCode.NonAuthoritativeInformation:
                                _logger.LogCritical("PAT might be misconfigured.");
                                shouldShutDown = true;
                                break;
                            case HttpStatusCode.NotFound:
                                _logger.LogCritical("A URL might be misconfigured.");
                                shouldShutDown = true;
                                break;
                        }

                        if (shouldShutDown)
                        {
                            throw new Exception("Shutting down due to critical error.");
                        }

                        _logger.LogInformation(
                            "Pulled Azure Dev Ops projects for {Organization}. ({ByteCount} bytes)",
                            organizationName,
                            contentLength);

                        _logger.LogDebug(
                            "Waiting for {PollingInterval} seconds.",
                            _options.PollingIntervalSeconds);

                        await Task.Delay(
                            TimeSpan.FromSeconds(_options.PollingIntervalSeconds),
                            stoppingToken);
                    }
                }
            }
        }
    }

    public class ExampleBackgroundServiceOptions
    {
        public const string ExampleBackgroundService = "ExampleBackgroundService";

        public int PollingIntervalSeconds { get; set; } = 60;
    }
}
