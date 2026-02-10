using System.Text.Json.Serialization;
using DistributedCache.Extensions;
using FileExporter.Extensions;
using FluentMinimalApiMapper;
using GridifyExtensions.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ResponseCrafter.Enums;
using ResponseCrafter.Extensions;
using SharedKernel.Demo;
using SharedKernel.Demo.Context;
using SharedKernel.Extensions;
using SharedKernel.Helpers;
using SharedKernel.Logging;
using SharedKernel.Logging.Middleware;
using SharedKernel.Maintenance;
using SharedKernel.OpenApi;
using SharedKernel.Resilience;
using SharedKernel.ValidatorAndMediatR;

var builder = WebApplication.CreateBuilder(args);

builder.LogStartAttempt();
AssemblyRegistry.Add(typeof(Program).Assembly);

builder
   // .ConfigureWithPandaVault()
   .AddSerilog(LogBackend.ElasticSearch)
   .AddResponseCrafter(NamingConvention.ToSnakeCase)
   .AddOpenApi()
   .AddMaintenanceMode()
   .AddOpenTelemetry()
   .AddMinimalApis(AssemblyRegistry.ToArray())
   .AddControllers(AssemblyRegistry.ToArray())
   .AddMediatrWithBehaviors(AssemblyRegistry.ToArray())
   .AddResilienceDefaultPipeline()
   .AddDistributedSignalR("localhost:6379", "app_name") // or .AddSignalR()
   .AddDistributedCache(o =>
   {
      o.RedisConnectionString = "localhost:6379";
      o.ChannelPrefix = "app_name";
   })
   .AddMassTransit(AssemblyRegistry.ToArray())
   .AddFileExporter(AssemblyRegistry.ToArray())
   .MapDefaultTimeZone()
   .AddCors()
   .AddOutboundLoggingHandler()
   .AddHealthChecks();


builder.Services.ConfigureHttpJsonOptions(options =>
{
   options.SerializerOptions.PropertyNamingPolicy = null;
});


builder.Services
       .AddHttpClient("RandomApiClient",
          client =>
          {
             client.DefaultRequestHeaders.Add("RequestCustomHeader", "CustomValue");
             client.BaseAddress = new Uri("http://localhost");
          })
       .AddOutboundLoggingHandler();

builder.UseSqlLiteInMemory();

var app = builder.Build();


app
   .UseRequestLogging()
   .UseMaintenanceMode()
   .UseResponseCrafter()
   .UseCors()
   .MapMinimalApis()
   .MapHealthCheckEndpoints()
   .MapPrometheusExporterEndpoints()
   .EnsureHealthy()
   .ClearAssemblyRegistry()
   .UseOpenApi()
   .MapControllers();

app.CreateInMemoryDb();

app.MapMaintenanceEndpoint();


app.MapGet("/outbox-count",
   async (InMemoryContext db) =>
   {
      var cnt = await db.OutboxMessages.CountAsync();
      return TypedResults.Ok(new
      {
         count = cnt
      });
   });

app.MapPost("/receive-file", ([FromForm] IFormFile file) => TypedResults.Ok())
   .DisableAntiforgery();

app.MapPost("/params", ([AsParameters] TestTypes testTypes) => TypedResults.Ok(testTypes));

app.MapPost("/body",
   ([FromBody] TestTypes testTypes, HttpContext httpContext) =>
   {
      httpContext.Response.ContentType = "application/json";
      httpContext.Response.Headers.Append("Custom-Header-Response", "CustomValue");

      return TypedResults.Ok(testTypes);
   });

app.MapGet("test-query", ([FromQuery] long id) => TypedResults.Ok(id));

app.MapHub<MessageHub>("/hub");

app.LogStartSuccess();
app.Run();

namespace SharedKernel.Demo
{
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
}