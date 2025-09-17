using FluentValidation;
using FluentValidation.Validators;
using Microsoft.AspNetCore.Http;

namespace SharedKernel.ValidatorAndMediatR.Validators.Files;

public sealed class FilesMaxCountValidator<T>(int maxCount) : PropertyValidator<T, IFormFileCollection?>
{
   public override string Name => "FilesMaxCount";

   public override bool IsValid(ValidationContext<T> context, IFormFileCollection? value)
   {
      var files = FormFileFilter.ForProperty(value, context.PropertyPath);
      if (files.Count <= maxCount)
      {
         return true;
      }

      context.AddFailure($"No more than {maxCount} files are allowed.");
      return false;
   }

   protected override string GetDefaultMessageTemplate(string errorCode) => "too_many_files";
}