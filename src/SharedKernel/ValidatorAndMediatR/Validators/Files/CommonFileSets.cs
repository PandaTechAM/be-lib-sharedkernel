namespace SharedKernel.ValidatorAndMediatR.Validators.Files;

public static class CommonFileSets
{
   public static readonly string[] ImagesAndAnimations =
      [".jpg", ".jpeg", ".png", ".gif", ".webp", ".heic", ".heif", ".svg", ".avif"];

   public static readonly string[] Documents =
   [
      ".pdf", ".txt", ".csv", ".json", ".xml", ".yaml", ".yml", ".md", ".docx", ".xlsx", ".pptx", ".odt", ".ods", ".odp"
   ];

   public static readonly string[] Images = [".jpg", ".jpeg", ".png", ".webp", ".heic", ".heif", ".svg", ".avif"];

   public static readonly string[] ImportFiles = [".csv", ".xlsx"];

   public static readonly string[] ImagesAndDocuments = Images.Concat(Documents)
                                                              .ToArray();
}