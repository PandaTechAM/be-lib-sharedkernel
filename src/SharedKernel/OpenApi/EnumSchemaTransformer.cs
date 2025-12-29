using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace SharedKernel.OpenApi;

internal class EnumSchemaTransformer : IOpenApiSchemaTransformer
{
   public Task TransformAsync(OpenApiSchema schema,
      OpenApiSchemaTransformerContext context,
      CancellationToken cancellationToken)
   {
      var type = context.JsonTypeInfo.Type;

      if (!type.IsEnum)
      {
         return Task.CompletedTask;
      }

      var enumDescriptions = Enum.GetValues(type)
                                 .Cast<object>()
                                 .Select(value => $"{value} = {(int)value}")
                                 .ToList();

      schema.Description = string.Join(", ", enumDescriptions);

      return Task.CompletedTask;
   }
}