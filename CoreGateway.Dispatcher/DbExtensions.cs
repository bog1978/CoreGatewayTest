using LinqToDB.Data;

namespace CoreGateway.Dispatcher
{
    internal static class DbExtensions
    {
        public static bool IsDatabaseExists(this DataConnection connection, string dbName) => connection
           .Execute<long>($"select count(*) from pg_database where datname = '{dbName}';") > 0;

        public static void CreateDatabase(this DataConnection connection, string dbName) => connection
           .Execute($"create database \"{dbName}\";");

        public static bool TryLock(this DataConnection connection, long key) => connection
            .Execute<bool>($"select pg_try_advisory_lock(@key);",
                DataParameter.Create("@key", key));

        public static bool Unlock(this DataConnection connection, long key) => connection
            .Execute<bool>($"select pg_advisory_unlock(@key);",
                DataParameter.Create("@key", key));
    }
}