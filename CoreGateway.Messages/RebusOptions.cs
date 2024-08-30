namespace CoreGateway.Messages
{
    public class RebusOptions
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

        /// <summary>
        /// Настройки маршрутизации сообщений. Это простой словарь:
        ///     тип сообщения такой-то --> очередь такая-то
        /// </summary>
        public IDictionary<string, string> Routing { get; set; } = new Dictionary<string, string>();
    }
}