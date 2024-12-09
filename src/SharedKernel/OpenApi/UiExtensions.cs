using Microsoft.AspNetCore.Builder;
using Scalar.AspNetCore;
using SharedKernel.OpenApi.Options;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace SharedKernel.OpenApi;

internal static class UiExtensions
{
   internal static WebApplication MapSwaggerUi(this WebApplication app, OpenApiConfig openApiConfigConfiguration)
   {
      app.UseSwaggerUI(options =>
      {
         foreach (var document in openApiConfigConfiguration.Documents)
         {
            options.SwaggerEndpoint($"{document.GetEndpointUrl()}", document.Title);
         }

         options.RoutePrefix = "swagger";
         options.AddPandaOptions();
      });


      foreach (var document in openApiConfigConfiguration.Documents.Where(x => x.ForExternalUse))
      {
         app.UseSwaggerUI(options =>
         {
            options.SwaggerEndpoint($"{document.GetEndpointUrl()}", document.Title);
            options.RoutePrefix = $"swagger/{document.GroupName}";
            options.AddPandaOptions();
         });
      }

      return app;
   }

   internal static WebApplication MapScalarUi(this WebApplication app)
   {
      app.MapScalarApiReference(options =>
      {
         options.Theme = ScalarTheme.Kepler;

         options.Favicon = "/swagger-resources/favicon.svg";
      });
      return app;
   }

   private static string GetEndpointUrl(this Document document)
   {
      return $"/openapi/{document.GroupName}.json";
   }

   private static SwaggerUIOptions AddPandaOptions(this SwaggerUIOptions options)
   {
      options.DocExpansion(DocExpansion.None);

      options.InjectStylesheet("/swagger-resources/panda-style.css");
      options.InjectJavascript("/swagger-resources/panda-style.js");

      return options;
   }
}