using FluentValidation;
using FluentValidation.Validators;
using Microsoft.AspNetCore.Http;

namespace SharedKernel.ValidatorAndMediatR.Validators.Files;

/// <summary>
///     Validates that a single uploaded file has one of the allowed extensions.
/// </summary>
/// <param name="allowedExts">The file extensions permitted for the property.</param>
public sealed class FileExtensionValidator<T>(params string[] allowedExts) : PropertyValidator<T, IFormFile?>
{
    private readonly HashSet<string> _allowed = new(allowedExts.Select(Exts.Norm), StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public override string Name => "FileExtension";

    /// <inheritdoc />
    public override bool IsValid(ValidationContext<T> context, IFormFile? value)
    {
        if (value is null)
        {
            return true;
        }

        var ext = Exts.GetExt(value.FileName);
        if (string.IsNullOrEmpty(ext))
        {
            context.AddFailure("File has no extension.");
            return false;
        }

        if (_allowed.Count == 0 || _allowed.Contains(ext))
        {
            return true;
        }

        context.AddFailure($"File '{value.FileName}' type is not allowed. Allowed: {string.Join(", ", _allowed)}");
        return false;
    }

    /// <inheritdoc />
    protected override string GetDefaultMessageTemplate(string errorCode)
    {
        return "file_type_not_allowed";
    }
}
