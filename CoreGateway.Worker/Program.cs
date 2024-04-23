using CoreGateway.Messages;
using CoreGateway.Worker.Handlers;
using Microsoft.Extensions.Options;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Retry.Simple;

namespace CoreGateway.Worker
{
    public static class Program
    {
        public static void Main(string[] args) => Host
            .CreateDefaultBuilder(args)
            .ConfigureServices((ctx, services) =>
            {
                // Регистрация настроек.
                var section = ctx.Configuration.GetRequiredSection(nameof(WorkerOptions));
                services.AddOptions<WorkerOptions>().Bind(section);

                services.AddLogging(l => l.AddConsole());
                services.AddRebus(ConfigureRebus);
                services.AddTransient<IHandleMessages<FileToProcessMessage>, FileToProcessHandler>();
                //services.AddTransient<IHandleMessages<IFailed<FileToProcessMessage>>, FileToProcessHandler>();
            })
            .Build()
            .Run();

        private static RebusConfigurer ConfigureRebus(RebusConfigurer configurer, IServiceProvider services)
        {
            var workerOptions = services.GetRequiredService<IOptions<WorkerOptions>>().Value;
            return configurer
                .Transport(transport =>
                {
                    transport
                        .UseRabbitMq(workerOptions.RabbitConnectionString, workerOptions.WorkerQueueName)
                        .PriorityQueue(5);
                })
                .Options(options =>
                {
                    options.SetBusName("Core gateway worker bus");
                    options.SetMaxParallelism(12);
                    options.RetryStrategy(
                        errorQueueName: $"{workerOptions.WorkerQueueName}_error",
                        maxDeliveryAttempts: 3);
                });
        }
    }
}