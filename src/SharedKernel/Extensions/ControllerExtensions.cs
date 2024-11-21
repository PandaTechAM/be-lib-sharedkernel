using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace SharedKernel.Extensions;

public static class ControllerExtensions
{
   public static WebApplicationBuilder AddControllers(this WebApplicationBuilder builder, Assembly[] assemblies)
   {
      var mvcBuilder = builder.Services.AddControllers();
      foreach (var assembly in assemblies)
      {
         mvcBuilder.AddApplicationPart(assembly);
      }

      return builder;
   }
}