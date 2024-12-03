using System.Reflection;
using System.Text.RegularExpressions;
using Humanizer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace SharedKernel.Extensions;

public static class ControllerExtensions
{
   public static WebApplicationBuilder AddControllers(this WebApplicationBuilder builder, Assembly[] assemblies)
   {
      var mvcBuilder = builder.Services.AddControllers(options => options.Conventions.Add(new ToLowerNamingConvention()));
      foreach (var assembly in assemblies)
      {
         mvcBuilder.AddApplicationPart(assembly);
      }

      return builder;
   }
}

public class ToLowerNamingConvention : IControllerModelConvention
{
   public void Apply(ControllerModel controller)
   {
      controller.ControllerName = controller.ControllerName.Kebaberize();

      foreach (var action in controller.Actions)
      {
         action.ActionName = action.ActionName.Kebaberize();
      }
   }
}