using System.Globalization;
using FluentValidation;
using FluentValidation.Validators;
using Microsoft.AspNetCore.Http;

namespace SharedKernel.ValidatorAndMediatR.Validators.Files;

public sealed class FilesEachMaxSizeMbValidator<T>(int maxMb) : PropertyValidator<T, IFormFileCollection?>
{
   private readonly long _maxBytes = maxMb * 1024L * 1024L;
   public override string Name => "FilesEachMaxSizeMb";

   public override bool IsValid(ValidationContext<T> context, IFormFileCollection? value)
   {
      var files = FormFileFilter.ForProperty(value, context.PropertyPath);
      if (files.Count == 0)
      {
         return true;
      }

      var ok = true;
      for (var i = 0; i < files.Count; i++)
      {
         var f = files[i];
         if (f.Length <= _maxBytes)
         {
            continue;
         }

         context.AddFailure($"File #{i + 1} '{f.FileName}' exceeds {maxMb.ToString(CultureInfo.InvariantCulture)} MB.");
         ok = false;
      }

      return ok;
   }

   protected override string GetDefaultMessageTemplate(string errorCode) => "file_too_large";
}