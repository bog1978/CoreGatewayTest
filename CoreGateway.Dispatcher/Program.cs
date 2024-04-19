using CoreGateway.Dispatcher.Handlers;
using CoreGateway.Messages;
using Microsoft.Extensions.Options;
using Rebus.Config;
using Rebus.Handlers;
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
                services.AddOptions<DispatcherOptions>().Bind(section);

                services.AddLogging(l => l.AddConsole());
                services.AddHostedService<Worker>();
                services.AddRebus(ConfigureRebus);
                services.AddTransient<IHandleMessages<FileProcessedMessage>, FileProcessedHandler>();
            })
            .Build()
            .Run();

        private static RebusConfigurer ConfigureRebus(RebusConfigurer configurer, IServiceProvider services)
        {
            var options = services.GetRequiredService<IOptions<DispatcherOptions>>().Value;
            return configurer
                .Transport(transport =>
                {
                    transport
                        .UseRabbitMq(options.RabbitConnectionString, options.DispatcherQueueName)
                        .PriorityQueue(5);
                })
                .Routing(router =>
                {
                    router
                        .TypeBased()
                        .Map<FileToProcessMessage>(options.WorkerQueueName);
                })
                .Options(options =>
                {
                    options.SetBusName("Core gateway dispatcher bus");
                    options.SetMaxParallelism(1);
                });
        }
    }
}