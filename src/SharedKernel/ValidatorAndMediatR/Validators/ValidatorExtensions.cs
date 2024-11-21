using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace SharedKernel.ValidatorAndMediatR.Validators;

public static class ValidatorExtensions
{
   public static IRuleBuilderOptions<T, IFormFile?> HasMaxFileSize<T>(this IRuleBuilder<T, IFormFile?> ruleBuilder,
      int maxFileSizeInMb)
   {
      return ruleBuilder.SetValidator(new FileSizeValidator<T>(maxFileSizeInMb));
   }

   public static IRuleBuilderOptions<T, IFormFile?> FileTypeIsOneOf<T>(this IRuleBuilder<T, IFormFile?> ruleBuilder,
      params string[] allowedFileExtensions)
   {
      return ruleBuilder.SetValidator(new FileTypeValidator<T>(allowedFileExtensions));
   }

   public static IRuleBuilderOptions<T, string> IsValidJson<T>(this IRuleBuilder<T, string> ruleBuilder)
   {
      return ruleBuilder.SetValidator(new JsonValidator<T>());
   }

   public static IRuleBuilderOptions<T, string> IsXssSanitized<T>(this IRuleBuilder<T, string> ruleBuilder)
   {
      return ruleBuilder.SetValidator(new XssSanitizationValidator<T>());
   }
}