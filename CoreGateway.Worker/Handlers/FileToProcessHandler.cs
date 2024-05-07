using System.Diagnostics;
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
            using var activity = CoreGatewayTraceing.CoreGatewayActivity
                .StartActivity(ActivityKind.Consumer, name: nameof(FileToProcessHandler));
            try
            {
                //if (Random.Shared.Next(10) < 8)
                //    throw new InvalidOperationException("Могу сделать только меньшую часть работы.");
                File.Delete(message.FilePath);
                await _bus.Reply(new FileProcessedMessage(message.Id, null));
                _logger.InterpolatedDebug($"Задача {message.Id:id} выполнена. Файл обработан: {message.FilePath:fileName}.");
                activity?.SetStatus(ActivityStatusCode.Ok);
            }
            catch (Exception ex)
            {
                await _bus.Reply(new FileProcessedMessage(message.Id, ex.Message));
                _logger.InterpolatedError(ex, $"Не удалось выполнить задачу {message.Id:id}. Файл {message.FilePath:fileName} не обработан.");
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            }
        }
    }
}
