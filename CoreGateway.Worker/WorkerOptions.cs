namespace CoreGateway.Worker
{
    internal class WorkerOptions
    {
        public string RabbitConnectionString { get; set; } = "";
        public string WorkerQueueName { get; set; } = "";
    }
}
