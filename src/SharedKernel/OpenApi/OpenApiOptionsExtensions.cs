using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using SharedKernel.OpenApi.Options;

namespace SharedKernel.OpenApi;

internal static class OpenApiOptionsExtensions
{
   extension(OpenApiOptions options)
   {
      internal OpenApiOptions AddDocument(Document doc, OpenApiConfig openApiConfig)
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
                  Name = openApiConfig.Contact.Name,
                  Url = new Uri(openApiConfig.Contact.Url),
                  Email = openApiConfig.Contact.Email
               }
            };

            return Task.CompletedTask;
         });

         return options;
      }
      
      internal OpenApiOptions UseApiSecuritySchemes(OpenApiConfig? config)
      {
         if (config?.SecuritySchemes is not { Count: > 0 }) { return options; }

         options.AddDocumentTransformer((document, _, _) =>
         {
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??=
               new Dictionary<string, IOpenApiSecurityScheme>(StringComparer.Ordinal);

            document.Security ??= new List<OpenApiSecurityRequirement>();
            document.Security.Clear();

            foreach (var s in config.SecuritySchemes)
            {
               // Strongly recommended: separate ID from header name.
               // If you can't change config now, keep s.HeaderName as the ID.
               var schemeId = s.HeaderName;

               document.Components.SecuritySchemes[schemeId] = new OpenApiSecurityScheme
               {
                  Type = SecuritySchemeType.ApiKey,
                  In = ParameterLocation.Header,
                  Name = s.HeaderName,
                  Description = s.Description
               };

               // IMPORTANT: reference must be created with document context
               document.Security.Add(new OpenApiSecurityRequirement
               {
                  [new OpenApiSecuritySchemeReference(schemeId, document)] = []
               });
            }

            return Task.CompletedTask;
         });

         return options;
      }

      // internal OpenApiOptions UseApiSecuritySchemes(OpenApiConfig? config)
      // {
      //    if (config?.SecuritySchemes is not { Count: > 0 })
      //    {
      //       return options;
      //    }
      //
      //    options.AddDocumentTransformer((document, _, _) =>
      //    {
      //       document.Components ??= new OpenApiComponents();
      //       document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>(StringComparer.Ordinal);
      //
      //       foreach (var scheme in config.SecuritySchemes)
      //       {
      //          document.Components.SecuritySchemes[scheme.HeaderName] = new OpenApiSecurityScheme
      //          {
      //             Type = SecuritySchemeType.ApiKey,
      //             In = ParameterLocation.Header,
      //             Name = scheme.HeaderName,
      //             Description = scheme.Description
      //          };
      //       }
      //
      //       return Task.CompletedTask;
      //    });
      //
      //    options.AddOperationTransformer((operation, _, _) =>
      //    {
      //       operation.Security ??= new List<OpenApiSecurityRequirement>();
      //
      //       foreach (var scheme in config.SecuritySchemes)
      //       {
      //          operation.Security.Add(new OpenApiSecurityRequirement
      //          {
      //             [new OpenApiSecuritySchemeReference(scheme.HeaderName)] = []
      //          });
      //       }
      //
      //       return Task.CompletedTask;
      //    });
      //
      //    return options;
      // }
   }
}
