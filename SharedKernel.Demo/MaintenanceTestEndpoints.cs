using FluentMinimalApiMapper;

namespace SharedKernel.Demo;

public class MaintenanceTestEndpoints : IEndpoint
{
   public void AddRoutes(IEndpointRouteBuilder app)
   {
      var grp = app.MapGroup("/")
                   .WithTags("maintenance");


      grp.MapGet("/api/admin/v1/test", () => Results.Ok("ok"));
      grp.MapGet("/api/admin/v2/test", () => Results.Ok("ok"));
      grp.MapGet("/api/integration/v1/test", () => Results.Ok("ok"));
   }
}