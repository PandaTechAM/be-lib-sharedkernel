namespace SharedKernel.ValidatorAndMediatR.Validators.Files;

internal static class Exts
{
   public static string Norm(string ext)
   {
      if (string.IsNullOrWhiteSpace(ext))
      {
         return "";
      }

      return ext.StartsWith('.') ? ext.ToLowerInvariant() : "." + ext.ToLowerInvariant();
   }

   public static string GetExt(string fileName) => Norm(Path.GetExtension(fileName));
}