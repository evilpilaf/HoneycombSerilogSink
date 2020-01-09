using System;
using System.Net.Http;

namespace Honeycomb.Serilog.Sink.Tests
{
    internal class HoneycombSerilogSinkStub : HoneycombSerilogSink
    {
        private readonly HttpClient _client;

        public HoneycombSerilogSinkStub(HttpClient client, string teamId, string apiKey)
            : base(teamId, apiKey, 1, TimeSpan.FromMilliseconds(1))
        {
            _client = client;
        }

        protected override HttpClient Client => _client;
    }
}
