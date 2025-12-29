using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace SharedKernel.OpenApi;

internal class RemoveServersTransformer : IOpenApiDocumentTransformer
{
   public Task TransformAsync(OpenApiDocument document,
      OpenApiDocumentTransformerContext context,
      CancellationToken cancellationToken)
   {
      document.Servers?.Clear();
      return Task.CompletedTask;
   }
}