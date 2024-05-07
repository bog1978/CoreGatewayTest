using OpenTelemetry.Trace;

namespace CoreGateway.Messages
{
    public static class TracerBuilderExtensions
    {
        public static TracerProviderBuilder AddCoreGatewayInstrumentation(this TracerProviderBuilder builder) =>
            builder != null
                ? builder.AddSource(CoreGatewayTraceing.CoreGatewayActivity.Name)
                : throw new ArgumentNullException(nameof(builder));
    }
}
