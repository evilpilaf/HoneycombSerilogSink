# Honeycomb Serilog sink

[![Build Status](https://dev.azure.com/evilpilaf/Honeycomb%20Serilog%20sink/_apis/build/status/evilpilaf.HoneycombSerilogSink?branchName=master)](https://dev.azure.com/evilpilaf/Honeycomb%20Serilog%20sink/_build/latest?definitionId=5&branchName=master) ![Nuget](https://img.shields.io/nuget/vpre/Honeycomb.Serilog.Sink)

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

string teamId = "The Id of your team as defined in Honeycomb";
string apiKey = "The api key given to you in Honeycomb";

var logger = new LoggerConfiguration()
                    .WriteTo
                    [...]
                    .HoneycombSink(teamId, apiKey)
                    [...]
                    .CreateLogger();
```
