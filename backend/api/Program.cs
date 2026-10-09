using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json.Serialization;
using api.Background;
using api.Errors;
using api.Sidecar;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Infrastructure;
using Persistence;
using Serilog;
using Services;

namespace api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var sidecar = builder.Configuration.GetSection(SidecarOptions.SectionName).Get<SidecarOptions>() ?? new SidecarOptions();
            var dataDirectory = sidecar.ResolveDataDirectory();
            Directory.CreateDirectory(dataDirectory);
            builder.Services.AddSingleton(sidecar);

            // Loopback only: nothing on the network can reach the backend.
            builder.WebHost.ConfigureKestrel(kestrel => kestrel.Listen(IPAddress.Loopback, sidecar.Port));

            // Console for Electron/dev, rolling file so users can attach logs to bug reports.
            builder.Services.AddSerilog(logger => logger
                .ReadFrom.Configuration(builder.Configuration)
                .WriteTo.Console()
                .WriteTo.File(Path.Combine(dataDirectory, "logs", "backend-.log"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7));

            builder.Services.AddControllers()
                .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
            builder.Services.AddProblemDetails();
            builder.Services.AddExceptionHandler<ApiExceptionHandler>();
            builder.Services.AddPersistence(Path.Combine(dataDirectory, "autojob.db"));
            builder.Services.AddServices();
            builder.Services.AddInfrastructure(dataDirectory);
            builder.Services.AddCors(cors => cors.AddDefaultPolicy(policy => policy
                .WithOrigins(sidecar.AllowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()));
            builder.Services.AddHostedService<ParentProcessWatcher>();
            builder.Services.AddHostedService<SavedSearchRefresher>();
            builder.Services.AddHostedService<GenerationWorker>();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            var app = builder.Build();

            if (string.IsNullOrEmpty(sidecar.Token) && !app.Environment.IsDevelopment())
                throw new InvalidOperationException("Sidecar:Token must be set outside Development.");

            app.Services.ApplyMigrations();

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                // Swagger UI on the built-in OpenAPI document: http://127.0.0.1:5246/swagger
                app.UseSwaggerUI(o => o.SwaggerEndpoint("/openapi/v1.json", "AutoJob API"));
            }

            app.UseExceptionHandler();

            // CORS first so preflight (OPTIONS, no token) is answered before the token check.
            app.UseCors();
            app.UseMiddleware<TokenAuthMiddleware>();

            app.MapSidecarEndpoints();
            app.MapControllers();

            // Electron waits for this exact line on stdout to know the port and that we're ready.
            app.Lifetime.ApplicationStarted.Register(() =>
                Console.WriteLine($"AUTOJOB_READY {app.Urls.FirstOrDefault()}"));

            app.Run();
        }
    }
}
