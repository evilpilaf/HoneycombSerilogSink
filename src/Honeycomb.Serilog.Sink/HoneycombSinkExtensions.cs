using System;
using System.Net.Http;

using Honeycomb.Serilog.Sink.Enricher;
using Honeycomb.Serilog.Sink.Sink;

using Serilog;
using Serilog.Configuration;
using Serilog.Sinks.PeriodicBatching;

namespace Honeycomb.Serilog.Sink
{
    public static class HoneycombSinkExtensions
    {
        private const string DefaultHoneycombUri = "https://api.honeycomb.io/";

        /// <param name="loggerConfiguration"></param>
        /// <param name="dataset">The name of the dataset where to send the events to</param>
        /// <param name="apiKey">The API key given in the Honeycomb ui</param>
        /// <param name="batchSizeLimit">The maximum number of events to include in a single batch.</param>
        /// <param name="period">The time to wait between checking for event batches.</param>
        /// <param name="httpClientFactory"></param>
        /// <param name="honeycombUrl"></param>
        /// <summary>See the official Honeycomb <a href="https://docs.honeycomb.io/api/events/">documentation</a> for more details.</summary>
        public static LoggerConfiguration HoneycombSink(this LoggerSinkConfiguration loggerConfiguration,
                                                        string dataset,
                                                        string apiKey,
                                                        int batchSizeLimit,
                                                        TimeSpan period,
                                                        Func<HttpClient>? httpClientFactory = null,
                                                        string honeycombUrl = DefaultHoneycombUri)
        {
            var batchingOptions = new PeriodicBatchingSinkOptions
            {
                BatchSizeLimit = batchSizeLimit,
                Period = period
            };
            return loggerConfiguration.HoneycombSink(dataset,
                                                     apiKey,
                                                     batchingOptions,
                                                     httpClientFactory,
                                                     honeycombUrl);
        }

        public static LoggerConfiguration HoneycombSink(this LoggerSinkConfiguration loggerConfiguration,
                                                        string teamId,
                                                        string apiKey,
                                                        PeriodicBatchingSinkOptions? batchingOptions = default,
                                                        Func<HttpClient>? httpClientFactory = null,
                                                        string honeycombUrl = DefaultHoneycombUri)
        {
            var honeycombSink = new HoneycombSerilogSink(teamId, apiKey, httpClientFactory, honeycombUrl);

            var batchingSink = new PeriodicBatchingSink(honeycombSink, batchingOptions ?? new PeriodicBatchingSinkOptions());

            return loggerConfiguration.Sink(batchingSink).Enrich.WithActivity();
        }
    }
}
