namespace SharedKernel.OpenApi.Options;

internal class OpenApiConfig
{
   public List<string> DisabledEnvironments { get; init; } = [];
   public List<SecuritySchema> SecuritySchemes { get; init; } = [];
   public List<Document> Documents { get; init; } = [];
   public required Contact Contact { get; init; }
}