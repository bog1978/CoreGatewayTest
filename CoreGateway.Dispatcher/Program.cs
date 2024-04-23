using CoreGateway.Dispatcher.DataAccess;
using CoreGateway.Dispatcher.Handlers;
using CoreGateway.Messages;
using Microsoft.Extensions.Options;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Retry.Simple;
using Rebus.Routing.TypeBased;

namespace CoreGateway.Dispatcher
{
    public static class Program
    {
        public static void Main(string[] args) => Host
            .CreateDefaultBuilder(args)
            .ConfigureServices((ctx, services) =>
            {
                // Регистрация настроек.
                var section = ctx.Configuration.GetRequiredSection(nameof(DispatcherOptions));
                if (section == null)
                    throw new InvalidOperationException($"Не найдена секция конфигурации: [{nameof(DispatcherOptions)}].");
                services.AddOptions<DispatcherOptions>().Bind(section);

                services.AddLogging(l => l.AddConsole());
                services.AddHostedService<Worker>();
                services.AddRebus(ConfigureRebus);
                services.AddTransient<IHandleMessages<FileProcessedMessage>, FileProcessedHandler>();
                services.AddSingleton<IDispatcherDataAccess, DispatcherDataAccess>();
            })
            .Build()
            .Run();

        private static RebusConfigurer ConfigureRebus(RebusConfigurer configurer, IServiceProvider services)
        {
            var dispatcherOptions = services.GetRequiredService<IOptions<DispatcherOptions>>().Value;
            return configurer
                .Transport(transport =>
                {
                    transport
                        .UseRabbitMq(dispatcherOptions.RabbitConnectionString, dispatcherOptions.DispatcherQueueName)
                        .PriorityQueue(5);
                })
                .Routing(router =>
                {
                    router
                        .TypeBased()
                        .Map<FileToProcessMessage>(dispatcherOptions.WorkerQueueName);
                })
                .Options(options =>
                {
                    options.SetBusName("Core gateway dispatcher bus");
                    options.SetMaxParallelism(12);
                    options.RetryStrategy(
                        errorQueueName: $"{dispatcherOptions.DispatcherQueueName}_error",
                        maxDeliveryAttempts: 3);
                });
        }
    }
}