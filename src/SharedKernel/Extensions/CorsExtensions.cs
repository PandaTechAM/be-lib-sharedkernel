using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SharedKernel.Helpers;

namespace SharedKernel.Extensions;

public static class CorsExtensions
{
   private static readonly string[] ExposedHeaders = ["Content-Disposition"];

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
                 .AllowAnyHeader()
                 .WithExposedHeaders(ExposedHeaders)));
      }
      else
      {
         builder.Services.AddCors(options => options.AddPolicy("AllowAll",
            p => p
                 .SetIsOriginAllowed(_ => true)
                 .AllowCredentials()
                 .AllowAnyMethod()
                 .AllowAnyHeader()
                 .WithExposedHeaders(ExposedHeaders)));
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
         throw new ArgumentException("CORS origins cannot be null or empty.", nameof(input));

      return input
             .Split([';', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
             .Where(origin =>
             {
                if (ValidationHelper.IsUri(origin, false))
                {
                   return true;
                }

                Console.WriteLine($"Removed invalid CORS origin: {origin}");
                return false;
             })
             .ToArray();
   }

   private static string[] EnsureWwwAndNonWwwVersions(this string[] uris)
   {
      var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

      foreach (var uri in uris)
      {
         if (!Uri.TryCreate(uri, UriKind.Absolute, out var parsed)) continue;

         var bare = parsed.Host.StartsWith("www.", StringComparison.OrdinalIgnoreCase)
            ? parsed.Host[4..]
            : parsed.Host;

         result.Add(BuildOrigin(parsed, bare));
         result.Add(BuildOrigin(parsed, "www." + bare));
      }

      return [..result];
   }

   private static string BuildOrigin(Uri source, string host) =>
      new UriBuilder(source)
         {
            Host = host
         }.Uri
          .ToString()
          .TrimEnd('/');
}