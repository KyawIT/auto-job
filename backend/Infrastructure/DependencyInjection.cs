using Infrastructure.Ai;
using Infrastructure.Export;
using Infrastructure.Files;
using Infrastructure.JobSources;
using Infrastructure.Secrets;
using Infrastructure.Text;
using Microsoft.Extensions.DependencyInjection;
using Services.Ports;

namespace Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, string dataDirectory)
        {
            services.AddSingleton<ISecretStore, InMemorySecretStore>();
            services.AddSingleton<IFileStorage>(new LocalFileStorage(dataDirectory));
            services.AddSingleton<ITextExtractor, DocumentTextExtractor>();
            services.AddSingleton<ICoverLetterRenderer, CoverLetterRenderer>();
            services.AddSingleton<IEmailDraftBuilder, EmlDraftBuilder>();

            services.AddHttpClient<IJobSearchClient, AdzunaClient>(c => c.Timeout = TimeSpan.FromSeconds(30));
            services.AddHttpClient<IJobSearchClient, BundesagenturClient>(c => c.Timeout = TimeSpan.FromSeconds(30));
            // Long letters from large models can take a while.
            services.AddHttpClient<IAiClient, NimClient>(c => c.Timeout = TimeSpan.FromMinutes(3));
            return services;
        }
    }
}
