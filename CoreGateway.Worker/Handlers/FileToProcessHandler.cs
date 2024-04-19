using CoreGateway.Messages;
using Rebus.Bus;
using Rebus.Handlers;

namespace CoreGateway.Worker.Handlers
{
    internal class FileToProcessHandler : IHandleMessages<FileToProcessMessage>
    {
        private readonly ILogger<FileToProcessHandler> _logger;
        private readonly IBus _bus;

        public FileToProcessHandler(ILogger<FileToProcessHandler> logger, IBus bus)
        {
            _logger = logger;
            _bus = bus;
        }

        public async Task Handle(FileToProcessMessage message)
        {
            try
            {
                File.Delete(message.FilePath);
                await _bus.Reply(new FileProcessedMessage(message.Id, null));
                _logger.InterpolatedDebug($"File processed: {message.FilePath}.");
            }
            catch (Exception ex)
            {
                await _bus.Reply(new FileProcessedMessage(message.Id, ex.Message));
                _logger.InterpolatedError(ex, $"Error occured while processing file: {message.FilePath}.");
            }
        }
    }
}
