using System.Diagnostics.CodeAnalysis;
using Aesir.Orchestration.Extensions;
using Aesir.Infrastructure.Extensions;
using Aesir.Infrastructure.Middleware;
using NLog;
using NLog.Web;

namespace Aesir.Api.Server;

public class Program
{
    [Experimental("SKEXP0070")]
    public static async Task Main(string[] args)
    {
        // Initialize NLog for early logging
        var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
        logger.Debug("Initializing Aesir API");

        try
        {
            var builder = WebApplication.CreateBuilder(args);

            // Clear default logging providers and use NLog
            builder.Logging.ClearProviders();
            builder.Host.UseNLog();

            await builder.Services
                .ConfigureAesirInfrastructureAsync(builder.Configuration);

            builder.Services.AddAesirAIOrchestrationServices();

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAvaloniaApp", policy =>
                {
                    policy.WithOrigins("http://aesir.localhost:5236")
                        .AllowAnyMethod()
                        .AllowAnyHeader();
                });
            });

            // Register controllers and explicitly add module assemblies to ApplicationPartManager
            builder.Services.AddControllers()
                .ConfigureApplicationPartManager(apm =>
                {
                    var moduleAssemblies = ModuleExtensions.GetModuleAssemblies();
                    foreach (var assembly in moduleAssemblies)
                    {
                        logger.Info("[AESIR] Adding module assembly to MVC: {0}", assembly.GetName().Name);
                        apm.ApplicationParts.Add(new Microsoft.AspNetCore.Mvc.ApplicationParts.AssemblyPart(assembly));
                    }
                });

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddHealthChecks();

            builder.Services.AddSignalR();

            var app = builder.Build();

            // Dynamically discover and map SignalR hubs from loaded modules
            var moduleAssemblies = ModuleExtensions.GetModuleAssemblies();

            foreach (var assembly in moduleAssemblies)
            {
                var hubTypes = assembly.GetTypes()
                    .Where(t => t.IsClass && !t.IsAbstract && t.BaseType != null &&
                                t.BaseType.Name == "Hub");

                foreach (var hubType in hubTypes)
                {
                    // Get the MapHub method and invoke it
                    var hubName = hubType.Name.Replace("Hub", "").ToLowerInvariant();
                    var pattern = $"/{hubName}hub";

                    logger.Info("[AESIR] Mapping hub: {0} to {1}", hubType.FullName, pattern);

                    typeof(HubEndpointRouteBuilderExtensions)
                        .GetMethod(nameof(HubEndpointRouteBuilderExtensions.MapHub),
                            new[] { typeof(IEndpointRouteBuilder), typeof(string) })
                        ?.MakeGenericMethod(hubType)
                        .Invoke(null, [app, pattern]);
                }
            }

            app.MapHealthChecks("/healthz");

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            // Let Traffic do this for us
            //app.UseHttpsRedirection();

            app.UseCors("AllowAvaloniaApp");

            // Add correlation ID tracking for request tracing
            app.UseCorrelationId();

            app.UseAuthorization();

            app.MapControllers();

            // Initialize all registered modules
            app.UseModules();

            // Register model lifecycle hooks for graceful shutdown
            await app.RegisterModelLifecycleAsync();

            await app.RunAsync();
        }
        catch (Exception exception)
        {
            // Log fatal exception and rethrow
            logger.Error(exception, "Aesir API stopped due to exception");
            throw;
        }
        finally
        {
            // Ensure NLog flushes and shuts down properly
            LogManager.Shutdown();
        }
    }
}