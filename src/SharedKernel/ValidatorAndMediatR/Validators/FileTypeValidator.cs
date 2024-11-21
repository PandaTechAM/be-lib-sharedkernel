using FluentValidation;
using FluentValidation.Validators;
using Microsoft.AspNetCore.Http;

namespace SharedKernel.ValidatorAndMediatR.Validators;

public class FileTypeValidator<T>(params string[] allowedExtensions) : PropertyValidator<T, IFormFile?>
{
   public override string Name => "FileTypeValidator";

   public override bool IsValid(ValidationContext<T> context, IFormFile? value)
   {
      if (value is null)
      {
         return true;
      }

      if (string.IsNullOrWhiteSpace(value.FileName))
      {
         context.AddFailure("File has no name.");
         return false;
      }

      var fileExtension = Path.GetExtension(value.FileName)
                              .ToLowerInvariant();

      if (allowedExtensions.Contains(fileExtension, StringComparer.OrdinalIgnoreCase))
      {
         return true;
      }

      context.AddFailure($"Invalid file type. Allowed file types are: {string.Join(", ", allowedExtensions)}");
      return false;
   }

   protected override string GetDefaultMessageTemplate(string errorCode)
   {
      return $"Invalid file type. Allowed file types are: {string.Join(", ", allowedExtensions)}";
   }
}