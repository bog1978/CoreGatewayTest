namespace Rebus.Config;

public class JetStreamTransportOptions
{
    public string InputQueueName { get; internal set; }

    internal bool IsOneWayClient => InputQueueName == null;
}