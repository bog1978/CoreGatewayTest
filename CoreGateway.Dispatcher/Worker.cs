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

            // ���� ������ ���, � �� �� �����, ����� �� ������� ������.
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

            // ���� ���� ������� �� ������� � �������, ���� ���������� ���.
            var serverNow = DateTime.UtcNow + serverOffset;
            if (existingFile != null && existingFile.TryAfter > serverNow)
                return;

            switch (existingFile)
            {
                // ����� ����.
                case null:
                    var newFile = await _dataAccess.InsertFileToProcess(fileName, stoppingToken);
                    await _bus.Send(new FileToProcessMessage(newFile.Id, fileName));
                    _logger.InterpolatedInformation($"�������� ����� ������ {newFile.Id:cg_taskId} �� ��������� ����� [{fileName:cg_fileName}]");
                    break;
                // ���� ��� ���������. ������� ����� ������� ��������� ��������.
                case { Status: FileStatus.Ok }:
                    _logger.InterpolatedWarning($"���� {fileName:cg_fileName} �������� ��� ������������, �� �� ������. ��� �����-�� �����.");
                    break;
                // � �������� ��������� ����� �������� ������.
                case { Status: FileStatus.Error, TryCount: < 25 }:
                    var resentFile = await _dataAccess.ResendFileToProcess(existingFile.Id);
                    await _bus.Send(new FileToProcessMessage(resentFile.Id, fileName));
                    _logger.InterpolatedWarning($"��������� �������� ������ {resentFile.Id:cg_taskId} �� ��������� ����� {fileName:cg_fileName} (������� {resentFile.TryCount:cg_tryCount}).");
                    break;
                case { Status: FileStatus.Error }:
                    _logger.InterpolatedError($"����� ������� ���������� ������ {existingFile.Id:cg_taskId} �� ��������� ����� {fileName:cg_fileName} �������� (������� {existingFile.TryCount:cg_tryCount}).");
                    break;
                case { Status: FileStatus.Waiting }:
                    // ������� ���������� ������.
                    _logger.InterpolatedInformation($"������ {existingFile.Id:cg_taskId} �� ��������� ����� {fileName:cg_fileName} ��� ���������� � �������.");
                    break;
            }
        }
    }
}