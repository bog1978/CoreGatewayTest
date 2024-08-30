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
                services.AddOptions<DispatcherOptions>().Bind(
                    ctx.Configuration.GetRequiredSection(nameof(DispatcherOptions)));

                services.AddLogging(l => l.AddConsole());
                services.AddHostedService<Worker>();
                services.AddRebusOptions(ctx.Configuration);
                services.AddRebus(ConfigureRebus);
                services.AddTransient<IHandleMessages<FileProcessedMessage>, FileProcessedHandler>();
                services.AddSingleton<IDispatcherDataAccess, DispatcherDataAccess>();
                services.ConfigureOTel();
            })
            .Build()
            .Run();

        private static RebusConfigurer ConfigureRebus(RebusConfigurer configurer, IServiceProvider services)
        {
            var rebusOptions = services.GetRequiredService<IOptions<RebusOptions>>().Value;
            return configurer
                .Transport(transport => transport.ConfigureRebusTransport(rebusOptions))
                .Routing(router => router.ConfigureRebusRouting(rebusOptions))
                .Options(options => options.ConfigureRebusOptions(rebusOptions, serviceName));
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