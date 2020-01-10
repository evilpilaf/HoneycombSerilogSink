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
        private static readonly JsonValueFormatter ValueFormatter = new JsonValueFormatter();

        public void Format(LogEvent logEvent, TextWriter output)
        {
            FormatContent(logEvent, output);
        }

        public static void FormatContent(LogEvent logEvent, TextWriter output)
        {
            if (logEvent == null) throw new ArgumentNullException(nameof(logEvent));
            if (output == null) throw new ArgumentNullException(nameof(output));

            output.Write($"{{\"time\":\"{logEvent.Timestamp:O}\",");
            output.Write($"\"data\":{{");
            output.Write($"\"level\":\"{logEvent.Level}\"");
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
            var precedingDelimiter = ",";
            foreach (var property in properties)
            {
                output.Write(precedingDelimiter);

                JsonValueFormatter.WriteQuotedJsonString(property.Key, output);
                output.Write(':');
                ValueFormatter.Format(property.Value, output);
            }
        }
    }
}
