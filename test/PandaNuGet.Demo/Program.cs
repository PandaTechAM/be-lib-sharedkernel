using Microsoft.AspNetCore.Mvc;
using PandaNuGet.Demo;
using SharedKernel.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();

builder.AddOpenApi();

var app = builder.Build();

app.UseOpenApi();

app.MapPost("/params", ([AsParameters] TestTypes testTypes) => TypedResults.Ok(testTypes));
app.MapPost("/body", ([FromBody] TestTypes testTypes) => TypedResults.Ok(testTypes));

app.Run();

namespace PandaNuGet.Demo
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