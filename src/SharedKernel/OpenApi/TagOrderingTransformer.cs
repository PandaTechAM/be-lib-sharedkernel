using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace SharedKernel.OpenApi;

internal sealed class TagOrderingTransformer : IOpenApiDocumentTransformer
{
   public Task TransformAsync(OpenApiDocument document,
      OpenApiDocumentTransformerContext context,
      CancellationToken cancellationToken)
   {
      var tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
      foreach (var path in document.Paths.Values)
      {
         foreach (var op in path.Operations.Values)
         {
            foreach (var t in op.Tags ?? [])
            {
               tags.Add(t.Name);
            }
         }
      }

      var ordered = tags.OrderBy(t => t, StringComparer.OrdinalIgnoreCase)
                        .ToList();

      document.Tags = ordered.Select(t => new OpenApiTag
                             {
                                Name = t
                             })
                             .ToList();

      var index = document.Tags
                          .Select((t, i) => (t.Name, i))
                          .ToDictionary(x => x.Name, x => x.i, StringComparer.OrdinalIgnoreCase);

      foreach (var path in document.Paths.Values)
      {
         foreach (var op in path.Operations.Values)
         {
            if (op.Tags is { Count: > 1 })
            {
               op.Tags = op.Tags
                           .OrderBy(t => index[t.Name])
                           .ToList();
            }
         }
      }

      return Task.CompletedTask;
   }
}