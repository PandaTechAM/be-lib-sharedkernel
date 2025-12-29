using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace SharedKernel.OpenApi;

internal sealed class TagOrderingTransformer : IOpenApiDocumentTransformer
{
   public Task TransformAsync(OpenApiDocument document,
      OpenApiDocumentTransformerContext context,
      CancellationToken cancellationToken)
   {
      var tagsByName = new Dictionary<string, OpenApiTag>(StringComparer.OrdinalIgnoreCase);

      if (document.Tags is not null)
      {
         foreach (var t in document.Tags)
         {
            if (!string.IsNullOrWhiteSpace(t.Name))
            {
               tagsByName[t.Name] = t; // keep existing metadata if present
            }
         }
      }

      foreach (var path in document.Paths.Values)
      {
         var operations = path.Operations;
         if (operations is null)
         {
            continue;
         }

         foreach (var name in from opTags in operations.Values
                                                       .Select(op => op.Tags)
                                                       .OfType<ISet<OpenApiTagReference>>()
                              from tr in opTags
                              select tr.Name
                              into name
                              where !string.IsNullOrWhiteSpace(name)
                              where !tagsByName.ContainsKey(name)
                              select name)
         {
            tagsByName[name] = new OpenApiTag
            {
               Name = name
            };
         }
      }

      var orderedNames = tagsByName.Keys
                                   .OrderBy(static x => x, StringComparer.OrdinalIgnoreCase)
                                   .ToList();

      var index = orderedNames
                  .Select((name, i) => (name, i))
                  .ToDictionary(x => x.name, x => x.i, StringComparer.OrdinalIgnoreCase);

      document.Tags ??= new HashSet<OpenApiTag>();
      document.Tags.Clear();

      foreach (var name in orderedNames)
      {
         document.Tags.Add(tagsByName[name]);
      }

      foreach (var opTags in document.Paths
                                     .Values
                                     .Select(path => path.Operations)
                                     .OfType<Dictionary<HttpMethod, OpenApiOperation>>()
                                     .SelectMany(operations => operations.Values
                                                                         .Select(op => op.Tags)
                                                                         .OfType<ISet<OpenApiTagReference>>()
                                                                         .Where(opTags => opTags.Count > 1)))
      {
         var ordered = opTags
                       .OrderBy(t => index.TryGetValue(t.Name ?? "", out var i) ? i : int.MaxValue)
                       .ToList();

         opTags.Clear();
         foreach (var t in ordered)
         {
            opTags.Add(t);
         }
      }

      return Task.CompletedTask;
   }
}