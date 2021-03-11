namespace Serilog.Enricher.ActivityEnricher
{
    using System;
    using Serilog.Configuration;


    /// <summary>
    /// <see cref="LoggerEnrichmentConfiguration"/> extension methods.
    /// </summary>
    public static class LoggerEnrichmentConfigurationExtensions
    {
        public static LoggerConfiguration WithActivity(this LoggerEnrichmentConfiguration self)
        {
            if (self is null)
            {
                throw new ArgumentNullException(nameof(self));
            }
            return self.With<ActivityEnricher>();
        }
    }
}
