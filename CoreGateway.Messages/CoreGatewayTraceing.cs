using System.Diagnostics;

namespace CoreGateway.Messages
{
    public static class CoreGatewayTraceing
    {
        public static readonly ActivitySource CoreGatewayActivity = new("CoreGatewayActivity");
    }
}
