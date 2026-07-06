using System.Globalization;
using FluentValidation;
using FluentValidation.Validators;
using Microsoft.AspNetCore.Http;

namespace SharedKernel.ValidatorAndMediatR.Validators.Files;

/// <summary>
///     Validates that the combined size of all files in an uploaded collection does not exceed the allowed total.
/// </summary>
/// <param name="maxMb">The maximum allowed total size across all files, in megabytes.</param>
public sealed class FilesTotalMaxSizeMbValidator<T>(int maxMb) : PropertyValidator<T, IFormFileCollection?>
{
    private readonly long _maxBytes = maxMb * 1024L * 1024L;

    /// <inheritdoc />
    public override string Name => "FilesTotalMaxSizeMb";

    /// <inheritdoc />
    public override bool IsValid(ValidationContext<T> context, IFormFileCollection? value)
    {
        var files = FormFileFilter.ForProperty(value, context.PropertyPath);
        if (files.Count == 0)
        {
            return true;
        }

        long sum = 0;
        foreach (var f in files)
        {
            sum += f.Length;
            if (sum > _maxBytes)
            {
                context.AddFailure($"Total upload size exceeds {maxMb.ToString(CultureInfo.InvariantCulture)} MB.");
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc />
    protected override string GetDefaultMessageTemplate(string errorCode)
    {
        return "total_upload_too_large";
    }
}
