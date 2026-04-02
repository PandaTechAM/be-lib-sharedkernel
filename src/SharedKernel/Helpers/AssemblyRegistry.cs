using System.Reflection;
using Microsoft.AspNetCore.Builder;

namespace SharedKernel.Helpers;

public static class AssemblyRegistry
{
   private static readonly HashSet<Assembly> Assemblies = [];

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

   public static void Clear()
   {
      lock (Assemblies)
      {
         Assemblies.Clear();
      }
   }

   public static WebApplication ClearAssemblyRegistry(this WebApplication app)
   {
      Clear();
      return app;
   }

   public static Assembly[] ToArray()
   {
      lock (Assemblies)
      {
         return Assemblies.ToArray();
      }
   }
}