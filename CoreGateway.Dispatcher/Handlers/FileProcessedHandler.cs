using CoreGateway.Dispatcher.DbModel;
using CoreGateway.Messages;
using LinqToDB;
using Microsoft.Extensions.Options;
using Rebus.Bus;
using Rebus.Handlers;

namespace CoreGateway.Dispatcher.Handlers
{
    internal class FileProcessedHandler : IHandleMessages<FileProcessedMessage>
    {
        private readonly ILogger<FileProcessedHandler> _logger;
        private readonly IBus _bus;
        private readonly DispatcherOptions _options;
        public FileProcessedHandler(ILogger<FileProcessedHandler> logger, IBus bus, IOptions<DispatcherOptions> options)
        {
            _logger = logger;
            _bus = bus;
            _options = options.Value;
        }

        public async Task Handle(FileProcessedMessage message)
        {
            try
            {
                await using var db = new CoreGatewayDb(
                    new DataOptions<CoreGatewayDb>(
                        new DataOptions()
                            .UsePostgreSQL(_options.StorageConnectionString)
                            //.UseInterceptor(new MyInterceptor(_logger))
                            ));

                var updated = await db.FileToProcesses
                    .Where(f => f.Id == message.Id)
                    .Set(f => f.CompletedAt, () => Sql.CurrentTimestamp)
                    .Set(f => f.Error, () => message.Error)
                    .UpdateWithOutputAsync((del, ins) => ins);

                var u = updated.SingleOrDefault();

                if (u != null)
                    _logger.InterpolatedDebug($"Task {message.Id} completed, file updated: {u.Name}.");
                else
                    _logger.InterpolatedWarning($"Task [{message.Id}] completed, but is not found in DB.");
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
