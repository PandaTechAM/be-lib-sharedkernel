using System.Reflection;
using Microsoft.AspNetCore.Builder;

namespace SharedKernel.Helpers;

/// <summary>
///     A static registry of assemblies collected during startup for scanning (e.g. MediatR, validators).
/// </summary>
public static class AssemblyRegistry
{
    private static readonly HashSet<Assembly> Assemblies = [];

    /// <summary>
    ///     Adds one or more assemblies to the registry.
    /// </summary>
    public static void Add(params Assembly[] assemblies)
    {
        lock (Assemblies)
        {
            foreach (var assembly in assemblies)
            {
                Assemblies.Add(assembly);
            }
        }
    }

    /// <summary>
    ///     Removes all assemblies from the registry.
    /// </summary>
    public static void Clear()
    {
        lock (Assemblies)
        {
            Assemblies.Clear();
        }
    }

    /// <summary>
    ///     Clears the assembly registry, freeing the memory it holds after startup scanning is complete.
    /// </summary>
    public static WebApplication ClearAssemblyRegistry(this WebApplication app)
    {
        Clear();
        return app;
    }

    /// <summary>
    ///     Returns a snapshot array of all registered assemblies.
    /// </summary>
    public static Assembly[] ToArray()
    {
        lock (Assemblies)
        {
            return Assemblies.ToArray();
        }
    }
}
