using Microsoft.AspNetCore.Http;

namespace SharedKernel.ValidatorAndMediatR.Validators.Files;

// IFormFileCollection is bound from the entire Request.Form.Files, not filtered by the property name. Without this code validations fail if one request has multiple file upload properties.
internal static class FormFileFilter
{
   public static IReadOnlyList<IFormFile> ForProperty(IFormFileCollection? files, string propertyName)
   {
      if (files is null || files.Count == 0)
      {
         return [];
      }

      // Match exact field name (e.g., "Docs") OR indexed/nested names (Docs[0], Docs.0, command.Docs..., etc.)
      var prefix = propertyName + "[";
      var dot = propertyName + ".";
      var list = new List<IFormFile>(files.Count);

      foreach (var f in files)
      {
         var name = f.Name;
         if (name.Equals(propertyName, StringComparison.OrdinalIgnoreCase)
             || name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
             || name.StartsWith(dot, StringComparison.OrdinalIgnoreCase)
             || EndsWithSegment(name, propertyName))
         {
            list.Add(f);
         }
      }

      return list;

      // Handles cases like "command.Docs[0]" or "request.Docs"
      static bool EndsWithSegment(string name, string segment)
      {
         if (name.Length < segment.Length) return false;
         var i = name.LastIndexOf(segment, StringComparison.OrdinalIgnoreCase);
         if (i < 0) return false;

         var startOk = i == 0 || name[i - 1] is '.'; // previous is a dot boundary
         var endIdx = i + segment.Length;
         var endOk = endIdx == name.Length || name[endIdx] is '.' or '[';
         return startOk && endOk;
      }
   }
}