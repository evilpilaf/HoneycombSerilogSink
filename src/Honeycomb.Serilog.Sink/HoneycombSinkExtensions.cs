using Serilog;
using Serilog.Configuration;

namespace Honeycomb.Serilog.Sink
{
    public static class HoneycombSinkExtensions
    {
        public static LoggerConfiguration HoneycombSink(this LoggerSinkConfiguration loggerConfiguration,
                                                        string teamId,
                                                        string apiKey)
        {
            return loggerConfiguration.Sink(new HoneycombSerilogSink(teamId, apiKey));
        }
    }
}
