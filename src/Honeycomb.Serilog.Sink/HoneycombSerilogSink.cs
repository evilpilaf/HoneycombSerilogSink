using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using Honeycomb.Serilog.Sink.Formatters;

using Serilog.Events;
using Serilog.Sinks.PeriodicBatching;

namespace Honeycomb.Serilog.Sink
{
    internal class HoneycombSerilogSink : PeriodicBatchingSink
    {
        private static readonly Uri _honeycombApiUrl = new Uri("https://api.honeycomb.io/");

        private readonly string _teamId;
        private readonly string _apiKey;

        private readonly Lazy<HttpClient> _clientBuilder = new Lazy<HttpClient>(BuildHttpClient);
        protected virtual HttpClient Client => _clientBuilder.Value;

        /// <param name="teamId">The name of the team to submit the events to</param>
        /// <param name="apiKey">The API key given in the Honeycomb ui</param>
        /// <param name="batchSizeLimit">The maximum number of events to include in a single batch.</param>
        /// <param name="period">The time to wait between checking for event batches.</param>
        public HoneycombSerilogSink(
            string teamId,
            string apiKey,
            int batchSizeLimit,
            TimeSpan period)
             : base(batchSizeLimit, period)
        {
            _teamId = string.IsNullOrWhiteSpace(teamId) ? throw new ArgumentNullException(nameof(teamId)) : teamId;
            _apiKey = string.IsNullOrWhiteSpace(apiKey) ? throw new ArgumentNullException(nameof(apiKey)) : apiKey;
        }

        protected override async Task EmitBatchAsync(IEnumerable<LogEvent> events)
        {
            using (TextWriter writer = new StringWriter())
            {
                BuildLogEvent(events, writer);
                await SendBatchedEvents(writer.ToString());
            }
        }

        private async Task SendBatchedEvents(string events)
        {
            var message = new HttpRequestMessage(HttpMethod.Post, $"/1/batch/{_teamId}")
            {
                Content = new StringContent(events, Encoding.UTF8, "application/json")
            };
            message.Headers.Add("X-Honeycomb-Team", _apiKey);
            var result = await Client.SendAsync(message).ConfigureAwait(false);
            var response = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
        }

        private static void BuildLogEvent(IEnumerable<LogEvent> logEvents, TextWriter payload)
        {
            payload.Write("[");
            var eventSepparator = "";
            foreach (var evnt in logEvents)
            {
                payload.Write(eventSepparator);
                eventSepparator = ",";
                RawJsonFormatter.FormatContent(evnt, payload);
            }
            payload.Write("]");
        }

        private static HttpClient BuildHttpClient()
        {
            var client = new HttpClient
            {
                BaseAddress = _honeycombApiUrl
            };
            return client;
        }
    }
}
