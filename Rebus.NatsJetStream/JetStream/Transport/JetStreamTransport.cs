﻿using System.Text;
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

        var natsOpts = new NatsOpts()
        {
            Url = options.Url ?? throw new NullReferenceException("Свойство Url не может быть null."),
            HeaderEncoding = Encoding.UTF8,
            SubjectEncoding = Encoding.UTF8

        };
        _natsConnection = new NatsConnection(natsOpts);
        _natsJsContext = new NatsJSContext(_natsConnection);
        _rebusTime = rebusTime ?? throw new ArgumentNullException(nameof(rebusTime));
        _log = rebusLoggerFactory.GetLogger<JetStreamTransport>();
    }

    protected override async Task SendOutgoingMessages(IEnumerable<OutgoingTransportMessage> outgoingMessages, ITransactionContext context)
    {
        foreach (var msg in outgoingMessages)
            await InternalSend(msg.DestinationAddress, msg.TransportMessage, context);
    }

    public void Initialize()
    {
        CreateQueue(Address);
    }

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
        var ack = await _natsJsContext.PublishAsync(
            Normalize(destinationAddress) + ".new",
            message.Body,
            headers: headers);
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
            var msg = await consumer.NextAsync<byte[]>(cancellationToken: cancellationToken);
            if (msg is { } m)
            {
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
        }

        return tm;
    }

    private async Task<INatsJSConsumer> GetConsumer()
    {
        if (_consumer == null)
        {
            var config = new ConsumerConfig(Normalize(Address));
            _consumer = await _natsJsContext.CreateOrUpdateConsumerAsync(Normalize(Address), config);
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
            _natsConnection.DisposeAsync().GetAwaiter().GetResult();
        }
        finally
        {
            _disposed = true;
        }
    }

    private static string Normalize(string queue) => queue.Replace('.', '_');
}