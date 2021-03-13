using System.Diagnostics;
using Serilog.Core;
using Serilog.Events;

namespace Honeycomb.Serilog.Sink.Enricher
{
    public class ActivityEnricher : ILogEventEnricher
    {
        /// <inheritdoc/>
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            var current = Activity.Current;
            if (current is not null)
            {
                logEvent.AddPropertyIfAbsent(new LogEventProperty("trace.parent_id", new ScalarValue(current.GetSpanId())));
                logEvent.AddPropertyIfAbsent(new LogEventProperty("trace.trace_id", new ScalarValue(current.GetTraceId())));
            }
        }
    }
}
