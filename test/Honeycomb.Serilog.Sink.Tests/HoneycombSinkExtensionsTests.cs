using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

using FluentAssertions;
using FluentAssertions.Execution;

using Honeycomb.Serilog.Sink.Enricher;
using Honeycomb.Serilog.Sink.Tests.Builders;

using Serilog;

using Xunit;

namespace Honeycomb.Serilog.Sink.Tests
{
    public class HoneycombSinkExtensionsTests
    {
        [Fact]
        public async Task HoneycombSink_AlwaysEnrichesWithActivityInfo()
        {
            const string dataset = nameof(dataset);
            const string apiKey = nameof(apiKey);

            HttpClientStub clientStub = A.HttpClient()
                                         .ThatReturnsStatusCode(HttpStatusCode.OK);

            var log = new LoggerConfiguration()
                .WriteTo.HoneycombSink(dataset,
                                       apiKey,
                                       batchSizeLimit: 1,
                                       period: TimeSpan.FromMilliseconds(1),
                                       httpClientFactory: () => clientStub)
                .CreateLogger();

            string traceId;
            string spanId;

            Activity.DefaultIdFormat = ActivityIdFormat.W3C;
            Activity.ForceDefaultIdFormat = true;

            using (var activity = new Activity(nameof(HoneycombSink_AlwaysEnrichesWithActivityInfo)))
            {
                activity.Start();
                traceId = activity.GetTraceId();
                spanId = activity.GetSpanId();
                log.Information("This is a test message");
            }

            await Task.Delay(TimeSpan.FromSeconds(1));

            var requestContent = clientStub.RequestContent!;

            using (var document = JsonDocument.Parse(requestContent))
            using (new AssertionScope())
            {
                document.RootElement.ValueKind.Should().Be(JsonValueKind.Array);
                document.RootElement.GetArrayLength().Should().Be(1);
                JsonElement sentEvent = document.RootElement.EnumerateArray().Single();

                sentEvent.GetProperty("data").ValueKind.Should().Be(JsonValueKind.Object);

                JsonElement data = sentEvent.GetProperty("data");

                data.GetProperty("trace.parent_id").ValueKind.Should().Be(JsonValueKind.String);
                data.GetProperty("trace.parent_id").GetString().Should().Be(spanId);
                data.GetProperty("trace.trace_id").ValueKind.Should().Be(JsonValueKind.String);
                data.GetProperty("trace.trace_id").GetString().Should().Be(traceId);
            }
        }
    }
}
