using System;
using System.IO;
using System.Net.Http;
using System.Text;

using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Compact;

namespace Honeycomb.Serilog.Sink
{
    internal class HoneycombSerilogSink : ILogEventSink
    {
        private static readonly Uri _honeycombApiUrl = new Uri("https://api.honeycomb.io/1/events/");
        private readonly ITextFormatter _formatter;
        private readonly string _teamId;
        private readonly string _apiKey;

        private readonly Lazy<HttpClient> _clientBuilder = new Lazy<HttpClient>(BuildHttpClient);
        protected virtual HttpClient Client => _clientBuilder.Value;

        public HoneycombSerilogSink(string teamId, string apiKey)
        {
            _teamId = string.IsNullOrWhiteSpace(teamId) ? throw new ArgumentNullException(nameof(teamId)) : teamId;
            _apiKey = string.IsNullOrWhiteSpace(apiKey) ? throw new ArgumentNullException(nameof(apiKey)) : apiKey;
            _formatter = new CompactJsonFormatter();
        }

        public void Emit(LogEvent logEvent)
        {
            using (var buffer = new StringWriter(new StringBuilder()))
            {
                _formatter.Format(logEvent, buffer);
                var formattedLogEventText = buffer.ToString();
                var message = new HttpRequestMessage(HttpMethod.Post, $"/{_teamId}")
                {
                    Content = new StringContent(formattedLogEventText)
                };
                message.Headers.Add("'X-Honeycomb-Team", _apiKey);
                Client.SendAsync(message).ConfigureAwait(false);
            }
        }

        private static HttpClient BuildHttpClient()
        {
            var client = new HttpClient
            {
                BaseAddress = _honeycombApiUrl
            };
            client.DefaultRequestHeaders.Add("Content-Type", "application/json");
            return client;
        }
    }
}
