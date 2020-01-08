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
                    Content = new StringContent(evnt, Encoding.UTF8, "application/json")
                };
                message.Headers.Add("X-Honeycomb-Team", _apiKey);
                Client.SendAsync(message).ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        private static string BuildLogEvent(LogEvent logEvent)
        {
            var evnt = new StringBuilder("{");

            var propertyList = new List<string>(logEvent.Properties.Count() + 4)
            {
                $"\"timestamp\": \"{logEvent.Timestamp:O}\"",
                $"\"level\": \"{logEvent.Level}\"",
                $"\"messageTemplate\": \"{logEvent.MessageTemplate}\""
            };

            if (logEvent.Exception != null)
            {
                propertyList.Add($"\"exception\": \"{logEvent.Exception.ToString()}\"");
            }

            foreach (var prop in logEvent.Properties)
            {
                propertyList.Add($"\"{prop.Key}\": {prop.Value.ToString()}");
            }

            evnt.Append(string.Join(",", propertyList));
            evnt.Append("}");
            return evnt.ToString();
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
