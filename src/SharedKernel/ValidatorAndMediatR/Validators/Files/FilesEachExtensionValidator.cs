using FluentValidation;
using FluentValidation.Validators;
using Microsoft.AspNetCore.Http;

namespace SharedKernel.ValidatorAndMediatR.Validators.Files;

public sealed class FilesEachExtensionValidator<T>(params string[] allowedExts)
   : PropertyValidator<T, IFormFileCollection?>
{
   private readonly HashSet<string> _allowed = new(allowedExts.Select(Exts.Norm), StringComparer.OrdinalIgnoreCase);
   public override string Name => "FilesEachExtension";

   public override bool IsValid(ValidationContext<T> context, IFormFileCollection? value)
   {
      if (_allowed.Count == 0)
      {
         return true;
      }

      var files = FormFileFilter.ForProperty(value, context.PropertyPath);
      if (files.Count == 0)
      {
         return true;
      }

      var ok = true;
      for (var i = 0; i < files.Count; i++)
      {
         var f = files[i];
         var ext = Exts.GetExt(f.FileName);
         if (_allowed.Contains(ext))
         {
            continue;
         }

         context.AddFailure($"File #{i + 1} '{f.FileName}' type is not allowed. Allowed: {string.Join(", ", _allowed)}");
         ok = false;
      }

      return ok;
   }

   protected override string GetDefaultMessageTemplate(string errorCode) => "file_type_not_allowed";
}