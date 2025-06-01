using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SharedKernel.Helpers;

namespace SharedKernel.Extensions;

public static class CorsExtensions
{
   public static WebApplicationBuilder AddCors(this WebApplicationBuilder builder)
   {
      if (builder.Environment.IsProduction())
      {
         var allowedOrigins = builder.Configuration
                                     .GetAllowedCorsOrigins()
                                     .SplitOrigins()
                                     .EnsureWwwAndNonWwwVersions();

         builder.Services.AddCors(options => options.AddPolicy("AllowSpecific",
            p => p
                 .WithOrigins(allowedOrigins)
                 .AllowCredentials()
                 .AllowAnyMethod()
                 .AllowAnyHeader()));
      }
      else
      {
         builder.Services.AddCors(options => options.AddPolicy("AllowAll",
            p => p
                 .SetIsOriginAllowed(_ => true)
                 .AllowCredentials()
                 .AllowAnyMethod()
                 .AllowAnyHeader()));
      }

      return builder;
   }

   public static WebApplication UseCors(this WebApplication app)
   {
      app.UseCors(app.Environment.IsProduction() ? "AllowSpecific" : "AllowAll");
      return app;
   }

   private static string[] SplitOrigins(this string input)
   {
      if (string.IsNullOrWhiteSpace(input))
      {
         throw new ArgumentException("Cors Origins cannot be null or empty.");
      }

      var result = input.Split([';', ','], StringSplitOptions.RemoveEmptyEntries);

      for (var i = 0; i < result.Length; i++)
      {
         result[i] = result[i]
            .Trim();

         if (ValidationHelper.IsUri(result[i], false))
         {
            continue;
         }

         Console.WriteLine($"Removed invalid cors origin: {result[i]}");
         result[i] = string.Empty;
      }

      return result.Where(x => !string.IsNullOrEmpty(x))
                   .ToArray();
   }

   private static string[] EnsureWwwAndNonWwwVersions(this string[] uris)
   {
      var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

      foreach (var uri in uris)
      {
         if (!Uri.TryCreate(uri, UriKind.Absolute, out var parsedUri))
         {
            continue;
         }

         var uriString = parsedUri.ToString()
                                  .TrimEnd('/');

         result.Add(uriString);


         var hostWithoutWww = parsedUri.Host.StartsWith("www.")
            ? parsedUri.Host.Substring(4)
            : parsedUri.Host;

         var uriWithoutWww = new UriBuilder(parsedUri)
            {
               Host = hostWithoutWww
            }.Uri
             .ToString()
             .TrimEnd('/');

         var uriWithWww = new UriBuilder(parsedUri)
            {
               Host = "www." + hostWithoutWww
            }.Uri
             .ToString()
             .TrimEnd('/');

         result.Add(uriWithoutWww);
         result.Add(uriWithWww);
      }

      return new List<string>(result).ToArray();
   }
}