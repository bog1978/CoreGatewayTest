using CoreGateway.Messages;
using CoreGateway.Worker.Handlers;
using Microsoft.Extensions.Options;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.OpenTelemetry.Configuration;
using Rebus.Retry.Simple;

namespace CoreGateway.Worker
{
    public static class Program
    {
        const string serviceName = "CoreGateway.Worker";

        public static void Main(string[] args) => Host
            .CreateDefaultBuilder(args)
            .ConfigureServices((ctx, services) =>
            {
                // ����������� ��������.
                var section = ctx.Configuration.GetRequiredSection(nameof(WorkerOptions));
                services.AddOptions<WorkerOptions>().Bind(section);

                services.AddLogging(l => l.AddConsole());
                services.AddRebus(ConfigureRebus);
                services.AddTransient<IHandleMessages<FileToProcessMessage>, FileToProcessHandler>();
                services.ConfigureOTel();
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
                    options.SetBusName($"{serviceName}.Bus");
                    options.SetMaxParallelism(12);
                    options.RetryStrategy(
                        errorQueueName: $"{workerOptions.WorkerQueueName}_error",
                        maxDeliveryAttempts: 3);
                    options.EnableDiagnosticSources();
                });
        }

        private static IServiceCollection ConfigureOTel(this IServiceCollection services)
        {
            services.AddOpenTelemetry()
                .ConfigureResource(resource => resource
                    .AddService(serviceName))
                .WithTracing(tracing => tracing
                    .AddHttpClientInstrumentation()
                    .AddRebusInstrumentation()
                    .AddCoreGatewayInstrumentation()
                    .AddConsoleExporter()
                    .AddOtlpExporter())
                .WithMetrics(metrics => metrics
                    .AddRuntimeInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddProcessInstrumentation()
                    .AddRebusInstrumentation()
                    .AddConsoleExporter()
                    .AddOtlpExporter());
            services.AddLogging(builder =>
                builder
                    .AddOpenTelemetry(options =>
                    {
                        options
                            .SetResourceBuilder(
                                ResourceBuilder
                                    .CreateDefault()
                                    .AddService(serviceName))
                            .AddConsoleExporter()
                            .AddOtlpExporter();
                        options.IncludeFormattedMessage = true;
                    }));
            return services;
        }
    }
}