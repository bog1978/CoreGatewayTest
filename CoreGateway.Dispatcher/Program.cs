using CoreGateway.Dispatcher.DataAccess;
using CoreGateway.Dispatcher.Handlers;
using CoreGateway.Messages;
using Microsoft.Extensions.Options;
using Npgsql;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.OpenTelemetry.Configuration;

namespace CoreGateway.Dispatcher
{
    public static class Program
    {
        const string serviceName = "CoreGateway.Dispatcher";

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
                services.ConfigureOTel();
            })
            .Build()
            .Run();

        private static RebusConfigurer ConfigureRebus(RebusConfigurer configurer, IServiceProvider services)
        {
            var dispatcherOptions = services.GetRequiredService<IOptions<DispatcherOptions>>().Value;
            return configurer
                .Transport(transport => transport.ConfigureRebusTransport(dispatcherOptions))
                .Routing(router => router.ConfigureRebusRouting(dispatcherOptions))
                .Options(options => options.ConfigureRebusOptions(dispatcherOptions, serviceName));
        }

        private static IServiceCollection ConfigureOTel(this IServiceCollection services)
        {
            services.AddOpenTelemetry()
                .ConfigureResource(resource => resource
                    .AddService(serviceName))
                .WithTracing(tracing => tracing
                    .AddNpgsql()
                    .AddHttpClientInstrumentation()
                    .AddRebusInstrumentation()
                    .AddCoreGatewayInstrumentation()
                    //.AddConsoleExporter()
                    .AddOtlpExporter())
                .WithMetrics(metrics => metrics
                    .AddRuntimeInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddProcessInstrumentation()
                    .AddRebusInstrumentation()
                    //.AddConsoleExporter()
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
                            //.AddConsoleExporter()
                            .AddOtlpExporter();
                        options.IncludeFormattedMessage = true;
                    }));
            return services;
        }
    }
}