using System.Globalization;
using FluentValidation;
using FluentValidation.Validators;
using Microsoft.AspNetCore.Http;

namespace SharedKernel.ValidatorAndMediatR.Validators.Files;

public sealed class FilesTotalMaxSizeMbValidator<T>(int maxMb) : PropertyValidator<T, IFormFileCollection?>
{
   private readonly long _maxBytes = maxMb * 1024L * 1024L;
   public override string Name => "FilesTotalMaxSizeMb";

   public override bool IsValid(ValidationContext<T> context, IFormFileCollection? value)
   {
      var files = FormFileFilter.ForProperty(value, context.PropertyPath);
      if (files.Count == 0)
      {
         return true;
      }

      long sum = 0;
      foreach (var f in files)
      {
         sum += f.Length;
         if (sum > _maxBytes)
         {
            context.AddFailure($"Total upload size exceeds {maxMb.ToString(CultureInfo.InvariantCulture)} MB.");
            return false;
         }
      }

      return true;
   }

   protected override string GetDefaultMessageTemplate(string errorCode) => "total_upload_too_large";
}