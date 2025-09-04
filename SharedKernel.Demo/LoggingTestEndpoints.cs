using System.Text;
using FluentMinimalApiMapper;
using Microsoft.AspNetCore.Mvc;

namespace SharedKernel.Demo;

public class LoggingTestEndpoints : IEndpoint
{
   public void AddRoutes(IEndpointRouteBuilder app)
   {
      var grp = app.MapGroup("/tests")
                   .WithTags("logs");

      grp.MapPost("/json", ([FromBody] TestTypes payload) => Results.Ok(payload));

      grp.MapPost("/json-array",
         ([FromBody] int[] numbers) => Results.Ok(new
         {
            count = numbers?.Length ?? 0,
            numbers
         }));

      grp.MapPost("/form-urlencoded", ([FromForm] FormUrlDto form) => Results.Ok(form))
         .DisableAntiforgery();

      grp.MapGet("/json-per-property",
         () =>
         {
            var big = new string('x', 6_000); // ~6KB
            var payload = new
            {
               small = "ok",
               bigString = big, // should be redacted/omitted per-property
               tail = "done"
            };
            return Results.Json(payload);
         });


      grp.MapPost("/multipart",
            async ([FromForm] MultipartDto form) =>
            {
               var meta = new
               {
                  form.Description,
                  File = form.File is null
                     ? null
                     : new
                     {
                        form.File.FileName,
                        form.File.ContentType,
                        form.File.Length
                     }
               };
               return Results.Ok(meta);
            })
         .DisableAntiforgery();

      grp.MapGet("/query", ([AsParameters] QueryDto q) => Results.Ok(q));

      grp.MapGet("/route/{id:int}",
         (int id) => Results.Ok(new
         {
            id
         }));

      grp.MapPost("/headers",
         ([FromHeader(Name = "x-trace-id")] string? traceId, HttpRequest req) =>
         {
            var hasAuth = req.Headers.ContainsKey("Authorization");
            return Results.Ok(new
            {
               traceId,
               hasAuth
            });
         });

      grp.MapGet("/binary",
         () =>
         {
            var bytes = Encoding.UTF8.GetBytes("Hello Binary");
            return Results.File(bytes, "application/octet-stream", "demo.bin");
         });

      grp.MapGet("/no-content-type",
         async (HttpContext ctx) =>
         {
            await ctx.Response.Body.WriteAsync(Encoding.UTF8.GetBytes("raw body with no content-type"));
         });

      grp.MapGet("/large-json",
         () =>
         {
            var items = Enumerable.Range(1, 10_000)
                                  .Select(i => new
                                  {
                                     i,
                                     text = "xxxxxxxxxx"
                                  });
            return Results.Ok(items);
         });

      grp.MapGet("/large-text",
         () =>
         {
            var sb = new StringBuilder();
            for (var i = 0; i < 20_000; i++) sb.Append('x');
            return Results.Text(sb.ToString(), "text/plain", Encoding.UTF8);
         });

      grp.MapPost("/echo-with-headers",
         ([FromBody] SharedKernel.Demo.TestTypes payload, HttpResponse res) =>
         {
            res.Headers["Custom-Header-Response"] = "CustomValue";
            res.ContentType = "application/json; charset=utf-8";
            return Results.Json(payload);
         });

      grp.MapGet("/ping", () => Results.Text("pong", "text/plain"));
      

      grp.MapGet("/invalid-json",
         async (HttpContext ctx) =>
         {
            ctx.Response.StatusCode = StatusCodes.Status200OK;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.Body.WriteAsync("{ invalid-json: true"u8.ToArray());
         });

      // Your HttpClient test moved here, targets our echo endpoint
      grp.MapGet("/httpclient",
         async (IHttpClientFactory httpClientFactory) =>
         {
            var httpClient = httpClientFactory.CreateClient("RandomApiClient");
            httpClient.DefaultRequestHeaders.Add("auth", "hardcoded-auth-value");

            var body = new SharedKernel.Demo.TestTypes
            {
               AnimalType = AnimalType.Cat,
               JustText = "Hello from Get Data",
               JustNumber = 100
            };

            var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(body),
               System.Text.Encoding.UTF8,
               "application/json");

            var response = await httpClient.PostAsync("tests/echo-with-headers?barev=5", content);

            if (!response.IsSuccessStatusCode)
               throw new Exception("Something went wrong");

            var responseBody = await response.Content.ReadAsStringAsync();
            var testTypes = System.Text.Json.JsonSerializer.Deserialize<SharedKernel.Demo.TestTypes>(responseBody);

            if (testTypes == null)
               throw new Exception("Failed to get data from external API");

            return TypedResults.Ok(testTypes);
         });
   }
}

public record FormUrlDto(string? Username, string? Password, string? Note);

public class MultipartDto
{
   public IFormFile? File { get; init; }
   public string? Description { get; init; }
}

public record QueryDto(int Page = 1, int PageSize = 10, string? Search = null, string[]? Tags = null);