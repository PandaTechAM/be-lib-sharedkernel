namespace SharedKernel.ValidatorAndMediatR.Validators.Files;

/// <summary>
///     Predefined sets of allowed file extensions for common upload scenarios.
/// </summary>
public static class CommonFileSets
{
    /// <summary>
    ///     Raster and vector image extensions, including animated formats like GIF.
    /// </summary>
    public static readonly string[] ImagesAndAnimations =
        [".jpg", ".jpeg", ".png", ".gif", ".webp", ".heic", ".heif", ".svg", ".avif"];

    /// <summary>
    ///     Common office and data document extensions (PDF, text, spreadsheets, presentations, etc.).
    /// </summary>
    public static readonly string[] Documents =
    [
        ".pdf", ".txt", ".csv", ".json", ".xml", ".yaml", ".yml", ".md", ".docx", ".xlsx", ".pptx", ".odt", ".ods",
        ".odp"
    ];

    /// <summary>
    ///     Static and vector image extensions, excluding animated formats like GIF.
    /// </summary>
    public static readonly string[] Images = [".jpg", ".jpeg", ".png", ".webp", ".heic", ".heif", ".svg", ".avif"];

    /// <summary>
    ///     Extensions accepted for bulk data import (CSV and Excel).
    /// </summary>
    public static readonly string[] ImportFiles = [".csv", ".xlsx"];

    /// <summary>
    ///     Combined set of <see cref="Images" /> and <see cref="Documents" /> extensions.
    /// </summary>
    public static readonly string[] ImagesAndDocuments = Images.Concat(Documents)
        .ToArray();
}
