using FluentValidation;
using FluentValidation.Validators;
using SharedKernel.Helpers;

namespace SharedKernel.ValidatorAndMediatR.Validators;

/// <summary>
///     FluentValidation property validator that checks whether a string is a valid credit card number.
/// </summary>
public sealed class CreditCardNumberValidator<T> : PropertyValidator<T, string?>
{
    /// <inheritdoc />
    public override string Name => "CreditCardNumberValidator";

    /// <inheritdoc />
    public override bool IsValid(ValidationContext<T> context, string? value)
    {
        if (value is null)
        {
            return true;
        }

        if (ValidationHelper.IsCreditCardNumber(value))
        {
            return true;
        }

        context.AddFailure("credit_card_number_format_is_not_valid");
        return false;
    }

    /// <inheritdoc />
    protected override string GetDefaultMessageTemplate(string errorCode)
    {
        return "credit_card_number_format_is_not_valid";
    }
}
