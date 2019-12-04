using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;

namespace CosmosData.Tests
{
    public static class InsightsHelper
    {
        public static TelemetryClient InitializeTelemetryClient(string iKey)
        {
            var telemetryConfig = TelemetryConfiguration.CreateDefault();
            telemetryConfig.InstrumentationKey = iKey;

            return new TelemetryClient(telemetryConfig);
        }
    }
}
