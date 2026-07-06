using System.Reflection;
using Humanizer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.DependencyInjection;

namespace SharedKernel.Extensions;

/// <summary>
///     Provides MVC controller registration helpers.
/// </summary>
public static class ControllerExtensions
{
    /// <summary>
    ///     Register MVC controllers with kebab-case route naming, adding the given assemblies as application parts.
    /// </summary>
    public static WebApplicationBuilder AddControllers(this WebApplicationBuilder builder, Assembly[] assemblies)
    {
        var mvcBuilder =
            builder.Services.AddControllers(options => options.Conventions.Add(new ToLowerNamingConvention()));
        foreach (var assembly in assemblies)
        {
            mvcBuilder.AddApplicationPart(assembly);
        }

        return builder;
    }
}

/// <summary>
///     Controller model convention that kebab-cases controller and action names for lowercase routes.
/// </summary>
public class ToLowerNamingConvention : IControllerModelConvention
{
    /// <summary>
    ///     Kebab-case the controller name and all of its action names.
    /// </summary>
    public void Apply(ControllerModel controller)
    {
        controller.ControllerName = controller.ControllerName.Kebaberize();

        foreach (var action in controller.Actions)
        {
            action.ActionName = action.ActionName.Kebaberize();
        }
    }
}
