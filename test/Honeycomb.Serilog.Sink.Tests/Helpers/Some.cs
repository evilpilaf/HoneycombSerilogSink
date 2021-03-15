using System;
using System.Collections.Generic;

using Serilog;
using Serilog.Events;

using Xunit.Sdk;

namespace Honeycomb.Serilog.Sink.Tests.Helpers
{
    static class Some
    {
        public static LogEvent LogEvent(string messageTemplate, params object[] propertyValues)
        {
            return LogEvent(null, messageTemplate, propertyValues);
        }

        public static LogEvent LogEvent(Exception? exception, string messageTemplate, params object[] propertyValues)
        {
            return LogEvent(LogEventLevel.Information, exception, messageTemplate, propertyValues);
        }

        public static LogEvent LogEvent(LogEventLevel level, Exception? exception, string messageTemplate, params object[] propertyValues)
        {
            var log = new LoggerConfiguration().CreateLogger();
#pragma warning disable Serilog004 // Constant MessageTemplate verifier
            if (!log.BindMessageTemplate(messageTemplate, propertyValues, out MessageTemplate template, out IEnumerable<LogEventProperty> properties))
#pragma warning restore Serilog004 // Constant MessageTemplate verifier
            {
                throw new XunitException("Template could not be bound.");
            }
            return new LogEvent(DateTimeOffset.Now, level, exception, template, properties);
        }

        public static LogEvent LogEvent(LogEventLevel level, string messageTemplate, params object[] propertyValues)
        {
            var log = new LoggerConfiguration().CreateLogger();

#pragma warning disable Serilog004 // Constant MessageTemplate verifier
            if (!log.BindMessageTemplate(messageTemplate, propertyValues, out var template, out var properties))
#pragma warning restore Serilog004 // Constant MessageTemplate verifier
            {
                throw new XunitException("Template could not be bound.");
            }
            return new LogEvent(DateTimeOffset.Now, level, null, template, properties);
        }

        public static LogEvent DebugEvent()
        {
            return LogEvent(LogEventLevel.Debug, null, "Debug event");
        }

        public static LogEvent InformationEvent()
        {
            return LogEvent(LogEventLevel.Information, null, "Information event");
        }

        public static LogEvent ErrorEvent()
        {
            return LogEvent(LogEventLevel.Error, null, "Error event");
        }

        public static string String()
        {
            return Guid.NewGuid().ToString("n");
        }
    }
}
