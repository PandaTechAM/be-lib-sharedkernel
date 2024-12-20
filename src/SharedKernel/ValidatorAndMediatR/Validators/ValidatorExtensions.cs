using FluentValidation;
using Microsoft.AspNetCore.Http;
using RegexBox;

namespace SharedKernel.ValidatorAndMediatR.Validators;

public static class ValidatorExtensions
{
   public static IRuleBuilderOptions<T, string?> IsEmail<T>(this IRuleBuilder<T, string?> ruleBuilder)
   {
      return ruleBuilder.Must(x => x is null || PandaValidator.IsEmail(x))
                        .WithMessage("email_format_is_not_valid");
   }

   public static IRuleBuilderOptions<T, string?> IsPhoneNumber<T>(this IRuleBuilder<T, string?> ruleBuilder)
   {
      return ruleBuilder.Must(x => x is null || PandaValidator.IsPandaFormattedPhoneNumber(x))
                        .WithMessage("phone_number_format_is_not_valid");
   }

   public static IRuleBuilderOptions<T, string?> IsEmailOrPhoneNumber<T>(this IRuleBuilder<T, string?> ruleBuilder)
   {
      return ruleBuilder
             .Must(x => x is null || PandaValidator.IsPandaFormattedPhoneNumber(x) || PandaValidator.IsEmail(x))
             .WithMessage("phone_number_or_email_format_is_not_valid");
   }

   public static IRuleBuilderOptions<T, string?> IsPhoneNumberOrEmail<T>(this IRuleBuilder<T, string?> ruleBuilder)
   {
      return ruleBuilder.IsEmailOrPhoneNumber();
   }

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