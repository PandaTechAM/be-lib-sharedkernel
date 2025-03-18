using DistributedCache.Extensions;
using DistributedCache.Options;
using FluentMinimalApiMapper;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Demo2;
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
   // .ConfigureWithPandaVault()
   .AddSerilog(LogBackend.Loki)
   .AddResponseCrafter(NamingConvention.ToSnakeCase)
   .AddOpenApi()
   .AddOpenTelemetry()
   .AddMinimalApis(AssemblyRegistry.ToArray())
   .AddControllers(AssemblyRegistry.ToArray())
   .AddMediatrWithBehaviors(AssemblyRegistry.ToArray())
   .AddResilienceDefaultPipeline()
   .AddDistributedSignalR("localhost:6379", "app_name:") // or .AddSignalR()
   .AddDistributedCache(o =>
   {
      o.RedisConnectionString = "localhost:6379";
      o.ChannelPrefix = "app_name:";
   })
   .MapDefaultTimeZone()
   .AddCors()
   .AddOutboundLoggingHandler()
   .AddHealthChecks();



builder.Services
       .AddHttpClient("RandomApiClient",
          client =>
          {
             client.BaseAddress = new Uri("http://localhost");
          })
       .AddOutboundLoggingHandler();


var app = builder.Build();

app
   .UseRequestLogging()
   .UseResponseCrafter()
   .UseCors()
   .MapMinimalApis()
   .MapHealthCheckEndpoints()
   .MapPrometheusExporterEndpoints()
   .EnsureHealthy()
   .ClearAssemblyRegistry()
   .UseOpenApi()
   .MapControllers();


app.MapPost("/params", ([AsParameters] TestTypes testTypes) => TypedResults.Ok(testTypes));
app.MapPost("/body", ([FromBody] TestTypes testTypes) => TypedResults.Ok(testTypes));
app.MapGet("/hello", () => TypedResults.Ok("Hello World!"));

app.MapGet("/get-data",
   async (IHttpClientFactory httpClientFactory) =>
   {
      var httpClient = httpClientFactory.CreateClient("RandomApiClient");
      httpClient.DefaultRequestHeaders.Add("auth", "hardcoded-auth-value");
      var response = await httpClient.GetFromJsonAsync<object>("hello");

      return response;
   });

app.LogStartSuccess();
app.Run();

namespace SharedKernel.Demo2
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