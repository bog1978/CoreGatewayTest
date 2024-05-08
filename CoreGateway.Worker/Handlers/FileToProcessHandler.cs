using System.Diagnostics;
using CoreGateway.Messages;
using CoreGateway.Storage.Client;
using Rebus.Bus;
using Rebus.Handlers;

namespace CoreGateway.Worker.Handlers
{
    internal class FileToProcessHandler : IHandleMessages<FileToProcessMessage>
    {
        private readonly ILogger<FileToProcessHandler> _logger;
        private readonly IBus _bus;
        private readonly ICoreGatewayStorage _storage;

        public FileToProcessHandler(ILogger<FileToProcessHandler> logger, IBus bus, ICoreGatewayStorage storage)
        {
            _logger = logger;
            _bus = bus;
            _storage = storage;
        }

        public async Task Handle(FileToProcessMessage message)
        {
            using var activity = CoreGatewayTraceing.CoreGatewayActivity
                .StartActivity(ActivityKind.Consumer, name: nameof(FileToProcessHandler));
            try
            {
                var fileGuid = await SendFile(message.FilePath);
                File.Delete(message.FilePath);
                await _bus.Reply(new FileProcessedMessage(message.MessageId, fileGuid, null));
                _logger.InterpolatedDebug($"Задача {message.MessageId:cg_taskId} выполнена. Файл обработан: {message.FilePath:cg_fileName}.");
                activity?.SetStatus(ActivityStatusCode.Ok);
            }
            catch (Exception ex)
            {
                await _bus.Reply(new FileProcessedMessage(message.MessageId, null, ex.Message));
                _logger.InterpolatedError(ex, $"Не удалось выполнить задачу {message.MessageId:cg_taskId}. Файл {message.FilePath:cg_fileName} не обработан.");
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            }
        }

        private async Task<Guid> SendFile(string filePath)
        {
            using var stream = File.OpenRead(filePath);
            return await _storage.FilePUT(new Refit.StreamPart(stream, filePath));
        }
    }
}
