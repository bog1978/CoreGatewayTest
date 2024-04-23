using CoreGateway.Dispatcher.DbModel;
using EvolveDb;
using LinqToDB;
using LinqToDB.Data;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Npgsql;

namespace CoreGateway.Dispatcher.DataAccess
{
    internal class DispatcherDataAccess : IDispatcherDataAccess
    {
        private readonly ILogger<DispatcherDataAccess> _logger;
        private readonly DataOptions<CoreGatewayDb> _dataOptions;
        private readonly DispatcherOptions _options;

        public DispatcherDataAccess(ILogger<DispatcherDataAccess> logger, IOptions<DispatcherOptions> options)
        {
            _options = options.Value;
            _dataOptions = new DataOptions<CoreGatewayDb>(new DataOptions()
                //.UseInterceptor(new LogCommandsInterceptor(logger))
                .UsePostgreSQL(options.Value.StorageConnectionString));
            _logger = logger;
        }

        public async Task<FileToProcess?> FindFileToProcess(string fileName, CancellationToken token)
        {
            await using var db = GetConnection();
            return await db.FileToProcesses
                .SingleOrDefaultAsync(x => x.Name == fileName, token);
        }

        public async Task<DateTime> GetServerTime()
        {
            await using var db = GetConnection();
            return await db.SelectAsync(() => Sql.CurrentTimestampUtc);
        }

        public async Task<FileToProcess> InsertFileToProcess(string fileName, CancellationToken token)
        {
            await using var db = GetConnection();
            return await db.FileToProcesses
                .Value(f => f.Id, () => Guid.NewGuid())
                .Value(f => f.Name, fileName)
                .Value(f => f.Status, FileStatus.Waiting)
                .Value(f => f.TryCount, 1)
                .Value(f => f.CreatedAt, () => Sql.CurrentTimestampUtc)
                .Value(f => f.TryAfter, () => Sql.CurrentTimestampUtc)
                .Value(f => f.Errors, () => new string[] { })
                .InsertWithOutputAsync(token);
        }

        public async Task<FileToProcess?> DeferFileToProcess(Guid id, string error)
        {
            await using var db = GetConnection();
            var updated = await db.FileToProcesses
                .Where(f => f.Id == id)
                .Set(f => f.Status, FileStatus.Error)
                .Set(f => f.TryAfter, f => Sql.CurrentTimestampUtc.AddSeconds(f.TryCount * 10 + 10))
                .Set(f => f.Errors, f => PgSql.ArrayAppend(f.Errors, new[] { error }))
                .UpdateWithOutputAsync((del, ins) => ins);
            var sql = db.LastQuery;
            return updated.SingleOrDefault();
        }

        public async Task<FileToProcess> ResendFileToProcess(Guid id)
        {
            await using var db = GetConnection();
            var updated = await db.FileToProcesses
                .Where(f => f.Id == id)
                .Set(f => f.Status, FileStatus.Waiting)
                .Set(f => f.TryCount, f => f.TryCount + 1)
                .UpdateWithOutputAsync((del, ins) => ins);
            return updated.Single();
        }

        public async Task CompleteFileToProcess(Guid id)
        {
            await using var db = GetConnection();
            await using var tran = await db.BeginTransactionAsync();

            var deleted = await db.FileToProcesses
                .Where(f => f.Id == id)
                .DeleteWithOutputAsync();

            var del = deleted.Single();

            await db.FileToProcessHistories
                .Value(f => f.Id, del.Id)
                .Value(f => f.Name, del.Name)
                .Value(f => f.Status, FileStatus.Ok)
                .Value(f => f.TryCount, del.TryCount)
                .Value(f => f.CreatedAt, del.CreatedAt)
                .Value(f => f.CompletedAt, () => Sql.CurrentTimestampUtc)
                .Value(f => f.Errors, () => del.Errors)
                .InsertAsync();

            await tran.CommitAsync();
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

        private CoreGatewayDb GetConnection() => new CoreGatewayDb(_dataOptions);
    }
}
