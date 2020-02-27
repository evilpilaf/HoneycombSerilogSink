using System.Net.Http;
using System.Threading.Tasks;

using Serilog.Events;

namespace Honeycomb.Serilog.Sink.Tests
{
    internal class HoneycombSerilogSinkStub : HoneycombSerilogSink
    {
        private readonly HttpClient _client;

        public HoneycombSerilogSinkStub(HttpClient client, string teamId, string apiKey)
            : base(teamId, apiKey)
        {
            _client = client;
        }

        protected override HttpClient Client => _client;

        public Task EmitTestable(params LogEvent[] events)
        {
            return EmitBatchAsync(events);
        }
    }
}
