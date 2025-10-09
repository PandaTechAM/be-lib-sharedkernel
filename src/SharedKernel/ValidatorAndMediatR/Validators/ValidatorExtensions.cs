using CommissionCalculator.DTO;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using SharedKernel.Helpers;
using SharedKernel.ValidatorAndMediatR.Validators.Files;

namespace SharedKernel.ValidatorAndMediatR.Validators;

public static class ValidatorExtensions
{
   public static IRuleBuilderOptions<T, string?> IsEmail<T>(this IRuleBuilder<T, string?> ruleBuilder)
   {
      return ruleBuilder.Must(x => x is null || ValidationHelper.IsEmail(x))
                        .WithMessage("email_format_is_not_valid");
   }

   public static IRuleBuilderOptions<T, string?> IsPhoneNumber<T>(this IRuleBuilder<T, string?> ruleBuilder)
   {
      return ruleBuilder.Must(x => x is null || ValidationHelper.IsPandaFormattedPhoneNumber(x))
                        .WithMessage("phone_number_format_is_not_valid");
   }

   public static IRuleBuilderOptions<T, string?> IsEmailOrPhoneNumber<T>(this IRuleBuilder<T, string?> ruleBuilder)
   {
      return ruleBuilder
             .Must(x => x is null || ValidationHelper.IsPandaFormattedPhoneNumber(x) || ValidationHelper.IsEmail(x))
             .WithMessage("phone_number_or_email_format_is_not_valid");
   }

   public static IRuleBuilderOptions<T, string?> IsPhoneNumberOrEmail<T>(this IRuleBuilder<T, string?> ruleBuilder)
   {
      return ruleBuilder.IsEmailOrPhoneNumber();
   }

   public static IRuleBuilderOptions<T, string> IsValidJson<T>(this IRuleBuilder<T, string> ruleBuilder)
   {
      return ruleBuilder.SetValidator(new JsonValidator<T>());
   }

   public static IRuleBuilderOptions<T, string> IsXssSanitized<T>(this IRuleBuilder<T, string> ruleBuilder)
   {
      return ruleBuilder.SetValidator(new XssSanitizationValidator<T>());
   }

   // Single file
   public static IRuleBuilderOptions<T, IFormFile?> HasMaxSizeMb<T>(this IRuleBuilder<T, IFormFile?> rb, int maxMb)
   {
      return rb.SetValidator(new FileMaxSizeMbValidator<T>(maxMb));
   }

   public static IRuleBuilderOptions<T, IFormFile?> ExtensionIn<T>(this IRuleBuilder<T, IFormFile?> rb,
      params string[] allowedExts)
   {
      return rb.SetValidator(new FileExtensionValidator<T>(allowedExts));
   }

   // Collection files
   public static IRuleBuilderOptions<T, IFormFileCollection?> EachHasMaxSizeMb<T>(
      this IRuleBuilder<T, IFormFileCollection?> rb,
      int maxMb)
   {
      return rb.SetValidator(new FilesEachMaxSizeMbValidator<T>(maxMb));
   }

   public static IRuleBuilderOptions<T, IFormFileCollection?> EachExtensionIn<T>(
      this IRuleBuilder<T, IFormFileCollection?> rb,
      params string[] allowedExts)
   {
      return rb.SetValidator(new FilesEachExtensionValidator<T>(allowedExts));
   }

   public static IRuleBuilderOptions<T, IFormFileCollection?> TotalSizeMaxMb<T>(
      this IRuleBuilder<T, IFormFileCollection?> rb,
      int maxMb)
   {
      return rb.SetValidator(new FilesTotalMaxSizeMbValidator<T>(maxMb));
   }

   public static IRuleBuilderOptions<T, IFormFileCollection?> MaxCount<T>(this IRuleBuilder<T, IFormFileCollection?> rb,
      int maxCount)
   {
      return rb.SetValidator(new FilesMaxCountValidator<T>(maxCount));
   }
   public static IRuleBuilderOptions<T, CommissionRule?> ValidateCommissionRule<T>(this IRuleBuilder<T, CommissionRule?> rule)
   {
      return rule.SetValidator(new CommissionRuleValidator<T>());
   }
}