using Rebus.Injection;
using Rebus.JetStream.Transport;
using Rebus.Logging;
using Rebus.Threading;
using Rebus.Time;
using Rebus.Transport;

namespace Rebus.Config;

public static class JetStreamTransportConfigurationExtensions
{
    public static JetStreamTransportOptions UseJetStream(
        this StandardConfigurer<ITransport> configurer,
        string connectionString,
        string inputQueueName)
    {
        var transportOptions = new JetStreamTransportOptions
        {
            Url = connectionString
        };
        return Configure(
            configurer,
            (context, inputQueue) => new JetStreamTransport(
                context.Get<IRebusLoggerFactory>(),
                context.Get<IAsyncTaskFactory>(),
                context.Get<IRebusTime>(),
                inputQueue,
                transportOptions),
            transportOptions)
            .ReadFrom(inputQueueName);
    }

    public static JetStreamTransportOptions UseJetStreamAsOneWayClient(
        this StandardConfigurer<ITransport> configurer,
        string connectionString)
    {
        var transportOptions = new JetStreamTransportOptions
        {
            Url = connectionString
        };
        return Configure(
            configurer,
            (context, inputQueue) => new JetStreamTransport(
                context.Get<IRebusLoggerFactory>(),
                context.Get<IAsyncTaskFactory>(),
                context.Get<IRebusTime>(),
                inputQueue,
                transportOptions),
            transportOptions
            )
            .AsOneWayClient();
    }

    delegate JetStreamTransport TransportFactoryDelegate(IResolutionContext context, string inputQueueName);

    static TTransportOptions Configure<TTransportOptions>(StandardConfigurer<ITransport> configurer, TransportFactoryDelegate transportFactory, TTransportOptions transportOptions) where TTransportOptions : JetStreamTransportOptions
    {
        configurer.Register(
            context =>
            {
                if (transportOptions.IsOneWayClient)
                    OneWayClientBackdoor.ConfigureOneWayClient(configurer);
                return transportFactory(context, transportOptions.InputQueueName);
            }
        );

        //configurer.OtherService<Options>().Decorate(c => c.Get<Options>());

        return transportOptions;
    }
}