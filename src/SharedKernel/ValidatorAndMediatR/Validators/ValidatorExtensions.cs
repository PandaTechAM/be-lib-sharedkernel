using CommissionCalculator.DTO;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using SharedKernel.Helpers;
using SharedKernel.ValidatorAndMediatR.Validators.Files;

namespace SharedKernel.ValidatorAndMediatR.Validators;

/// <summary>
///     FluentValidation rule-builder extensions for common string, credit card, JSON, XSS, and file validations.
/// </summary>
public static class ValidatorExtensions
{
    /// <summary>
    ///     Requires the commission rule's ranges to be internally consistent.
    /// </summary>
    public static IRuleBuilderOptions<T, CommissionRule?> ValidateCommissionRule<T>(
        this IRuleBuilder<T, CommissionRule?> rule)
    {
        return rule.SetValidator(new CommissionRuleValidator<T>());
    }

    extension<T>(IRuleBuilder<T, string?> ruleBuilder)
    {
        /// <summary>
        ///     Requires the value to be null or a valid email address.
        /// </summary>
        public IRuleBuilderOptions<T, string?> IsEmail()
        {
            return ruleBuilder.Must(x => x is null || ValidationHelper.IsEmail(x))
                .WithMessage("email_format_is_not_valid");
        }

        /// <summary>
        ///     Requires the value to be null or a Panda-formatted phone number.
        /// </summary>
        public IRuleBuilderOptions<T, string?> IsPhoneNumber()
        {
            return ruleBuilder.Must(x => x is null || ValidationHelper.IsPandaFormattedPhoneNumber(x))
                .WithMessage("phone_number_format_is_not_valid");
        }

        /// <summary>
        ///     Requires the value to be null, a valid email address, or a Panda-formatted phone number.
        /// </summary>
        public IRuleBuilderOptions<T, string?> IsEmailOrPhoneNumber()
        {
            return ruleBuilder
                .Must(x => x is null || ValidationHelper.IsPandaFormattedPhoneNumber(x) || ValidationHelper.IsEmail(x))
                .WithMessage("phone_number_or_email_format_is_not_valid");
        }

        /// <summary>
        ///     Requires the value to be null, a valid email address, or a Panda-formatted phone number.
        /// </summary>
        public IRuleBuilderOptions<T, string?> IsPhoneNumberOrEmail()
        {
            return ruleBuilder.IsEmailOrPhoneNumber();
        }

        /// <summary>
        ///     Requires the value to be null or a valid credit card number.
        /// </summary>
        public IRuleBuilderOptions<T, string?> IsCreditCardNumber()
        {
            return ruleBuilder.SetValidator(new CreditCardNumberValidator<T>());
        }
    }

    extension<T>(IRuleBuilder<T, string> ruleBuilder)
    {
        /// <summary>
        ///     Requires the value to be valid JSON.
        /// </summary>
        public IRuleBuilderOptions<T, string> IsValidJson()
        {
            return ruleBuilder.SetValidator(new JsonValidator<T>());
        }

        /// <summary>
        ///     Requires the value to survive XSS sanitization without being entirely stripped.
        /// </summary>
        public IRuleBuilderOptions<T, string> IsXssSanitized()
        {
            return ruleBuilder.SetValidator(new XssSanitizationValidator<T>());
        }
    }

    // Single file
    extension<T>(IRuleBuilder<T, IFormFile?> rb)
    {
        /// <summary>
        ///     Requires the file's size to not exceed <paramref name="maxMb" /> megabytes.
        /// </summary>
        public IRuleBuilderOptions<T, IFormFile?> HasMaxSizeMb(int maxMb)
        {
            return rb.SetValidator(new FileMaxSizeMbValidator<T>(maxMb));
        }

        /// <summary>
        ///     Requires the file's extension to be one of <paramref name="allowedExts" />.
        /// </summary>
        public IRuleBuilderOptions<T, IFormFile?> ExtensionIn(params string[] allowedExts)
        {
            return rb.SetValidator(new FileExtensionValidator<T>(allowedExts));
        }
    }

    // Collection files
    extension<T>(IRuleBuilder<T, IFormFileCollection?> rb)
    {
        /// <summary>
        ///     Requires each file in the collection to not exceed <paramref name="maxMb" /> megabytes.
        /// </summary>
        public IRuleBuilderOptions<T, IFormFileCollection?> EachHasMaxSizeMb(int maxMb)
        {
            return rb.SetValidator(new FilesEachMaxSizeMbValidator<T>(maxMb));
        }

        /// <summary>
        ///     Requires each file in the collection to have an extension in <paramref name="allowedExts" />.
        /// </summary>
        public IRuleBuilderOptions<T, IFormFileCollection?> EachExtensionIn(params string[] allowedExts)
        {
            return rb.SetValidator(new FilesEachExtensionValidator<T>(allowedExts));
        }

        /// <summary>
        ///     Requires the combined size of all files in the collection to not exceed <paramref name="maxMb" /> megabytes.
        /// </summary>
        public IRuleBuilderOptions<T, IFormFileCollection?> TotalSizeMaxMb(int maxMb)
        {
            return rb.SetValidator(new FilesTotalMaxSizeMbValidator<T>(maxMb));
        }

        /// <summary>
        ///     Requires the collection to contain no more than <paramref name="maxCount" /> files.
        /// </summary>
        public IRuleBuilderOptions<T, IFormFileCollection?> MaxCount(int maxCount)
        {
            return rb.SetValidator(new FilesMaxCountValidator<T>(maxCount));
        }
    }
}
