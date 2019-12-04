using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;

using Serilog.Core;
using Serilog.Events;

namespace Honeycomb.Serilog.Sink
{
    internal class HoneycombSerilogSink : ILogEventSink
    {
        private static readonly Uri _honeycombApiUrl = new Uri("https://api.honeycomb.io/1/events/");

        private readonly string _teamId;
        private readonly string _apiKey;

        private readonly Lazy<HttpClient> _clientBuilder = new Lazy<HttpClient>(BuildHttpClient);
        protected virtual HttpClient Client => _clientBuilder.Value;

        public HoneycombSerilogSink(string teamId, string apiKey)
        {
            _teamId = string.IsNullOrWhiteSpace(teamId) ? throw new ArgumentNullException(nameof(teamId)) : teamId;
            _apiKey = string.IsNullOrWhiteSpace(apiKey) ? throw new ArgumentNullException(nameof(apiKey)) : apiKey;
        }

        public void Emit(LogEvent logEvent)
        {
            using (var buffer = new StringWriter(new StringBuilder()))
            {
                var evnt = BuildLogEvent(logEvent);
                var message = new HttpRequestMessage(HttpMethod.Post, $"/{_teamId}")
                {
                    Content = new StringContent(evnt.ToString())
                };
                message.Headers.Add("X-Honeycomb-Team", _apiKey);
                Client.SendAsync(message).ConfigureAwait(false);
            }
        }

        private static StringBuilder BuildLogEvent(LogEvent logEvent)
        {
            var evnt = new StringBuilder();
            evnt.Append("{");
            evnt.Append($"\"timestamp\": \"{logEvent.Timestamp:O}\",");
            evnt.Append($"\"level\": \"{logEvent.Level}\",");
            evnt.Append($"\"messageTemplate\": \"{logEvent.MessageTemplate}\",");
            var propertyList = new List<string>(logEvent.Properties.Count());
            foreach (var prop in logEvent.Properties)
            {
                propertyList.Add($"\"{prop.Key}\": {prop.Value.ToString()}");
            }
            evnt.Append(string.Join(",", propertyList));
            evnt.Append("}");
            return evnt;
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
