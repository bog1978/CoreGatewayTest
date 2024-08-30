using Rebus.Config;
using Rebus.Retry.Simple;
using Rebus.Routing;
using Rebus.Routing.TypeBased;
using Rebus.Transport;

namespace CoreGateway.Messages
{
    public static class RebusExt
    {
        public static void ConfigureRebusTransport(this StandardConfigurer<ITransport> transport, RebusOptions rebusOptions)
        {
            if (rebusOptions.TransportConnectionString.StartsWith("amqp:"))
                transport
                    .UseRabbitMq(rebusOptions.TransportConnectionString, rebusOptions.InputQueueName)
                    .PriorityQueue(5);
            else if (rebusOptions.TransportConnectionString.StartsWith("nats:"))
                transport
                    .UseJetStream(rebusOptions.TransportConnectionString, rebusOptions.InputQueueName);
            else
                throw new InvalidOperationException("Неизвестный тип строки подключения.");
        }

        public static void ConfigureRebusRouting(this StandardConfigurer<IRouter> router, RebusOptions rebusOptions)
        {
            var tb = router.TypeBased();
            foreach (var map in rebusOptions.Routing)
            {
                var msgType = Type.GetType(map.Key);
                tb.Map(msgType, map.Value);
            }
        }

        public static void ConfigureRebusOptions(this OptionsConfigurer options, RebusOptions rebusOptions, string serviceName)
        {
            options.SetBusName($"{serviceName}.Bus");
            options.SetMaxParallelism(rebusOptions.MaxParallelism);
            options.RetryStrategy(
                errorQueueName: $"{rebusOptions.InputQueueName}_error",
                maxDeliveryAttempts: 3);
            options.EnableDiagnosticSources();
        }
    }
}