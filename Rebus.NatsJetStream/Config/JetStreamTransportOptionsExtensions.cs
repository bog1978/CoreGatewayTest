namespace Rebus.Config;

public static class JetStreamTransportOptionsExtensions
{
    public static TTransportOptions AsOneWayClient<TTransportOptions>(this TTransportOptions options) where TTransportOptions : JetStreamTransportOptions
    {
        options.InputQueueName = null;
        return options;
    }

    public static TTransportOptions ReadFrom<TTransportOptions>(this TTransportOptions options, string inputQueueName) where TTransportOptions : JetStreamTransportOptions
    {
        options.InputQueueName = inputQueueName;
        return options;
    }
}