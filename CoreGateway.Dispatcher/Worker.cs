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

            var filesNames = Directory.EnumerateFiles(_options.ListenDirectory, _options.FileFilter, eo);

            var serverOffset = await _dataAccess.GetServerTime() - DateTime.UtcNow;

            foreach (var fileName in filesNames)
            {
                if (stoppingToken.IsCancellationRequested)
                    break;

                var existingFile = await _dataAccess.FindFileToProcess(fileName, stoppingToken);

                // ���� ���� ������� �� ������� � �������, ���� ���������� ���.
                var serverNow = DateTime.UtcNow + serverOffset;
                if (existingFile != null && existingFile.TryAfter > serverNow)
                    continue;

                switch (existingFile)
                {
                    // ����� ����.
                    case null:
                        var newFile = await _dataAccess.InsertFileToProcess(fileName, stoppingToken);
                        await _bus.Send(new FileToProcessMessage(newFile.Id, fileName));
                        _logger.InterpolatedDebug($"�������� ����� ������ [{newFile.Id:id}] �� ��������� ����� [{fileName}]");
                        break;
                    // ���� ��� ���������. ������� ����� ������� ��������� ��������.
                    case { Status: FileStatus.Ok }:
                        _logger.InterpolatedWarning($"���� {fileName} �������� ��� ������������, �� �� ������. ��� �����-�� �����.");
                        break;
                    // � �������� ��������� ����� �������� ������.
                    case { Status: FileStatus.Error, TryCount: < 25 }:
                        var resentFile = await _dataAccess.ResendFileToProcess(existingFile.Id);
                        await _bus.Send(new FileToProcessMessage(resentFile.Id, fileName));
                        _logger.InterpolatedWarning($"��������� �������� ������ [{resentFile.Id:id}] �� ��������� ����� [{fileName}] (������� {resentFile.TryCount:tryCount}).");
                        break;
                    case { Status: FileStatus.Error }:
                        //_logger.InterpolatedError($"����� ������� ���������� ������ [{existingFile.Id:id}] �� ��������� ����� [{fileName}] �������� (������� {existingFile.TryCount:tryCount}).");
                        break;
                    case { Status: FileStatus.Waiting }:
                        // ������� ���������� ������.
                        break;
                }
            }
        }
    }
}