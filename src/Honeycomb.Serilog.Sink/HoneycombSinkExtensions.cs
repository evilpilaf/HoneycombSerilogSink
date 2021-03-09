using System;

using Serilog;
using Serilog.Configuration;
using Serilog.Sinks.PeriodicBatching;

namespace Honeycomb.Serilog.Sink
{
    public static class HoneycombSinkExtensions
    {
        /// <param name="loggerConfiguration"></param>
        /// <param name="dataset">The name of the dataset where to send the events to</param>
        /// <param name="apiKey">The API key given in the Honeycomb ui</param>
        /// <param name="batchSizeLimit">The maximum number of events to include in a single batch.</param>
        /// <param name="period">The time to wait between checking for event batches.</param>
        /// <summary>See the official Honeycomb <a href="https://docs.honeycomb.io/api/events/">documentation</a> for more details.</summary>
        public static LoggerConfiguration HoneycombSink(this LoggerSinkConfiguration loggerConfiguration,
                                                        string dataset,
                                                        string apiKey,
                                                        int batchSizeLimit,
                                                        TimeSpan period)
        {
            var batchingOptions = new PeriodicBatchingSinkOptions
            {
                BatchSizeLimit = batchSizeLimit,
                Period = period
            };

            return loggerConfiguration.HoneycombSink(dataset, apiKey, batchingOptions);
        }

        public static LoggerConfiguration HoneycombSink(this LoggerSinkConfiguration loggerConfiguration,
                                                        string teamId,
                                                        string apiKey,
                                                        PeriodicBatchingSinkOptions? batchingOptions = default)
        {
            var honeycombSink = new HoneycombSerilogSink(teamId, apiKey);

            var batchingSink = new PeriodicBatchingSink(honeycombSink, batchingOptions ?? new PeriodicBatchingSinkOptions());

            return loggerConfiguration.Sink(batchingSink);
        }
    }
}
