using System.Globalization;
using FluentValidation;
using FluentValidation.Validators;
using Microsoft.AspNetCore.Http;

namespace SharedKernel.ValidatorAndMediatR.Validators.Files;

/// <summary>
///     Validates that a single uploaded file does not exceed the allowed size.
/// </summary>
/// <param name="maxMb">The maximum allowed file size, in megabytes.</param>
public sealed class FileMaxSizeMbValidator<T>(int maxMb) : PropertyValidator<T, IFormFile?>
{
    private readonly long _maxBytes = maxMb * 1024L * 1024L;

    /// <inheritdoc />
    public override string Name => "FileMaxSizeMb";

    /// <inheritdoc />
    public override bool IsValid(ValidationContext<T> context, IFormFile? value)
    {
        if (value is null)
        {
            return true;
        }

        if (value.Length <= _maxBytes)
        {
            return true;
        }

        context.AddFailure($"File '{value.FileName}' exceeds {maxMb.ToString(CultureInfo.InvariantCulture)} MB.");
        return false;
    }

    /// <inheritdoc />
    protected override string GetDefaultMessageTemplate(string errorCode)
    {
        return "file_too_large";
    }
}
