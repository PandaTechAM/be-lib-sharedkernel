using FluentValidation;
using FluentValidation.Validators;
using SharedKernel.Helpers;

namespace SharedKernel.ValidatorAndMediatR.Validators;

/// <summary>
///     FluentValidation property validator that checks whether a string is valid JSON.
/// </summary>
public class JsonValidator<T> : PropertyValidator<T, string>
{
    /// <inheritdoc />
    public override string Name => "JsonValidator";

    /// <inheritdoc />
    public override bool IsValid(ValidationContext<T> context, string? value)
    {
        if (value is null)
        {
            return true;
        }

        var isJson = ValidationHelper.IsJson(value);
        if (isJson)
        {
            return true;
        }

        context.AddFailure("The input is not valid JSON.");
        return false;
    }

    /// <inheritdoc />
    protected override string GetDefaultMessageTemplate(string errorCode)
    {
        return "Invalid JSON format.";
    }
}
