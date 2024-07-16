using System.Diagnostics;
using CoreGateway.Dispatcher.DataAccess;
using CoreGateway.Dispatcher.DbModel;
using CoreGateway.Messages;
using Microsoft.Extensions.Options;
using Rebus.Bus;

namespace CoreGateway.Dispatcher
{
    internal class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IBus _bus;
        private readonly DispatcherOptions _options;
        private readonly IDispatcherDataAccess _dataAccess;

        public Worker(ILogger<Worker> logger, IBus bus, IOptions<DispatcherOptions> options, IDispatcherDataAccess dataAccess)
        {
            _logger = logger;
            _bus = bus;
            _options = options.Value;
            _dataAccess = dataAccess;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _dataAccess.PrepareDatabase();

            while (!stoppingToken.IsCancellationRequested)
            {
                await DoWork(stoppingToken);
                await Task.Delay(_options.ScanInterval, stoppingToken);
            }
        }

        private async Task DoWork(CancellationToken stoppingToken)
        {
            var eo = new EnumerationOptions
            {
                IgnoreInaccessible = true,
                RecurseSubdirectories = true,
                MaxRecursionDepth = 4,
            };

            // Если файлов нет, в БД не лезем, чтобы не плодить трэйсы.
            if (!Directory.EnumerateFiles(_options.ListenDirectory, _options.FileFilter, eo).Any())
                return;

            var filesNames = Directory.EnumerateFiles(_options.ListenDirectory, _options.FileFilter, eo);
            var serverOffset = await _dataAccess.GetServerTime() - DateTime.UtcNow;

            foreach (var fileName in filesNames)
            {
                using var activity = CoreGatewayTraceing.CoreGatewayActivity
                    .StartActivity(ActivityKind.Producer)
                    ?.SetTag("CoreGateway.FileName", fileName);
                try
                {
                    await ProcessFile(serverOffset, fileName, stoppingToken);
                    activity?.SetStatus(ActivityStatusCode.Ok);
                }
                catch (Exception ex)
                {
                    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                    throw;
                }
            }
        }

        private async Task ProcessFile(TimeSpan serverOffset, string fileName, CancellationToken stoppingToken)
        {
            if (stoppingToken.IsCancellationRequested)
                return;

            var existingFile = await _dataAccess.FindFileToProcess(fileName, stoppingToken);

            // Если файл отложен до момента в будущем, пока пропускаем его.
            var serverNow = DateTime.UtcNow + serverOffset;
            if (existingFile != null && existingFile.TryAfter > serverNow)
                return;

            switch (existingFile)
            {
                // Новый файл.
                case null:
                    var newFile = await _dataAccess.InsertFileToProcess(fileName, stoppingToken);
                    await _bus.Send(new FileToProcessMessage(newFile.Id, fileName));
                    _logger.InterpolatedInformation($"Отправка новой задачи {newFile.Id:cg_taskId} на обработку файла [{fileName:cg_fileName}]");
                    break;
                // Файл уже обработан. Наверно нужно сделать повторную отправку.
                case { Status: FileStatus.Ok }:
                    _logger.InterpolatedWarning($"Файл {fileName:cg_fileName} числится как обработанный, но не удален. Это какой-то косяк.");
                    break;
                // В процессе обработки файла возникла ошибка.
                case { Status: FileStatus.Error, TryCount: < 25 }:
                    var resentFile = await _dataAccess.ResendFileToProcess(existingFile.Id);
                    await _bus.Send(new FileToProcessMessage(resentFile.Id, fileName));
                    _logger.InterpolatedWarning($"Повторная отправка задачи {resentFile.Id:cg_taskId} на обработку файла {fileName:cg_fileName} (попытка {resentFile.TryCount:cg_tryCount}).");
                    break;
                case { Status: FileStatus.Error }:
                    _logger.InterpolatedError($"Лимит попыток выполнения задачи {existingFile.Id:cg_taskId} на обработку файла {fileName:cg_fileName} исчерпан (попытка {existingFile.TryCount:cg_tryCount}).");
                    break;
                case { Status: FileStatus.Waiting }:
                    // Ожидаем исполнения задачи.
                    _logger.InterpolatedInformation($"Задача {existingFile.Id:cg_taskId} на обработку файла {fileName:cg_fileName} уже отправлена в очередь.");
                    break;
            }
        }
    }
}