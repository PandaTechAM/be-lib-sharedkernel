using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace SharedKernel.OpenApi;

internal static class EmbeddedFilesExtension
{
   private static readonly HashSet<string> AllowedResources = new(StringComparer.OrdinalIgnoreCase)
   {
      "panda-style.css",
      "panda-style.js",
      "favicon.svg",
      "logo.svg",
      "logo-wording.svg"
   };

   internal static WebApplication MapSwaggerUiAssetEndpoint(this WebApplication app)
   {
      app.Map("/swagger-resources/{resourceName}",
            async (HttpContext context, string resourceName) =>
            {
               if (!AllowedResources.Contains(resourceName))
               {
                  context.Response.StatusCode = StatusCodes.Status404NotFound;
                  await context.Response.WriteAsync($"Resource '{resourceName}' not found.");
                  return;
               }

               var assembly = typeof(EmbeddedFilesExtension).Assembly;
               var resourcePath = assembly.GetManifestResourceNames()
                                          .FirstOrDefault(x =>
                                             x.EndsWith(resourceName, StringComparison.OrdinalIgnoreCase));

               if (resourcePath == null)
               {
                  context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                  await context.Response.WriteAsync($"Resource '{resourceName}' not found in assembly.");
                  return;
               }

               await using var stream = assembly.GetManifestResourceStream(resourcePath);
               if (stream == null)
               {
                  context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                  await context.Response.WriteAsync($"Failed to load resource '{resourceName}'.");
                  return;
               }

               context.Response.ContentType = GetContentType(resourceName);
               await stream.CopyToAsync(context.Response.Body);
            })
         .WithGroupName("SwaggerUiAssetEndpoint");

      return app;
   }

   private static string GetContentType(string resourceName)
   {
      return Path.GetExtension(resourceName)
                 .ToLowerInvariant() switch
      {
         ".css" => "text/css",
         ".js" => "application/javascript",
         ".svg" => "image/svg+xml",
         ".png" => "image/png",
         ".jpg" or ".jpeg" => "image/jpeg",
         _ => "application/octet-stream"
      };
   }
}