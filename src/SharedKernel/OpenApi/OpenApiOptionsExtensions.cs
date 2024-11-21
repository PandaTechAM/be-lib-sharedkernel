using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;
using SharedKernel.OpenApi.Options;

namespace SharedKernel.OpenApi;

internal static class OpenApiOptionsExtensions
{
   internal static OpenApiOptions AddDocument(this OpenApiOptions options,
      Document doc,
      OpenApiConfig openApiConfigConfiguration)
   {
      options.AddDocumentTransformer((document, _, _) =>
      {
         document.Info = new OpenApiInfo
         {
            Title = doc.Title,
            Description = doc.Description,
            Version = doc.Version,
            Contact = new OpenApiContact
            {
               Name = openApiConfigConfiguration.Contact.Name,
               Url = new Uri(openApiConfigConfiguration.Contact.Url),
               Email = openApiConfigConfiguration.Contact.Email
            }
         };
         return Task.CompletedTask;
      });

      return options;
   }

   internal static OpenApiOptions UseApiSecuritySchemes(this OpenApiOptions options, OpenApiConfig? configurations)
   {
      if (configurations is null)
      {
         return options;
      }

      foreach (var scheme in configurations.SecuritySchemes)
      {
         var securityScheme = new OpenApiSecurityScheme
         {
            Description = scheme.Description,
            Name = scheme.HeaderName,
            In = ParameterLocation.Header
         };

         options.AddDocumentTransformer((document, _, _) =>
         {
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes.Add(scheme.HeaderName, securityScheme);
            return Task.CompletedTask;
         });

         options.AddOperationTransformer((operation, _, _) =>
         {
            operation.Security ??= new List<OpenApiSecurityRequirement>();

            var securityRequirement = new OpenApiSecurityRequirement
            {
               {
                  new OpenApiSecurityScheme
                  {
                     Reference = new OpenApiReference
                     {
                        Type = ReferenceType.SecurityScheme,
                        Id = scheme.HeaderName
                     }
                  },
                  Array.Empty<string>()
               }
            };

            operation.Security.Add(securityRequirement);
            return Task.CompletedTask;
         });
      }

      return options;
   }
}