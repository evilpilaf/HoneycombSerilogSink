using System.Net.Http;
using System.Threading.Tasks;

using Honeycomb.Serilog.Sink.Sink;

using Serilog.Events;

namespace Honeycomb.Serilog.Sink.Tests
{
    internal class HoneycombSerilogSinkStub : HoneycombSerilogSink
    {
        public HoneycombSerilogSinkStub(HttpClient client, string dataset, string apiKey)
            : base(dataset, apiKey, httpClientFactory: () => client)
        { }

        public HoneycombSerilogSinkStub(HttpClient client, string dataset, string apiKey, string honeycombUrl)
            : base(dataset, apiKey, httpClientFactory: () => client, honeycombUrl: honeycombUrl)
        { }

        public Task EmitTestable(params LogEvent[] events)
        {
            return EmitBatchAsync(events);
        }
    }
}
