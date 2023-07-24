using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.DependencyModel.Resolution;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace CoreHook.Loader;

/// <summary>
/// Resolves assembly dependencies for the plugins during initialization,
/// such as NuGet packages dependencies.
/// </summary>
internal sealed class DependencyResolver
{
    private readonly ICompilationAssemblyResolver _assemblyResolver;
    private readonly AssemblyDependencyResolver _resolver;
    private readonly DependencyContext _dependencyContext;
    private readonly AssemblyLoadContext _loadContext;
    private readonly NotificationHelper _hostNotifier;

    private const string CoreHookModuleName = "CoreHook";

    public Assembly Assembly { get; }

    public DependencyResolver(string path, NotificationHelper hostNotifier)
    {
        _hostNotifier = hostNotifier;
        try
        {
            Log($"Image base path is {Path.GetDirectoryName(path)}");

            Assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
            
            _dependencyContext = DependencyContext.Load(Assembly);

            _assemblyResolver = new CompositeCompilationAssemblyResolver(new ICompilationAssemblyResolver[]
            {
                new AppBaseCompilationAssemblyResolver(Path.GetDirectoryName(path)),
                new ReferenceAssemblyPathResolver(),
                new PackageCompilationAssemblyResolver()
            });

            _loadContext = AssemblyLoadContext.GetLoadContext(Assembly);

            _loadContext.Resolving += OnResolving;
        }
        catch (Exception e)
        {
            Log($"AssemblyResolver error: {e}");
        }
    }

    private Assembly OnResolving(AssemblyLoadContext context, AssemblyName name)
    {
        bool NamesMatchOrContain(RuntimeLibrary runtime)
        {
            bool matched = string.Equals(runtime.Name, name.Name, StringComparison.OrdinalIgnoreCase);
            // if not matched by exact name or not a default corehook module (which should be matched exactly)
            if (!matched && !runtime.Name.Contains(CoreHookModuleName))
            {
                //matched = runtime.GetDefaultAssemblyNames(_dependencyContext).Any(name => string.Equals(name.Name, name.Name, StringComparison.OrdinalIgnoreCase));
                return runtime.Name.Contains(name.Name, StringComparison.OrdinalIgnoreCase);
            }
            return matched;
        }

        try
        {
            RuntimeLibrary library = _dependencyContext.RuntimeLibraries.FirstOrDefault(NamesMatchOrContain);
            
            if (library is not null)
            {
                var wrapper = new CompilationLibrary(
                    library.Type,
                    library.Name,
                    library.Version,
                    library.Hash,
                    library.RuntimeAssemblyGroups.SelectMany(g => g.AssetPaths),
                    library.Dependencies,
                    library.Serviceable);

                var assemblies = new List<string>();
                _assemblyResolver.TryResolveAssemblyPaths(wrapper, assemblies);

                if (assemblies.Count > 0)
                {
                    Log($"Resolved {assemblies[0]}");
                    return _loadContext.LoadFromAssemblyPath(assemblies[0]);
                }
                else
                {
                    Log($"Failed to resolve assembly {name.Name}");
                }
            }
        }
        catch (Exception e)
        {
            Log($"An error occured while resolving assembly {name.Name}: {e}");
        }
        return null;
    }

    public void Dispose()
    {
        _loadContext.Resolving -= OnResolving;
    }

    private void Log(string message)
    {
        _ = _hostNotifier.Log(message);
    }
}
