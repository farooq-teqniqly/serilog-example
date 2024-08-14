# Serilog Sample App

This app demonstrates using Serilog structured logging to send logs to Datadog.

The application calls the Azure DevOps REST API and downloads a list of projects in the ablcode Azure DevOps organization.

## Prerequisites

1. An Azure DevOps REST API personal access token.
2. A Datadog API key.

## Running the sample app

1. Create a [user secrets file](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-8.0&tabs=windows).

2. Add your Azure DevOps REST API personal access token and Datadog API key to the user secrets file:

```
"AzureDevOps:Pat": "your PAT",
"Serilog:WriteTo:0:Args:apiKey": "your API key"
```

3. Save and close the user secrets file.

### Change the logging metadata (optional)

If desried, change the `source`, `service`, and `tags` settings in `appsettings.json`.

4. Run the app in Debug mode.

5. After a few seconds, logs should appear in Datadog's Log Explorer. You can query by service (as specified by the `service` setting in `appsettings.json`) by running the following query:

```
service:farooq-serilog-example-app
```

## Example Datadog Dashboard

An example Datadog dashboard reporting metrics for the sample app can be found [here](https://app.datadoghq.com/dashboard/gpu-4n2-ub2?fromUser=false&refresh_mode=sliding&view=spans&from_ts=1720461606790&to_ts=1720548006790&live=true). The key here is that by virtue of Serilog's structured logging paradigm, it is easy to slice and dice the data by various dimensions.

## Polling Interval

The app calls the Azure DevOps REST API every 15 seconds. The `ExampleBackgroundService:PollingIntervalSeconds` can be changed to a longer or shorter interval if desired.
