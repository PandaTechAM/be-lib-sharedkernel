using FluentValidation;
using FluentValidation.Validators;
using Microsoft.AspNetCore.Http;

namespace SharedKernel.ValidatorAndMediatR.Validators.Files;

/// <summary>
///     Validates that an uploaded file collection does not exceed the allowed number of files.
/// </summary>
/// <param name="maxCount">The maximum number of files allowed.</param>
public sealed class FilesMaxCountValidator<T>(int maxCount) : PropertyValidator<T, IFormFileCollection?>
{
    /// <inheritdoc />
    public override string Name => "FilesMaxCount";

    /// <inheritdoc />
    public override bool IsValid(ValidationContext<T> context, IFormFileCollection? value)
    {
        var files = FormFileFilter.ForProperty(value, context.PropertyPath);
        if (files.Count <= maxCount)
        {
            return true;
        }

        context.AddFailure($"No more than {maxCount} files are allowed.");
        return false;
    }

    /// <inheritdoc />
    protected override string GetDefaultMessageTemplate(string errorCode)
    {
        return "too_many_files";
    }
}
