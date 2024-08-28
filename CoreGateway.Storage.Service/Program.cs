using CoreGateway.Dispatcher.DataAccess;
using CoreGateway.Messages;
using Npgsql;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace CoreGateway.Storage.Service
{
    public static class Program
    {
        const string serviceName = "CoreGateway.Storage";

        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Регистрация настроек.
            var section = builder.Configuration.GetRequiredSection(nameof(StorageOptions));
            if (section == null)
                throw new InvalidOperationException($"Не найдена секция конфигурации: [{nameof(StorageOptions)}].");
            builder.Services.AddOptions<StorageOptions>().Bind(section);

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.ConfigureOTel();
            builder.Services.AddSingleton<IStorageDataAccess, StorageDataAccess>();

            var app = builder.Build();

            var dataAccess = app.Services.GetRequiredService<IStorageDataAccess>();
            dataAccess.PrepareDatabase();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseAuthorization();
            app.MapControllers();
            app.Run();
        }

        private static IServiceCollection ConfigureOTel(this IServiceCollection services)
        {
            services.AddOpenTelemetry()
                .ConfigureResource(resource => resource
                    .AddService(serviceName))
                .WithTracing(tracing => tracing
                    .AddNpgsql()
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddCoreGatewayInstrumentation()
                    //.AddConsoleExporter()
                    .AddOtlpExporter())
                .WithMetrics(metrics => metrics
                    .AddAspNetCoreInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddProcessInstrumentation()
                    .AddCoreGatewayInstrumentation()
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
