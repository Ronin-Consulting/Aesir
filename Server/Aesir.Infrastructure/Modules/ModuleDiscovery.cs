using System.Reflection;
using Microsoft.Extensions.Logging;

namespace Aesir.Infrastructure.Modules;

/// <summary>
/// Provides functionality to discover modules in the application domain using convention-based assembly scanning.
/// Scans for assemblies matching the pattern "Aesir.Modules.*" and discovers types implementing IModule.
/// </summary>
public static class ModuleDiscovery
{
    /// <summary>
    /// Discovers all modules in the current application domain.
    /// </summary>
    /// <param name="loggerFactory">The logger factory for creating loggers.</param>
    /// <returns>A collection of discovered module instances.</returns>
    public static IEnumerable<IModule> DiscoverModules(ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("ModuleDiscovery");
        var modules = new List<IModule>();

        // Use the shared assembly discovery logic
        var moduleAssemblies = DiscoverModuleAssemblies(loggerFactory);

        // Discover module types from all module assemblies
        foreach (var assembly in moduleAssemblies)
        {
            try
            {
                var moduleTypes = assembly.GetTypes()
                    .Where(t => typeof(IModule).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

                foreach (var moduleType in moduleTypes)
                {
                    try
                    {
                        // Create a logger for this specific module type
                        var moduleLogger = loggerFactory.CreateLogger(moduleType);

                        if (Activator.CreateInstance(moduleType, moduleLogger) is IModule module)
                        {
                            modules.Add(module);
                            logger.LogInformation("Discovered module: {ModuleName} v{Version} from {AssemblyName}",
                                module.Name, module.Version, assembly.GetName().Name);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to instantiate module type {ModuleType}", moduleType.FullName);
                    }
                }
            }
            catch (ReflectionTypeLoadException ex)
            {
                logger.LogError(ex, "Failed to load types from assembly {AssemblyName}", assembly.FullName);
                foreach (var loaderException in ex.LoaderExceptions)
                {
                    if (loaderException != null)
                    {
                        logger.LogError(loaderException, "Loader exception details");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to scan assembly {AssemblyName}", assembly.FullName);
            }
        }

        logger.LogInformation("Discovered {ModuleCount} module(s)", modules.Count);
        return modules;
    }

    /// <summary>
    /// Discovers all module assemblies in the current application domain.
    /// This method loads module DLLs from the application directory if they haven't been loaded yet.
    /// </summary>
    /// <param name="loggerFactory">The logger factory for creating loggers.</param>
    /// <returns>A collection of module assemblies.</returns>
    public static IEnumerable<Assembly> DiscoverModuleAssemblies(ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("ModuleDiscovery");
        logger.LogInformation("Discovering modules...");

        var moduleAssemblies = new List<Assembly>();

        // Get all assemblies in the current app domain that match our module naming convention
        var loadedModuleAssemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && a.FullName != null && a.FullName.StartsWith("Aesir.Modules."))
            .ToList();

        moduleAssemblies.AddRange(loadedModuleAssemblies);

        // Also scan for module assemblies in the application directory
        var applicationPath = AppDomain.CurrentDomain.BaseDirectory;
        var dllFiles = Directory.GetFiles(applicationPath, "Aesir.Modules.*.dll", SearchOption.TopDirectoryOnly);

        foreach (var dllFile in dllFiles)
        {
            try
            {
                var assembly = Assembly.LoadFrom(dllFile);
                if (!moduleAssemblies.Contains(assembly))
                {
                    moduleAssemblies.Add(assembly);
                    logger.LogInformation("Loaded module assembly: {AssemblyName}", assembly.GetName().Name);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to load assembly {DllFile}", dllFile);
            }
        }

        return moduleAssemblies;
    }
}
