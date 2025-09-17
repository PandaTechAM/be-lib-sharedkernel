using FluentValidation;
using FluentValidation.Validators;
using Microsoft.AspNetCore.Http;

namespace SharedKernel.ValidatorAndMediatR.Validators.Files;

public sealed class FileExtensionValidator<T>(params string[] allowedExts) : PropertyValidator<T, IFormFile?>
{
   private readonly HashSet<string> _allowed = new(allowedExts.Select(Exts.Norm), StringComparer.OrdinalIgnoreCase);
   public override string Name => "FileExtension";

   public override bool IsValid(ValidationContext<T> context, IFormFile? value)
   {
      if (value is null)
      {
         return true;
      }

      var ext = Exts.GetExt(value.FileName);
      if (string.IsNullOrEmpty(ext))
      {
         context.AddFailure("File has no extension.");
         return false;
      }

      if (_allowed.Count == 0 || _allowed.Contains(ext))
      {
         return true;
      }

      context.AddFailure($"File '{value.FileName}' type is not allowed. Allowed: {string.Join(", ", _allowed)}");
      return false;
   }

   protected override string GetDefaultMessageTemplate(string errorCode) => "file_type_not_allowed";
}