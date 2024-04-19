using CoreGateway.Dispatcher.DbModel;
using CoreGateway.Messages;
using EvolveDb;
using LinqToDB;
using LinqToDB.Data;
using Microsoft.Extensions.Options;
using Npgsql;
using Rebus.Bus;

namespace CoreGateway.Dispatcher
{
    internal class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IBus _bus;
        private readonly DispatcherOptions _options;

        public Worker(ILogger<Worker> logger, IBus bus, IOptions<DispatcherOptions> options)
        {
            _logger = logger;
            _bus = bus;
            _options = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            CreateDatabaseIfNotExists();
            MigrateDatabase();

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

            using var db = new CoreGatewayDb(new DataOptions<CoreGatewayDb>(
                new DataOptions().UsePostgreSQL(_options.StorageConnectionString)));

            var filesNames = Directory.EnumerateFiles(_options.ListenDirectory, _options.FileFilter, eo);

            foreach (var fileName in filesNames)
            {
                if (stoppingToken.IsCancellationRequested)
                    break;

                var exists = db.FileToProcesses.Any(x => x.Name == fileName && x.CompletedAt == null);
                if (exists)
                    continue;

                var fileToProcess = await db.FileToProcesses
                    .Value(f => f.Id, () => Guid.NewGuid())
                    .Value(f => f.Name, fileName)
                    .Value(f => f.CreatedAt, () => Sql.CurrentTimestamp)
                    .InsertWithOutputAsync(stoppingToken);

                var time = DateTime.Now;
                await _bus.Send(new FileToProcessMessage(fileToProcess.Id, fileName));
                _logger.InterpolatedInformation($"Sent task [{fileToProcess.Id}] to process file [{fileName}]");
            }
        }

        private void CreateDatabaseIfNotExists()
        {
            var builder = new NpgsqlConnectionStringBuilder(_options.StorageConnectionString);
            var dbName = builder.Database ?? throw new InvalidOperationException("Не задано имя БД.");
            builder.Database = "postgres";

            using var db = new DataConnection(
                new DataOptions().UsePostgreSQL(builder.ConnectionString));

            if (!db.IsDatabaseExists(dbName))
                db.CreateDatabase(dbName);
        }

        private void MigrateDatabase()
        {
            using var db = new DataConnection(
                new DataOptions().UsePostgreSQL(_options.StorageConnectionString));

            var evolve = new Evolve(db.Connection, msg => _logger.LogInformation(msg))
            {
                EmbeddedResourceAssemblies = new[] { GetType().Assembly },
                IsEraseDisabled = true,
            };
            evolve.Repair();
            evolve.Migrate();
        }
    }
}