using FluentMinimalApiMapper;
using Microsoft.AspNetCore.Mvc;

namespace SharedKernel.Demo;

/// <summary>
///    Endpoints designed to verify OpenAPI 3.1 schema generation in Swagger and Scalar.
///    Key scenarios: nullable enums, nullable primitives, mixed nullability in query/body/response.
/// </summary>
public class OpenApiTestEndpoints : IEndpoint
{
   public void AddRoutes(IEndpointRouteBuilder app)
   {
      var grp = app.MapGroup("/openapi-tests")
                   .WithTags("OpenAPI 3.1 Tests");

      // ── Nullable enum in query ─────────────────────────────────────────
      // In 3.0: nullable enum couldn't show string values properly
      // In 3.1: type: ["string", "null"] + enum descriptions should render

      grp.MapGet("/search",
            (AnimalType? type, PriorityLevel? priority, string? name, int? minAge) =>
               TypedResults.Ok(new AnimalSearchResult
               {
                  Type = type,
                  Priority = priority,
                  Name = name ?? "any",
                  MinAge = minAge,
                  Count = 42
               }))
         .WithSummary("Nullable enums + nullable primitives in query params");

      // ── Nullable enum in request body ──────────────────────────────────

      grp.MapPost("/animals",
            ([FromBody] AnimalCreateRequest req) =>
               TypedResults.Ok(new AnimalResponse
               {
                  Id = 1,
                  Name = req.Name,
                  Type = req.Type,
                  Priority = req.Priority,
                  NickName = req.NickName,
                  Weight = req.Weight,
                  BirthDate = req.BirthDate,
                  IsVaccinated = req.IsVaccinated
               }))
         .WithSummary("Nullable enums + nullable types in request/response body");

      // ── Non-nullable vs nullable enum comparison ───────────────────────
      // Both should show string descriptions; nullable should indicate optional

      grp.MapPut("/animals/{id}",
            (int id, [FromBody] AnimalUpdateRequest req) =>
               TypedResults.Ok(new { id, req.Name, req.Type, req.Priority }))
         .WithSummary("Required enum vs nullable enum side by side");

      // ── Nullable primitives in query (3.1 type arrays) ─────────────────
      // 3.1 represents these as type: ["integer", "null"] instead of nullable: true

      grp.MapGet("/filter",
            (DateOnly? from, DateOnly? to, int? limit, decimal? minWeight, bool? active) =>
               TypedResults.Ok(new { from, to, limit, minWeight, active }))
         .WithSummary("Various nullable primitive types in query params");

      // ── Response with all-nullable fields ──────────────────────────────

      grp.MapGet("/animals/{id}",
            (int id) => TypedResults.Ok(new AnimalResponse
            {
               Id = id,
               Name = "Buddy",
               Type = AnimalType.Dog,
               Priority = null,
               NickName = null,
               Weight = 25.5m,
               BirthDate = new DateOnly(2020, 3, 15),
               IsVaccinated = true
            }))
         .WithSummary("Response with mix of null and non-null optional fields");

      // ── Enum-only endpoint ─────────────────────────────────────────────
      // Quick check that non-nullable enums still render descriptions

      grp.MapGet("/priorities",
            () => TypedResults.Ok(Enum.GetValues<PriorityLevel>()
               .Select(p => new { value = p, numeric = (int)p })))
         .WithSummary("Returns all PriorityLevel enum values");
   }
}

// ── DTOs ─────────────────────────────────────────────────────────────────────

public class AnimalCreateRequest
{
   public required string Name { get; init; }
   public AnimalType? Type { get; init; }
   public PriorityLevel? Priority { get; init; }
   public string? NickName { get; init; }
   public decimal? Weight { get; init; }
   public DateOnly? BirthDate { get; init; }
   public bool? IsVaccinated { get; init; }
}

public class AnimalUpdateRequest
{
   public required string Name { get; init; }
   public required AnimalType Type { get; init; }
   public PriorityLevel? Priority { get; init; }
}

public class AnimalResponse
{
   public int Id { get; init; }
   public required string Name { get; init; }
   public AnimalType? Type { get; init; }
   public PriorityLevel? Priority { get; init; }
   public string? NickName { get; init; }
   public decimal? Weight { get; init; }
   public DateOnly? BirthDate { get; init; }
   public bool? IsVaccinated { get; init; }
}

public class AnimalSearchResult
{
   public AnimalType? Type { get; init; }
   public PriorityLevel? Priority { get; init; }
   public required string Name { get; init; }
   public int? MinAge { get; init; }
   public int Count { get; init; }
}

/// <summary>
///    Second enum to verify the nullable enum schema fix works across multiple enum types.
/// </summary>
public enum PriorityLevel
{
   Low = 0,
   Medium = 1,
   High = 2,
   Critical = 3
}

// ── Shared DTOs used by multiple endpoint classes ────────────────────────────

public class TestTypes
{
   public AnimalType AnimalType { get; set; } = AnimalType.Dog;
   public required string JustText { get; set; } = "Hello";
   public int JustNumber { get; set; } = 42;
   public string? NullableText { get; set; }
}

public enum AnimalType
{
   Dog,
   Cat,
   Fish
}
