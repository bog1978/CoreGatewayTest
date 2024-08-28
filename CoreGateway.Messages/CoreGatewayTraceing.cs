using System.Diagnostics;
using System.Diagnostics.Metrics;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace CoreGateway.Messages
{
    public static class CoreGatewayTraceing
    {
        public static readonly ActivitySource CoreGatewayActivity = new(nameof(CoreGatewayActivity), "1.0");
        public static readonly Meter CoreGatewayMetrics = new(nameof(CoreGatewayMetrics), "1.0");
        public static readonly Counter<long> StoredDataCounter = CoreGatewayMetrics.CreateCounter<long>(nameof(StoredDataCounter));
        public static readonly Counter<long> StoredDataSizeTotal = CoreGatewayMetrics.CreateCounter<long>(nameof(StoredDataSizeTotal));
        public static readonly Histogram<long> StoredDataSize = CoreGatewayMetrics.CreateHistogram<long>(nameof(StoredDataSize));

        public static TracerProviderBuilder AddCoreGatewayInstrumentation(this TracerProviderBuilder builder) =>
            builder != null
                ? builder.AddSource(CoreGatewayActivity.Name)
                : throw new ArgumentNullException(nameof(builder));

        public static MeterProviderBuilder AddCoreGatewayInstrumentation(this MeterProviderBuilder builder) =>
            builder != null
                ? builder.AddMeter(CoreGatewayMetrics.Name)
                : throw new ArgumentNullException(nameof(builder));
    }
}
