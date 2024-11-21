using FluentValidation;
using FluentValidation.Validators;
using Microsoft.AspNetCore.Http;

namespace SharedKernel.ValidatorAndMediatR.Validators;

public class FileSizeValidator<T>(int maxFileSizeInMb) : PropertyValidator<T, IFormFile?>
{
   public override string Name => "FileSizeValidator";

   public override bool IsValid(ValidationContext<T> context, IFormFile? value)
   {
      if (value == null)
      {
         return true;
      }

      if (value.Length <= maxFileSizeInMb * 1024 * 1024)
      {
         return true;
      }

      context.AddFailure($"File size exceeds the maximum allowed size of {maxFileSizeInMb} MB.");
      return false;
   }

   protected override string GetDefaultMessageTemplate(string errorCode)
   {
      return $"File size must not exceed {maxFileSizeInMb} MB.";
   }
}