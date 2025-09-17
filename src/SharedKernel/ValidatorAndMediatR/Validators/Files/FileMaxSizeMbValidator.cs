using System.Globalization;
using FluentValidation;
using FluentValidation.Validators;
using Microsoft.AspNetCore.Http;

namespace SharedKernel.ValidatorAndMediatR.Validators.Files;

public sealed class FileMaxSizeMbValidator<T>(int maxMb) : PropertyValidator<T, IFormFile?>
{
   private readonly long _maxBytes = maxMb * 1024L * 1024L;
   public override string Name => "FileMaxSizeMb";

   public override bool IsValid(ValidationContext<T> context, IFormFile? value)
   {
      if (value is null)
      {
         return true;
      }

      if (value.Length <= _maxBytes)
      {
         return true;
      }

      context.AddFailure($"File '{value.FileName}' exceeds {maxMb.ToString(CultureInfo.InvariantCulture)} MB.");
      return false;
   }

   protected override string GetDefaultMessageTemplate(string errorCode) => "file_too_large";
}