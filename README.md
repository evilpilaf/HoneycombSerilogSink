# Honeycomb Serilog sink

[![Build Status](https://dev.azure.com/evilpilaf/Honeycomb%20Serilog%20sink/_apis/build/status/evilpilaf.HoneycombSerilogSink?branchName=master)](https://dev.azure.com/evilpilaf/Honeycomb%20Serilog%20sink/_build/latest?definitionId=5&branchName=master) [![Nuget](https://img.shields.io/nuget/vpre/Honeycomb.Serilog.Sink)](<(https://www.nuget.org/packages/Honeycomb.Serilog.Sink)>) ![Azure DevOps coverage](https://img.shields.io/azure-devops/coverage/evilpilaf/Honeycomb%20Serilog%20sink/5)

[![BuildHistory](https://buildstats.info/azurepipelines/chart/evilpilaf/Honeycomb%20Serilog%20sink/5)]

This project aims to provide a Serilog sink to push structured log events to the [Honeycomb](https://www.honeycomb.io/) platform for observability and monitoring purposes.

By hooking up to serilog the goal is to allow all existing applications which already produce structured events for logging to easily include Honeycomb as part of their pipeline.

This library will add an enricher that adds information about the ongoing [Activity/Span](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/src/OpenTelemetry.Api/README.md#introduction-to-opentelemetry-net-tracing-api).The moment the log message's created.
This adds a `trace.trace_id` property that matches the activities `TraceId` and a `trace.parent_id` property which matches the `SpanId` of the activity to each log event.
Every event will be tagged with `meta.annotation_type=span_event` in Honeycomb and you'll be able to see them when reviewing a trace.

## Setup

To start using this sink simply download the package from [Nuget](https://www.nuget.org/packages/Honeycomb.Serilog.Sink/) and add it to your Serilog configuration as another sink in the pipeline.

### Parameters

#### Mandatory

- dataset: The name of the dataset to send the log messages to.
- api key: An API key with `Send Events` permissions on the dataset.

#### Optional

- httpClientFactory: a factory which will provide an instance of HttpClient. When passed it's the responsability of the caller to manage the lifecycle of the client.
- honeycombUrl: the url to the honeycomb Events API, change it if you want to test or if using [Refinery](https://docs.honeycomb.io/manage-data-volume/refinery/). It defaults to _https://api.honeycomb.io_
- Batching Option:
  - batchSizeLimit: The maximum number of log events to send in a batch. Defaults to 1000.
  - period: The maximum amount of time before flushing the events. Defaults to 2 seconds.
 If you see issues with memory utilization troubleshoot the batching options, too big a batch size limmit might result in a lot of memory being used, too low numbers may result in too frequent calls to the API.

### Download the package

```powershell
 dotnet add package Honeycomb.Serilog.Sink
```

#### Example

```csharp
using Honeycomb.Serilog.Sink;

namespace Example
{
    public static class Program
    {
        public static int Main(string[] args)
        {
          Activity.DefaultIdFormat = ActivityIdFormat.W3C;
          Activity.ForceDefaultIdFormat = true;

          Log.Logger = new LoggerConfiguration()
                          .WriteTo.HoneycombSink(
                              teamId: dataset,
                              apiKey: apiKey)
                          .BuildLogger();

          // Do stuff
        }
    }
}
```

#### Using service provider

```csharp
namespace Example
{
  public static class Program
  {
    public static int Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateBootstrapLogger();

        try
        {
            Log.Information("Starting web host");
            CreateHostBuilder(args).Build().Run();
            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Host terminated unexpectedly");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    public static IHostBuilder CreateWebHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .UseSerilog((_, services, configuration) =>
            {
                configuration.WriteTo.HoneycombSink(
                    teamId: <Dataset>,
                    apiKey: <Api Key>,
                    httpClientFactory: () =>
                        services.GetRequiredService<IHttpClientFactory>()
                                .CreateClient("honeycomb"));
            });
    }
  }
}

```
