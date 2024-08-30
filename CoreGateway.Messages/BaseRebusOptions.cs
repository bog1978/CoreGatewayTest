namespace CoreGateway.Messages
{
    public abstract class BaseRebusOptions
    {
        /// <summary>
        /// Строка подключения к брокеру сообщений (например RabbitMQ).
        /// </summary>
        public string TransportConnectionString { get; set; } = "";

        /// <summary>
        /// Очередь входящих сообщений сервиса.
        /// </summary>
        public string InputQueueName { get; set; } = "";

        /// <summary>
        /// Максимальное количество параллельно обрабатываемых входящих сообщений в рамках процесса.
        /// </summary>
        public int MaxParallelism { get; set; } = 1;
    }
}