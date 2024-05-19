namespace CoreGateway.Storage.Service
{
    internal class StorageOptions
    {
        /// <summary>
        /// Строка подключение к БД, в которой хранятся все созданные задачи.
        /// </summary>
        public string StorageConnectionString { get; set; } = string.Empty;
    }
}
