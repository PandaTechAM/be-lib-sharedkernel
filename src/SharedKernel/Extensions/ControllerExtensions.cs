using System.Reflection;
using System.Text.RegularExpressions;
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
      controller.ControllerName = controller.ControllerName.ToLower();

      foreach (var action in controller.Actions)
      {
         action.ActionName = action.ActionName.ToLower();
      }
   }
}

// public class KebabCaseTagNamingDocumentFilter : IDocumentFilter
// {
//    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
//    {
//       // Modify the OpenAPI document here if needed
//       foreach (var pathItem in swaggerDoc.Paths)
//       {
//          // Modify the OpenAPI operation tags to kebab case
//          foreach (var operation in pathItem.Value.Operations)
//          {
//             operation.Value.Tags = operation.Value
//                                             .Tags
//                                             .Select(tag => KebabCase(tag))
//                                             .ToList();
//          }
//       }
//    }
//
//    private OpenApiTag KebabCase(OpenApiTag tag)
//    {
//       // Simple kebab case conversion logic
//       const string pattern = "[^a-zA-Z-]";
//
//       var name = Regex.Replace(tag.Name, pattern, "");
//
//       var newName = string
//                     .Concat(name.Select((x, i) => i > 0 && char.IsUpper(x)
//                        ? "-" + x.ToString()
//                                 .ToLower()
//                        : x.ToString()))
//                     .ToLower();
//
//       tag.Name = newName;
//       return tag;
//    }
// }