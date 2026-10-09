using Microsoft.Extensions.DependencyInjection;
using Services.Applications;
using Services.Documents;
using Services.Export;
using Services.Generation;
using Services.Jobs;
using Services.Profiles;
using Services.Settings;

namespace Services
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddServices(this IServiceCollection services)
        {
            services.AddScoped<ProfileService>();
            services.AddScoped<SettingsService>();
            services.AddScoped<DocumentService>();
            services.AddScoped<JobService>();
            services.AddScoped<JobSearchService>();
            services.AddScoped<ApplicationService>();
            services.AddScoped<GenerationService>();
            services.AddScoped<ProfileImportService>();
            services.AddScoped<BatchService>();
            services.AddScoped<ExportService>();
            services.AddSingleton<BatchProcessor>();
            services.AddSingleton(TimeProvider.System);
            return services;
        }
    }
}
