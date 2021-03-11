using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Json;

namespace Honeycomb.Serilog.Sink.Formatters
{
    internal class RawJsonFormatter : ITextFormatter
    {
        private static readonly JsonValueFormatter ValueFormatter = new();
        private static readonly IReadOnlyDictionary<string, string> PropertyTransforms = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            {"SpanId", "trace.parent_id"},
            {"TraceId", "trace.trace_id"}
        };

        public void Format(LogEvent logEvent, TextWriter output)
        {
            FormatContent(logEvent, output);
        }

        public static void FormatContent(LogEvent logEvent, TextWriter output)
        {
            if (logEvent == null)
            {
                throw new ArgumentNullException(nameof(logEvent));
            }

            if (output == null)
            {
                throw new ArgumentNullException(nameof(output));
            }

            output.Write($"{{\"time\":\"{logEvent.Timestamp:O}\",");
            output.Write("\"data\":{");
            output.Write($"\"level\":\"{logEvent.Level}\"");
            output.Write(",\"meta.annotation_type\":\"span_event\"");
            output.Write(",\"messageTemplate\":");
            JsonValueFormatter.WriteQuotedJsonString(logEvent.MessageTemplate.Text, output);
            if (logEvent.Exception != null)
            {
                output.Write(",\"exception\":");
                JsonValueFormatter.WriteQuotedJsonString(logEvent.Exception.ToString(), output);
            }

            if (logEvent.Properties.Any())
            {
                WriteProperties(logEvent.Properties, output);
            }

            output.Write('}');
            output.Write('}');
        }

        private static void WriteProperties(IReadOnlyDictionary<string, LogEventPropertyValue> properties, TextWriter output)
        {
            const string precedingDelimiter = ",";
            foreach (var property in properties.Where(p => p.Value != null && !string.IsNullOrWhiteSpace(p.Value.ToString())))
            {
                // Skip properties with empty values
                if (property.Value is ScalarValue v)
                {
                    if (v.Value == null || v.Value.ToString().Equals(""))
                    {
                        continue;
                    }
                }
                output.Write(precedingDelimiter);

                JsonValueFormatter.WriteQuotedJsonString(GetPropertyName(property.Key), output);
                output.Write(':');
                ValueFormatter.Format(property.Value, output);
            }
        }

        private static string GetPropertyName(string propertyName)
        {
            if (PropertyTransforms.TryGetValue(propertyName, out string transformedName))
            {
                return transformedName;
            }
            return propertyName;
        }
    }
}
