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

namespace Honeycomb.Serilog.Sink.Sink
{
    internal class HoneycombSerilogSink : IBatchedLogEventSink, IDisposable
    {
        private static readonly Lazy<HttpMessageHandler> _messageHandler = new(() =>
#if NETCOREAPP2_0_OR_GREATER
        new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(30) }
#else
        new HttpClientHandler()
#endif
        );

        private readonly Func<HttpClient> _httpClientFactory
            = () => new HttpClient(_messageHandler.Value, disposeHandler: false);

        private readonly string _apiKey;
        private readonly string _teamId;
        private readonly Uri _honeycombApiUrl;

        private static readonly string LibraryVersion = typeof(HoneycombSerilogSink).Assembly.GetName().Version?.ToString() ?? "1.0.0.0";
        private static readonly string LibraryName = typeof(HoneycombSerilogSink).Assembly.GetName().Name ?? "Honeycomb.Serilog.Sink";

        private const string JsonContentType = "application/json";
        private const string DefaultHoneycombUri = "https://api.honeycomb.io/";
        private const string HoneycombBatchEndpointTemplate = "/1/batch/{0}";
        private const string HoneycombTeamIdHeaderName = "X-Honeycomb-Team";

        private const string SelfLogMessageText = "Failure sending event to Honeycomb, received {statusCode} response with content {content}";

        /// <param name="dataset">The name of the dataset where to send the events to</param>
        /// <param name="apiKey">The API key given in the Honeycomb ui</param>
        /// <param name="httpClientFactory">A builder to aid in creating the HttpClient</param>
        /// <param name="honeycombUrl">The URL where to send the events. Default https://api.honeycomb.io</param>
        public HoneycombSerilogSink(string? dataset, string? apiKey, Func<HttpClient>? httpClientFactory = null, string honeycombUrl = DefaultHoneycombUri)
        {
            if (dataset is not null && !string.IsNullOrWhiteSpace(dataset))
            {
                _teamId = dataset;
            }
            else
            {
                throw new ArgumentNullException(nameof(dataset));
            }
            if (apiKey is not null && !string.IsNullOrWhiteSpace(apiKey))
            {
                _apiKey = apiKey;
            }
            else
            {
                throw new ArgumentNullException(nameof(apiKey));
            }
            if (httpClientFactory is not null)
            {
                _httpClientFactory = httpClientFactory;
            }
            _honeycombApiUrl = new Uri(honeycombUrl);
        }

        public async Task EmitBatchAsync(IEnumerable<LogEvent> events)
        {
            using TextWriter writer = new StringWriter();
            BuildLogEvent(events, writer);
            await SendBatchedEvents(writer.ToString()!).ConfigureAwait(false);
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
            using var client = BuildHttpClient();
            return await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        }

        private static void BuildLogEvent(IEnumerable<LogEvent> logEvents, TextWriter payload)
        {
            payload.Write("[");
            var eventSeparator = "";
            foreach (var e in logEvents)
            {
                e.AddPropertyIfAbsent(new LogEventProperty("library.name", new ScalarValue(LibraryName)));
                e.AddPropertyIfAbsent(new LogEventProperty("library.version", new ScalarValue(LibraryVersion)));
                payload.Write(eventSeparator);
                eventSeparator = ",";
                RawJsonFormatter.FormatContent(e, payload);
            }
            payload.Write("]");
        }

        private HttpClient BuildHttpClient()
        {
            var client = _httpClientFactory();

            client.BaseAddress = _honeycombApiUrl;

            return client;
        }

        public Task OnEmptyBatchAsync()
        {
            return Task.CompletedTask;
        }

        private void ReleaseUnmanagedResources()
        {
            _messageHandler?.Value.Dispose();
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
