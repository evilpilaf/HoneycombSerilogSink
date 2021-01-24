# Honeycomb Serilog sink

[![Build Status](https://dev.azure.com/evilpilaf/Honeycomb%20Serilog%20sink/_apis/build/status/evilpilaf.HoneycombSerilogSink?branchName=master)](https://dev.azure.com/evilpilaf/Honeycomb%20Serilog%20sink/_build/latest?definitionId=5&branchName=master) [![Nuget](https://img.shields.io/nuget/vpre/Honeycomb.Serilog.Sink)](<(https://www.nuget.org/packages/Honeycomb.Serilog.Sink)>) ![Azure DevOps coverage](https://img.shields.io/azure-devops/coverage/evilpilaf/Honeycomb%20Serilog%20sink/5)

[![BuildHistory](https://buildstats.info/azurepipelines/chart/evilpilaf/Honeycomb%20Serilog%20sink/5)]

This project aims to provide a Serilog sink to push structured log events to the [Honeycomb](https://www.honeycomb.io/) platform for observability and monitoring purposes.

By hooking up to serilog my objective is to allow all existing applications which already produce structured events for logging to easily include Honeycomb as part of their pipeline.

## Setup

To start using this sink simply download the package from [Nuget](https://www.nuget.org/packages/Honeycomb.Serilog.Sink/) and add it to your Serilog configuration as another sink in the pipeline.

### Download the package

```powershell
> dotnet add package Honeycomb.Serilog.Sink
```

```csharp
using Honeycomb.Serilog.Sink;

[...]

string dataset = "The name of the dataset wher your data will be sent";
string apiKey = "The api key given to you in Honeycomb";

var logger = new LoggerConfiguration()
                    .WriteTo
                    [...]
                    .HoneycombSink(dataset, apiKey)
                    [...]
                    .CreateLogger();
```
