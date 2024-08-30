using Rebus.Config;
using Rebus.Transport;

namespace CoreGateway.Messages
{
    public static class RebusExt
    {
        public static void ConfigureRebusTransport(this StandardConfigurer<ITransport> transport, BaseRebusOptions dispatcherOptions)
        {
            if (dispatcherOptions.TransportConnectionString.StartsWith("amqp:"))
                transport
                    .UseRabbitMq(dispatcherOptions.TransportConnectionString, dispatcherOptions.InputQueueName)
                    .PriorityQueue(5);
            else if (dispatcherOptions.TransportConnectionString.StartsWith("nats:"))
                transport
                    .UseJetStream(dispatcherOptions.TransportConnectionString, dispatcherOptions.InputQueueName);
            else
                throw new InvalidOperationException("Неизвестный тип строки подключения.");
        }
    }
}