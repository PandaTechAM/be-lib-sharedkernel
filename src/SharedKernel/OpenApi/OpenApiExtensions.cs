using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RegexBox;
using SharedKernel.OpenApi.Options;

namespace SharedKernel.OpenApi;

public static class OpenApiExtensions
{
   public static WebApplicationBuilder AddOpenApi(this WebApplicationBuilder builder,
      Action<OpenApiOptions>? configureOptions = null)
   {

      var openApiConfiguration = builder.Configuration
                                        .GetSection("OpenApi")
                                        .Get<OpenApiConfig>();

      if (builder.Environment.IsOpenApiConfigValidAndDisabled(openApiConfiguration))
      {
         return builder;
      }


      foreach (var document in openApiConfiguration.Documents)
      {
         builder.Services.AddOpenApi(document.GroupName,
            options =>
            {
               options.AddDocumentTransformer<RemoveServersTransformer>();
               options.AddDocument(document, openApiConfiguration);
               options.AddSchemaTransformer<EnumSchemaTransformer>();
               options.UseApiSecuritySchemes(openApiConfiguration);
               configureOptions?.Invoke(options);
            });
      }

      return builder;
   }

   public static WebApplication UseOpenApi(this WebApplication app)
   {
      var openApiConfiguration = app.Configuration
                                    .GetSection("OpenApi")
                                    .Get<OpenApiConfig>();

      if (app.Environment.IsOpenApiConfigValidAndDisabled(openApiConfiguration))
      {
         return app;
      }

      app.MapOpenApi();
      app.MapSwaggerUiAssetEndpoint();
      app.MapSwaggerUi(openApiConfiguration);
      app.MapScalarUi(openApiConfiguration);
      return app;
   }

   private static bool IsOpenApiConfigValidAndDisabled(this IHostEnvironment environment,
      [NotNull] OpenApiConfig? openApiConfiguration)
   {
      if (openApiConfiguration is null || openApiConfiguration.Documents.Count == 0)
      {
         throw new InvalidOperationException("OpenApi configuration is missing or contains no documents.");
      }

      // Validate Contact
      if (openApiConfiguration.Contact is null)
      {
         throw new InvalidOperationException("Contact configuration is required in OpenApi.");
      }

      if (string.IsNullOrWhiteSpace(openApiConfiguration.Contact.Name))
      {
         throw new InvalidOperationException("Contact Name is required in OpenApi configuration.");
      }

      if (!PandaValidator.IsUri(openApiConfiguration.Contact.Url))
      {
         throw new InvalidOperationException("Contact URL must be a valid URL in OpenApi configuration.");
      }

      if (!PandaValidator.IsEmail(openApiConfiguration.Contact.Email))
      {
         throw new InvalidOperationException("Contact Email is required in OpenApi configuration.");
      }

      // Validate OpenApi documents
      foreach (var document in openApiConfiguration.Documents)
      {
         if (string.IsNullOrWhiteSpace(document.Title))
         {
            throw new InvalidOperationException("Document Title is required in OpenApi configuration.");
         }

         if (string.IsNullOrWhiteSpace(document.Description))
         {
            throw new InvalidOperationException(
               $"Document Description is required for document '{document.Title}' in OpenApi configuration.");
         }

         if (string.IsNullOrWhiteSpace(document.GroupName))
         {
            throw new InvalidOperationException(
               $"GroupName is required for document '{document.Title}' in OpenApi configuration.");
         }

         if (string.IsNullOrWhiteSpace(document.Version))
         {
            throw new InvalidOperationException(
               $"Version is required for document '{document.Title}' in OpenApi configuration.");
         }

         document.GroupName = document.GroupName.ToLowerInvariant();
      }

      // Validate SecuritySchemes
      foreach (var schema in openApiConfiguration.SecuritySchemes)
      {
         if (string.IsNullOrWhiteSpace(schema.HeaderName))
         {
            throw new InvalidOperationException("SecuritySchema HeaderName is required in OpenApi configuration..");
         }

         if (string.IsNullOrWhiteSpace(schema.Description))
         {
            throw new InvalidOperationException(
               $"Description is required for SecuritySchema with HeaderName '{schema.HeaderName}' in OpenApi configuration..");
         }
      }

      // Check if the environment is disabled
      return openApiConfiguration.DisabledEnvironments.Contains(environment.EnvironmentName);
   }
}