using System;

using Serilog;
using Serilog.Configuration;
using Serilog.Sinks.PeriodicBatching;

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
            var batchingOptions = new PeriodicBatchingSinkOptions
            {
                BatchSizeLimit = batchSizeLimit,
                Period = period
            };

            return loggerConfiguration.HoneycombSink(teamId, apiKey, batchingOptions);
        }

        public static LoggerConfiguration HoneycombSink(this LoggerSinkConfiguration loggerConfiguration,
                                                        string teamId,
                                                        string apiKey,
                                                        PeriodicBatchingSinkOptions batchingOptions)
        {
            var honeycombSink = new HoneycombSerilogSink(teamId, apiKey);

            var batchingSink = new PeriodicBatchingSink(honeycombSink, batchingOptions);

            return loggerConfiguration.Sink(batchingSink);
        }
    }
}
