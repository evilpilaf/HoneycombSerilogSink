using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using Honeycomb.Serilog.Sink.Formatters;

using Serilog.Debugging;
using Serilog.Events;
using Serilog.Sinks.PeriodicBatching;

namespace Honeycomb.Serilog.Sink
{
    internal class HoneycombSerilogSink : IBatchedLogEventSink
    {
#if NETCOREAPP
        private static readonly SocketsHttpHandler _socketsHttpHandler = new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(30) };
        protected virtual HttpClient Client => BuildHttpClient();
#else
        private static readonly Lazy<HttpClient> _clientBuilder = new Lazy<HttpClient>(BuildHttpClient);
        protected virtual HttpClient Client => _clientBuilder.Value;
#endif
        private readonly string _apiKey;
        private readonly string _teamId;
        private static readonly Uri _honeycombApiUrl = new Uri("https://api.honeycomb.io/");

        /// <param name="teamId">The name of the team to submit the events to</param>
        /// <param name="apiKey">The API key given in the Honeycomb ui</param>
        /// <param name="batchSizeLimit">The maximum number of events to include in a single batch.</param>
        /// <param name="period">The time to wait between checking for event batches.</param>
        public HoneycombSerilogSink(string teamId, string apiKey)
        {
            _teamId = string.IsNullOrWhiteSpace(teamId) ? throw new ArgumentNullException(nameof(teamId)) : teamId;
            _apiKey = string.IsNullOrWhiteSpace(apiKey) ? throw new ArgumentNullException(nameof(apiKey)) : apiKey;
        }

        public async Task EmitBatchAsync(IEnumerable<LogEvent> events)
        {
            using (TextWriter writer = new StringWriter())
            {
                BuildLogEvent(events, writer);
                await SendBatchedEvents(writer.ToString());
            }
        }

        private async Task SendBatchedEvents(string events)
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"/1/batch/{_teamId}")
            {
                Content = new StringContent(events, Encoding.UTF8, "application/json"),
                Version = new Version(2, 0)
            };

            requestMessage.Headers.Add("X-Honeycomb-Team", _apiKey);
            var response = await SendRequest(requestMessage).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                using (Stream contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                using (var reader = new StreamReader(contentStream))
                {
                    var responseContent = await reader.ReadToEndAsync().ConfigureAwait(false);
                    SelfLog.WriteLine("Failure sending event to Honeycomb, received {statusCode} response with content {content}", response.StatusCode, responseContent);
                }
            }
        }

        private async Task<HttpResponseMessage> SendRequest(HttpRequestMessage request)
        {
#if NETCOREAPP
            using (var client = Client)
            {
                return await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            }
#else
            return await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
#endif
        }

        private static void BuildLogEvent(IEnumerable<LogEvent> logEvents, TextWriter payload)
        {
            payload.Write("[");
            var eventSeparator = "";
            foreach (var evnt in logEvents)
            {
                payload.Write(eventSeparator);
                eventSeparator = ",";
                RawJsonFormatter.FormatContent(evnt, payload);
            }
            payload.Write("]");
        }

        private static HttpClient BuildHttpClient()
        {
            HttpClient client;
#if NETCOREAPP
            client = new HttpClient(_socketsHttpHandler, disposeHandler: false);
#else
            client = new HttpClient();
#endif
            client.BaseAddress = _honeycombApiUrl;

            return client;
        }

        public Task OnEmptyBatchAsync()
        {
            return Task.CompletedTask;
        }
    }
}
