using CommissionCalculator.DTO;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using SharedKernel.Helpers;
using SharedKernel.ValidatorAndMediatR.Validators.Files;

namespace SharedKernel.ValidatorAndMediatR.Validators;

public static class ValidatorExtensions
{
   extension<T>(IRuleBuilder<T, string?> ruleBuilder)
   {
      public IRuleBuilderOptions<T, string?> IsEmail()
      {
         return ruleBuilder.Must(x => x is null || ValidationHelper.IsEmail(x))
                           .WithMessage("email_format_is_not_valid");
      }

      public IRuleBuilderOptions<T, string?> IsPhoneNumber()
      {
         return ruleBuilder.Must(x => x is null || ValidationHelper.IsPandaFormattedPhoneNumber(x))
                           .WithMessage("phone_number_format_is_not_valid");
      }

      public IRuleBuilderOptions<T, string?> IsEmailOrPhoneNumber()
      {
         return ruleBuilder
                .Must(x => x is null || ValidationHelper.IsPandaFormattedPhoneNumber(x) || ValidationHelper.IsEmail(x))
                .WithMessage("phone_number_or_email_format_is_not_valid");
      }

      public IRuleBuilderOptions<T, string?> IsPhoneNumberOrEmail()
      {
         return ruleBuilder.IsEmailOrPhoneNumber();
      }

      public IRuleBuilderOptions<T, string?> IsCreditCardNumber()
      {
         return ruleBuilder.SetValidator(new CreditCardNumberValidator<T>());
      }
   }

   extension<T>(IRuleBuilder<T, string> ruleBuilder)
   {
      public IRuleBuilderOptions<T, string> IsValidJson()
      {
         return ruleBuilder.SetValidator(new JsonValidator<T>());
      }

      public IRuleBuilderOptions<T, string> IsXssSanitized()
      {
         return ruleBuilder.SetValidator(new XssSanitizationValidator<T>());
      }
   }

   // Single file
   extension<T>(IRuleBuilder<T, IFormFile?> rb)
   {
      public IRuleBuilderOptions<T, IFormFile?> HasMaxSizeMb(int maxMb)
      {
         return rb.SetValidator(new FileMaxSizeMbValidator<T>(maxMb));
      }

      public IRuleBuilderOptions<T, IFormFile?> ExtensionIn(params string[] allowedExts)
      {
         return rb.SetValidator(new FileExtensionValidator<T>(allowedExts));
      }
   }

   // Collection files
   extension<T>(IRuleBuilder<T, IFormFileCollection?> rb)
   {
      public IRuleBuilderOptions<T, IFormFileCollection?> EachHasMaxSizeMb(int maxMb)
      {
         return rb.SetValidator(new FilesEachMaxSizeMbValidator<T>(maxMb));
      }

      public IRuleBuilderOptions<T, IFormFileCollection?> EachExtensionIn(params string[] allowedExts)
      {
         return rb.SetValidator(new FilesEachExtensionValidator<T>(allowedExts));
      }

      public IRuleBuilderOptions<T, IFormFileCollection?> TotalSizeMaxMb(int maxMb)
      {
         return rb.SetValidator(new FilesTotalMaxSizeMbValidator<T>(maxMb));
      }

      public IRuleBuilderOptions<T, IFormFileCollection?> MaxCount(int maxCount)
      {
         return rb.SetValidator(new FilesMaxCountValidator<T>(maxCount));
      }
   }

   public static IRuleBuilderOptions<T, CommissionRule?> ValidateCommissionRule<T>(this IRuleBuilder<T, CommissionRule?> rule)
   {
      return rule.SetValidator(new CommissionRuleValidator<T>());
   }
}