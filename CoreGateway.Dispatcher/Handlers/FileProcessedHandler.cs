using System.Diagnostics;
using CoreGateway.Dispatcher.DataAccess;
using CoreGateway.Messages;
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
        private readonly IDispatcherDataAccess _dataAccess;

        public FileProcessedHandler(ILogger<FileProcessedHandler> logger, IBus bus, IOptions<DispatcherOptions> options, IDispatcherDataAccess dataAccess)
        {
            _logger = logger;
            _bus = bus;
            _options = options.Value;
            _dataAccess = dataAccess;
        }

        public async Task Handle(FileProcessedMessage message)
        {
            using var activity = CoreGatewayTraceing.CoreGatewayActivity
                .StartActivity(ActivityKind.Consumer, name: "ProcessNewFile.End")
                ?.CheckBaggage(_logger);
            try
            {
                switch (message)
                {
                    //case null:
                    //    _logger.InterpolatedWarning($"Задача [{message.Id:id}] не найдена в БД.");
                    //    break;
                    case { Error: null }:
                        await _dataAccess.CompleteFileToProcess(message.MessageId);
                        _logger.InterpolatedInformation($"Задача {message.MessageId:cg_taskId} успешно выполнена.");
                        break;
                    case { Error: var err }:
                        var fileToProcess = await _dataAccess.DeferFileToProcess(message.MessageId, message.Error);
                        _logger.InterpolatedError($"Задача {message.MessageId:cg_taskId} не выполнена, т.к. возникла ошибка: {err:cg_error}");
                        break;
                }
                activity?.SetStatus(ActivityStatusCode.Ok);
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                throw;
            }
        }
    }
}
