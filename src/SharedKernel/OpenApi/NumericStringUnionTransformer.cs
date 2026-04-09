using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace SharedKernel.OpenApi;

// Workaround for https://github.com/dotnet/aspnetcore/issues/58882 and #64145.
// Microsoft.AspNetCore.OpenApi reflects JsonNumberHandling.AllowReadingFromString
// (enabled by default via JsonSerializerDefaults.Web) by emitting numeric schemas as
// type: ["integer","string"] with a bigint regex pattern. Swagger UI renders this as a
// string in bodies and breaks query/path parameters; downgrading to OpenAPI 3.0 drops
// the type entirely. We strip the string alternative so numbers stay numbers in the doc.
// Runtime deserialization is unchanged: STJ still accepts quoted numbers if a client sends them.
internal sealed class NumericStringUnionTransformer : IOpenApiSchemaTransformer
{
   public Task TransformAsync(OpenApiSchema schema,
      OpenApiSchemaTransformerContext context,
      CancellationToken cancellationToken)
   {
      Strip(schema);
      return Task.CompletedTask;
   }

   private static void Strip(IOpenApiSchema? schema)
   {
      if (schema is not OpenApiSchema s)
      {
         return;
      }

      if (s.Type is { } t
          && (t & JsonSchemaType.String) != 0
          && (t & (JsonSchemaType.Integer | JsonSchemaType.Number)) != 0)
      {
         s.Type = t & ~JsonSchemaType.String;
         s.Pattern = null;
      }

      if (s.Properties is not null)
      {
         foreach (var p in s.Properties.Values)
         {
            Strip(p);
         }
      }

      Strip(s.Items);
      Strip(s.AdditionalProperties);
      Strip(s.Not);

      if (s.AllOf is not null)
      {
         foreach (var sub in s.AllOf)
         {
            Strip(sub);
         }
      }

      if (s.OneOf is not null)
      {
         foreach (var sub in s.OneOf)
         {
            Strip(sub);
         }
      }

      if (s.AnyOf is not null)
      {
         foreach (var sub in s.AnyOf)
         {
            Strip(sub);
         }
      }
   }
}
