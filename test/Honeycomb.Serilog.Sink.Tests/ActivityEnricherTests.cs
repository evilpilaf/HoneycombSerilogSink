using System;
using System.Diagnostics;

using FluentAssertions;
using FluentAssertions.Execution;

using Honeycomb.Serilog.Sink.Enricher;
using Honeycomb.Serilog.Sink.Tests.Helpers;

using Serilog;
using Serilog.Events;

using Xunit;

namespace Honeycomb.Serilog.Sink.Tests
{
    public class ActivityEnricherTests
    {

        [Fact]
        public void ActivityEnricher_AddsParentAndTraceId()
        {
            LogEvent? evnt = null;

            var log = new LoggerConfiguration()
                .Enrich.WithActivity()
                .WriteTo.Sink(new DelegatingSink(e => evnt = e))
                .CreateLogger();

            string spanId;
            string traceId;

            Activity.DefaultIdFormat = ActivityIdFormat.W3C;

            using (var activity = new Activity(nameof(ActivityEnricher_AddsParentAndTraceId)))
            {
                activity.SetParentId(Guid.NewGuid().ToString());
                activity.Start();
                traceId = activity.GetTraceId();
                spanId = activity.GetSpanId();
                log.Information("Test message");
            }

            using (new AssertionScope())
            {
                evnt.Should().NotBeNull();
                evnt!.Properties["trace.parent_id"].LiteralValue().As<string>().Should().Be(spanId);
                evnt!.Properties["trace.trace_id"].LiteralValue().As<string>().Should().Be(traceId);
            }
        }
    }

    internal static class Extensions
    {
        public static object LiteralValue(this LogEventPropertyValue @this)
        {
            return ((ScalarValue)@this).Value;
        }
    }
}
