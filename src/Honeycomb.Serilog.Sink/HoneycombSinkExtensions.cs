using System;

using Serilog;
using Serilog.Configuration;

namespace Honeycomb.Serilog.Sink
{
    public static class HoneycombSinkExtensions
    {
        /// <param name="teamId">The name of the team to submit the events to</param>
        /// <param name="apiKey">The API key given in the Honeycomb ui</param>
        /// <param name="batchSizeLimit">The maximum number of events to include in a single batch.</param>
        /// <param name="period">The time to wait between checking for event batches.</param>
        public static LoggerConfiguration HoneycombSink(this LoggerSinkConfiguration loggerConfiguration,
                                                        string teamId,
                                                        string apiKey,
                                                        int batchSizeLimit,
                                                        TimeSpan period)
        {
            return loggerConfiguration.Sink(new HoneycombSerilogSink(teamId, apiKey, batchSizeLimit, period));
        }
    }
}
