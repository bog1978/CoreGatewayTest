using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Threading;
using Rebus.Time;
using Rebus.Transport;

namespace Rebus.JetStream.Transport;

public class JetStreamTransport : AbstractRebusTransport, IInitializable, IDisposable
{
    private readonly NatsConnection _natsConnection;
    private readonly NatsJSContext _natsJsContext;
    private readonly IRebusTime _rebusTime;
    private readonly ILog _log;
    private INatsJSConsumer? _consumer;
    private IAsyncEnumerable<NatsJSMsg<byte[]>>? _msgStream;
    private bool _disposed;

    public JetStreamTransport(IRebusLoggerFactory rebusLoggerFactory, IAsyncTaskFactory asyncTaskFactory, IRebusTime rebusTime, string inputQueueName, JetStreamTransportOptions options)
        : base(inputQueueName)
    {
        if (rebusLoggerFactory == null)
            throw new ArgumentNullException(nameof(rebusLoggerFactory));
        if (asyncTaskFactory == null)
            throw new ArgumentNullException(nameof(asyncTaskFactory));
        _natsConnection = new NatsConnection();
        _natsJsContext = new NatsJSContext(_natsConnection);
        _rebusTime = rebusTime ?? throw new ArgumentNullException(nameof(rebusTime));
        _log = rebusLoggerFactory.GetLogger<JetStreamTransport>();
        Address = Normalize(inputQueueName);
    }

    protected override async Task SendOutgoingMessages(IEnumerable<OutgoingTransportMessage> outgoingMessages, ITransactionContext context)
    {
        foreach(var msg in outgoingMessages)
            await InternalSend(msg.DestinationAddress, msg.TransportMessage, context);
    }

    public void Initialize()
    {
        CreateQueue(Address);
    }

    public string Address { get; }

    public override void CreateQueue(string address)
    {
        if (address == null)
            return;

        AsyncHelpers.RunSync(() => EnsureJetStreamIsCreatedAsync(Normalize(address)));
    }

    private async Task EnsureJetStreamIsCreatedAsync(string address)
    {
        var config = new StreamConfig(address, new[] { $"{address}.*" })
        {
            Retention = StreamConfigRetention.Workqueue,
            Storage = StreamConfigStorage.File,
        };
        await _natsJsContext.CreateStreamAsync(config);
    }

    private async Task InternalSend(string destinationAddress, TransportMessage message, ITransactionContext context)
    {
        var headers = new NatsHeaders();
        foreach (var h in message.Headers)
            headers.Add(h.Key, h.Value);
        var ack = await _natsJsContext.PublishAsync(Normalize(destinationAddress) + ".new", message.Body, headers: headers);
        context.OnCommit(ctx => Task.Run(ack.EnsureSuccess));
        context.OnAck(ctx => Task.CompletedTask);
        context.OnNack(ctx => Task.CompletedTask);
        context.OnDisposed(ctx => { });
        context.OnRollback(ctx => Task.CompletedTask);
    }

    public override async Task<TransportMessage> Receive(ITransactionContext context, CancellationToken cancellationToken)
    {
        var consumer = await GetConsumer();
        TransportMessage? tm = null;
        while (!cancellationToken.IsCancellationRequested)
        {
            var msg = await consumer.NextAsync<byte[]>();
            if (msg is { } m)
            {
                _log.Warn($"Сообщение получено.");
                var headers = new Dictionary<string, string>();
                if (m.Headers != null)
                    foreach (var h in m.Headers)
                        headers.Add(h.Key, h.Value);
                tm = new TransportMessage(headers, m.Data);
                context.OnCommit(ctx => Task.CompletedTask);
                context.OnAck(ctx => m.AckAsync().AsTask());
                context.OnNack(ctx => Task.CompletedTask);
                context.OnDisposed(ctx => { });
                context.OnRollback(ctx => Task.CompletedTask);
                break;
            }
            else
            {
                _log.Warn($"Сообщение не получено.");
            }
        }

        return tm;
    }

    private async Task<INatsJSConsumer> GetConsumer()
    {
        if (_consumer == null)
        {
            var config = new ConsumerConfig(Address);
            _consumer = await _natsJsContext.CreateOrUpdateConsumerAsync(Address, config);
            return _consumer;
        }
        return _consumer;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        try
        {
        }
        finally
        {
            _disposed = true;
        }
    }

    private string Normalize(string queue) => queue.Replace('.', '_');
}