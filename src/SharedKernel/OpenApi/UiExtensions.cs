using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Scalar.AspNetCore;
using SharedKernel.Extensions;
using SharedKernel.OpenApi.Options;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace SharedKernel.OpenApi;

internal static class UiExtensions
{
   internal static WebApplication MapSwaggerUi(this WebApplication app, OpenApiConfig openApiConfigConfiguration)
   {
      var swaggerUiConfig = app.Configuration
                               .GetSection("SwaggerUi")
                               .Get<SwaggerUiConfig>();

      app.UseSwaggerUI(options =>
      {
         foreach (var document in openApiConfigConfiguration.Documents)
         {
            options.SwaggerEndpoint($"{document.GetEndpointUrl()}", document.Title);
         }

         options.RoutePrefix = "swagger";
         options.AddPandaOptions(swaggerUiConfig);
      });


      foreach (var document in openApiConfigConfiguration.Documents.Where(x => x.ForExternalUse))
      {
         app.UseSwaggerUI(options =>
         {
            options.SwaggerEndpoint($"{document.GetEndpointUrl()}", document.Title);
            options.RoutePrefix = $"doc/{document.GroupName}";
            options.AddPandaOptions(swaggerUiConfig);
         });
      }

      return app;
   }

   internal static WebApplication MapScalarUi(this WebApplication app)
   {
      var scalarConfig = app.Configuration
                            .GetSection("ScalarUi")
                            .Get<ScalarUiConfig>();

      app.MapScalarApiReference(options =>
      {
         options.Theme = ScalarTheme.Kepler;
         if (scalarConfig?.FaviconPath is not null)
         {
            options.Favicon = "/swagger-resources/favicon.svg";
         }
      });
      return app;
   }

   private static string GetEndpointUrl(this Document document)
   {
      return $"/openapi/{document.GroupName}.json";
   }

   private static SwaggerUIOptions AddPandaOptions(this SwaggerUIOptions options, SwaggerUiConfig? swaggerUiConfig)
   {
      options.DocExpansion(DocExpansion.None);
      if (swaggerUiConfig is null)
      {
         return options;
      }
      
      options.InjectStylesheet("/swagger-resources/panda-style.css");
      options.InjectJavascript("/swagger-resources/panda-style.js");

      return options;
   }
}