namespace CoreGateway.Dispatcher
{
    internal class DispatcherOptions
    {
        /// <summary>
        /// Строка подключение к БД, в которой хранятся все созданные задачи.
        /// </summary>
        public string StorageConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// Строка подключения к брокеру сообщений (например RabbitMQ).
        /// </summary>
        public string RabbitConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// Очередь входящих сообщений диспетчера.
        /// </summary>
        public string DispatcherQueueName { get; set; } = string.Empty;

        /// <summary>
        /// Очередь входящих сообщений воркера.
        /// </summary>
        public string WorkerQueueName { get; set; } = string.Empty;

        /// <summary>
        /// Каталог, из которого вычитываются файлы для обработки
        /// </summary>
        public string ListenDirectory { get; set; } = string.Empty;

        /// <summary>
        /// Фильтр на тип обрабатываемых файлов
        /// </summary>
        public string FileFilter { get; set; } = "*.zip";

        /// <summary>
        /// Интервал между сканами <see cref="ListenDirectory"/>
        /// </summary>
        public TimeSpan ScanInterval { get; set; } = TimeSpan.FromSeconds(5);
    }
}
