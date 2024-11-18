namespace SharedKernel.OpenApi.Options;

internal class Document
{
   public required string Title { get; set; }
   public required string Description { get; set; }
   public required string GroupName { get; set; }
   public required string Version { get; set; }
   public bool ForExternalUse { get; set; }
}