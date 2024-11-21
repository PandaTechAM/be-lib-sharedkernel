using FluentMinimalApiMapper;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Demo;
using ResponseCrafter.Enums;
using ResponseCrafter.Extensions;
using SharedKernel.Extensions;
using SharedKernel.Helpers;
using SharedKernel.Logging;
using SharedKernel.OpenApi;
using SharedKernel.Resilience;
using SharedKernel.ValidatorAndMediatR;

var builder = WebApplication.CreateBuilder(args);

builder.LogStartAttempt();
AssemblyRegistry.Add(typeof(Program).Assembly);

builder
   .AddPandaVault()
   .AddSerilog()
   .AddResponseCrafter(NamingConvention.ToSnakeCase)
   .AddOpenApi()
   .AddOpenTelemetry()
   .AddEndpoints(AssemblyRegistry.ToArray())
   .AddControllers(AssemblyRegistry.ToArray())
   .AddMediatrWithBehaviors(AssemblyRegistry.ToArray())
   .AddResilienceDefaultPipeline()
   .MapDefaultTimeZone()
   .AddCors();

builder.Services.AddHealthChecks();


var app = builder.Build();

app
   .UseRequestResponseLogging()
   .UseResponseCrafter()
   .UseCors()
   .MapEndpoints()
   .MapDefaultEndpoints()
   .EnsureHealthy()
   .ClearAssemblyRegistry()
   .UseOpenApi();

app.MapPost("/params", ([AsParameters] TestTypes testTypes) => TypedResults.Ok(testTypes));
app.MapPost("/body", ([FromBody] TestTypes testTypes) => TypedResults.Ok(testTypes));


app.LogStartSuccess();
app.Run();

namespace SharedKernel.Demo
{
   public class TestTypes
   {
      public AnimalType AnimalType { get; set; } = AnimalType.Dog;
      public required string JustText { get; set; } = "Hello";
      public int JustNumber { get; set; } = 42;
   }

   public enum AnimalType
   {
      Dog,
      Cat,
      Fish
   }
}