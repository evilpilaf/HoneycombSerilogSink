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
    internal class HoneycombSerilogSink : IBatchedLogEventSink, IDisposable
    {
#if NETCOREAPP
        private static SocketsHttpHandler? _socketsHttpHandler;

        private static SocketsHttpHandler SocketsHttpHandler
        {
            get
            {
                return _socketsHttpHandler ??= new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(30) };
            }
        }

        protected virtual HttpClient Client => BuildHttpClient();
#else
        private static readonly Lazy<HttpClient> _clientBuilder = new Lazy<HttpClient>(BuildHttpClient);
        protected virtual HttpClient Client => _clientBuilder.Value;
#endif
        private readonly string _apiKey;
        private readonly string _teamId;
        private static readonly Uri _honeycombApiUrl = new Uri(HoneycombBaseUri);

        private const string JsonContentType = "application/json";
        private const string HoneycombBaseUri = "https://api.honeycomb.io/";
        private const string HoneycombBatchEndpointTemplate = "/1/batch/{0}";
        private const string HoneycombTeamIdHeaderName = "X-Honeycomb-Team";

        private const string SelfLogMessageText = "Failure sending event to Honeycomb, received {statusCode} response with content {content}";

        /// <param name="dataset">The name of the dataset where to send the events to</param>
        /// <param name="apiKey">The API key given in the Honeycomb ui</param>
        public HoneycombSerilogSink(string dataset, string apiKey)
        {
            _teamId = string.IsNullOrWhiteSpace(dataset) ? throw new ArgumentNullException(nameof(dataset)) : dataset;
            _apiKey = string.IsNullOrWhiteSpace(apiKey) ? throw new ArgumentNullException(nameof(apiKey)) : apiKey;
        }

        public async Task EmitBatchAsync(IEnumerable<LogEvent> events)
        {
            using TextWriter writer = new StringWriter();
            BuildLogEvent(events, writer);
            await SendBatchedEvents(writer!.ToString()).ConfigureAwait(false);
        }

        private async Task SendBatchedEvents(Stream events)
        {
            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, string.Format(HoneycombBatchEndpointTemplate, _teamId))
            {
                Content = new StreamContent(events),
                Version = new Version(2, 0)
            };

            requestMessage.Headers.Add(HoneycombTeamIdHeaderName, _apiKey);
            var response = await SendRequest(requestMessage).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                using Stream contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                using var reader = new StreamReader(contentStream);
                var responseContent = await reader.ReadToEndAsync().ConfigureAwait(false);
                SelfLog.WriteLine(SelfLogMessageText, response.StatusCode, responseContent);
            }
        }

        private async Task SendBatchedEvents(string events)
        {
            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, string.Format(HoneycombBatchEndpointTemplate, _teamId))
            {
                Content = new StringContent(events, Encoding.UTF8, JsonContentType),
                Version = new Version(2, 0)
            };

            requestMessage.Headers.Add(HoneycombTeamIdHeaderName, _apiKey);
            var response = await SendRequest(requestMessage).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                using Stream contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                using var reader = new StreamReader(contentStream);
                var responseContent = await reader.ReadToEndAsync().ConfigureAwait(false);
                SelfLog.WriteLine(SelfLogMessageText, response.StatusCode, responseContent);
            }
        }

        private async Task<HttpResponseMessage> SendRequest(HttpRequestMessage request)
        {
#if NETCOREAPP
            using var client = Client;
            return await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
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
            client = new HttpClient(SocketsHttpHandler, disposeHandler: false);
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

        private void ReleaseUnmanagedResources()
        {
#if NETCORE
            _socketsHttpHandler?.Dispose();
            _socketsHttpHandler = null;
#endif
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~HoneycombSerilogSink()
        {
            ReleaseUnmanagedResources();
        }
    }
}
