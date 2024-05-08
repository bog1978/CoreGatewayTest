using CoreGateway.Storage.Client;
using Microsoft.Extensions.Configuration;
using Refit;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddCoreGatewayStorageClient(this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddRefitClient<ICoreGatewayStorage>()
                .ConfigureHttpClient(c => c.BaseAddress = new Uri("http://localhost:5006"));
            return services;
        }
    }
}
