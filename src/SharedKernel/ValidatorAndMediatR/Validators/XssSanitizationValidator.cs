using FluentValidation;
using FluentValidation.Validators;

namespace SharedKernel.ValidatorAndMediatR.Validators;

/// <summary>
///     FluentValidation property validator that rejects strings which are entirely stripped by XSS sanitization.
/// </summary>
public class XssSanitizationValidator<T> : PropertyValidator<T, string>
{
    /// <inheritdoc />
    public override string Name => "XssSanitizationValidator";

    /// <inheritdoc />
    public override bool IsValid(ValidationContext<T> context, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        var sanitizedValue = XssSanitizer.Sanitize(value);

        if (!string.IsNullOrWhiteSpace(sanitizedValue))
        {
            return true;
        }

        context.AddFailure("The input was completely sanitized, indicating invalid content.");
        return false;
    }

    /// <inheritdoc />
    protected override string GetDefaultMessageTemplate(string errorCode)
    {
        return "The input contains XSS threats or is empty after sanitization.";
    }
}
