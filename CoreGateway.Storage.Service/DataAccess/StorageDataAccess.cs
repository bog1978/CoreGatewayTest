using CoreGateway.Storage.Service;
using CoreGateway.Storage.Service.DbModel;
using EvolveDb;
using LinqToDB;
using LinqToDB.Data;
using Microsoft.Extensions.Options;
using Npgsql;

namespace CoreGateway.Dispatcher.DataAccess
{
    internal class StorageDataAccess : IStorageDataAccess
    {
        private readonly ILogger<StorageDataAccess> _logger;
        private readonly DataOptions<StorageDb> _dataOptions;
        private readonly StorageOptions _options;

        public StorageDataAccess(ILogger<StorageDataAccess> logger, IOptions<StorageOptions> options)
        {
            _options = options.Value;
            _dataOptions = new DataOptions<StorageDb>(new DataOptions()
                //.UseInterceptor(new LogCommandsInterceptor(logger))
                .UsePostgreSQL(options.Value.StorageConnectionString));
            _logger = logger;
        }

        public async Task<FileDatum> InsertFile(Guid id, string fileName, byte[] data, CancellationToken token)
        {
            await using var db = GetConnection();
            return await db.FileData
                .Value(f => f.Id, () => Guid.NewGuid())
                .Value(f => f.Name, fileName)
                .Value(f => f.Data, data)
                .InsertWithOutputAsync(token);
        }

        public async Task<FileDatum?> GetFile(Guid id, CancellationToken token)
        {
            await using var db = GetConnection();
            return await db.FileData.SingleOrDefaultAsync(x => x.Id == id);
        }

        public void PrepareDatabase()
        {
            CreateDatabaseIfNotExists();
            MigrateDatabase();
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

        private StorageDb GetConnection() => new StorageDb(_dataOptions);
    }
}
