using DistributedCache.Extensions;
using FluentMinimalApiMapper;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Demo2;
using ResponseCrafter.Enums;
using ResponseCrafter.Extensions;
using SharedKernel.Demo;
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

app.MapPost("user", async ([FromBody] UserCommand user, ISender sender) =>
{
   await sender.Send(user);
   return Results.Ok();
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

app.MapGet("/get-data",
   async (IHttpClientFactory httpClientFactory) =>
   {
      var httpClient = httpClientFactory.CreateClient("RandomApiClient");
      httpClient.DefaultRequestHeaders.Add("auth", "hardcoded-auth-value");

      var body = new TestTypes
      {
         AnimalType = AnimalType.Cat,
         JustText = "Hello from Get Data",
         JustNumber = 100
      };
      var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(body),
         System.Text.Encoding.UTF8,
         "application/json");

      var response = await httpClient.PostAsync("body", content);

      if (!response.IsSuccessStatusCode)
      {
         throw new Exception("Something went wrong");
      }

      var responseBody = await response.Content.ReadAsStringAsync();

      var testTypes = System.Text.Json.JsonSerializer.Deserialize<TestTypes>(responseBody);

      if (testTypes == null)
      {
         throw new Exception("Failed to get data from external API");
      }

      return TypedResults.Ok(testTypes);
   });

app.MapHub<MessageHub>("/hub");

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

public record UserCommand(string Name, string Email) : ICommand<string>;

public class UserCommandHandler : ICommandHandler<UserCommand, string>
{
   public Task<string> Handle(UserCommand request, CancellationToken cancellationToken)
   {
      return Task.FromResult($"User {request.Name} with email {request.Email} created successfully.");
   }
}

public class User
{
   public string Name { get; set; } = string.Empty;
   public string Email { get; set; } = string.Empty;
}

public class UserValidator : AbstractValidator<UserCommand>
{
   public UserValidator()
   {
      RuleFor(x => x.Name)
         .NotEmpty()
         .WithMessage("Name is required.")
         .MaximumLength(100)
         .WithMessage("Name cannot exceed 100 characters.");

      RuleFor(x => x.Email)
         .NotEmpty()
         .WithMessage("Email is required.")
         .EmailAddress()
         .WithMessage("Invalid email format.");
   }
}