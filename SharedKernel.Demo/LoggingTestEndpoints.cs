using System.Text;
using System.Text.Json;
using FluentMinimalApiMapper;
using Microsoft.AspNetCore.Mvc;

namespace SharedKernel.Demo;

public class LoggingTestEndpoints : IEndpoint
{
   public void AddRoutes(IEndpointRouteBuilder app)
   {
      var grp = app.MapGroup("/tests")
                   .WithTags("Logging Tests");

      // ═══════════════════════════════════════════════════════════════════
      // INBOUND REQUEST TESTS
      // ═══════════════════════════════════════════════════════════════════

      grp.MapPost("/json", ([FromBody] TestTypes payload) => Results.Ok(payload))
         .WithSummary("JSON body - should log request and response bodies");

      grp.MapPost("/json-array", ([FromBody] int[] numbers) => Results.Ok(new { count = numbers.Length, numbers }))
         .WithSummary("JSON array body");

      grp.MapPost("/form-urlencoded", ([FromForm] FormUrlDto form) => Results.Ok(form))
         .DisableAntiforgery()
         .WithSummary("Form URL encoded - should log form fields");

      grp.MapPost("/multipart", async ([FromForm] MultipartDto form, CancellationToken ct) =>
         {
            var meta = new
            {
               form.Description,
               File = form.File is null ? null : new
               {
                  form.File.FileName,
                  form.File.ContentType,
                  form.File.Length
               }
            };
            return Results.Ok(meta);
         })
         .DisableAntiforgery()
         .WithSummary("Multipart form - files should show [OMITTED: file XKB]");

      grp.MapGet("/query", ([AsParameters] QueryDto q) => Results.Ok(q))
         .WithSummary("Query string parameters - should appear in Query scope field");

      grp.MapGet("/route/{id:int}", (int id) => Results.Ok(new { id }))
         .WithSummary("Route parameter test");

      // ═══════════════════════════════════════════════════════════════════
      // RESPONSE SIZE/TYPE TESTS
      // ═══════════════════════════════════════════════════════════════════

      grp.MapGet("/large-json", () =>
         {
            var items = Enumerable.Range(1, 10_000).Select(i => new { i, text = "xxxxxxxxxx" });
            return Results.Ok(items);
         })
         .WithSummary("Large JSON response - should show [OMITTED: exceeds-limit]");

      grp.MapGet("/large-text", () =>
         {
            var text = new string('x', 20_000);
            return Results.Text(text, "text/plain", Encoding.UTF8);
         })
         .WithSummary("Large text response - should show [OMITTED: exceeds-limit]");

      grp.MapGet("/binary", () =>
         {
            var bytes = Encoding.UTF8.GetBytes("Hello Binary");
            return Results.File(bytes, "application/octet-stream", "demo.bin");
         })
         .WithSummary("Binary response - should show [OMITTED: non-text]");

      grp.MapGet("/no-content-type", async ctx =>
         {
            await ctx.Response.Body.WriteAsync("raw body with no content-type"u8.ToArray());
         })
         .WithSummary("No content-type header - body logging behavior");

      grp.MapGet("/invalid-json", async ctx =>
         {
            ctx.Response.StatusCode = StatusCodes.Status200OK;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.Body.WriteAsync("{ invalid-json: true"u8.ToArray());
         })
         .WithSummary("Invalid JSON - should show invalidJson: true in logs");

      grp.MapGet("/json-per-property", () =>
         {
            var big = new string('x', 6_000);
            return Results.Json(new { small = "ok", bigString = big, tail = "done" });
         })
         .WithSummary("Large property value - should show [OMITTED: exceeds-limit ~XKB]");

      grp.MapGet("/ping", () => Results.Text("pong", "text/plain"))
         .WithSummary("Simple ping - minimal logging");

      grp.MapGet("/empty", () => Results.NoContent())
         .WithSummary("204 No Content - empty response body");

      // ═══════════════════════════════════════════════════════════════════
      // HEADER REDACTION TESTS
      // ═══════════════════════════════════════════════════════════════════

      grp.MapPost("/headers", ([FromHeader(Name = "X-Trace-Id")] string? traceId,
                               [FromHeader(Name = "Authorization")] string? auth,
                               [FromHeader(Name = "X-Api-Token")] string? token,
                               HttpRequest req) =>
         {
            return Results.Ok(new
            {
               traceId,
               hasAuth = !string.IsNullOrEmpty(auth),
               hasToken = !string.IsNullOrEmpty(token),
               cookieCount = req.Cookies.Count
            });
         })
         .WithSummary("Header redaction - Authorization, Token, Cookie should be [REDACTED]");

      grp.MapPost("/echo-with-headers", ([FromBody] TestTypes payload, HttpResponse res) =>
         {
            res.Headers["X-Custom-Response"] = "CustomValue";
            res.Headers["X-Auth-Token"] = "secret-token-value";
            res.ContentType = "application/json; charset=utf-8";
            return Results.Json(payload);
         })
         .WithSummary("Response headers - X-Auth-Token should be [REDACTED]");

      // ═══════════════════════════════════════════════════════════════════
      // SENSITIVE DATA REDACTION TESTS
      // ═══════════════════════════════════════════════════════════════════

      grp.MapPost("/login", ([FromBody] LoginDto login) => Results.Ok(new { success = true, user = login.Username }))
         .WithSummary("Login - password field should be [REDACTED]");

      grp.MapPost("/payment", ([FromBody] PaymentDto payment) =>
            Results.Ok(new { success = true, last4 = payment.Pan?[^4..] }))
         .WithSummary("Payment - pan, cvv fields should be [REDACTED]");

      // ═══════════════════════════════════════════════════════════════════
      // OUTBOUND HTTP CLIENT TESTS (tests OutboundLoggingHandler)
      // ═══════════════════════════════════════════════════════════════════

      grp.MapGet("/outbound/json", async (IHttpClientFactory factory) =>
         {
            var client = factory.CreateClient("RandomApiClient");
            var body = new TestTypes { AnimalType = AnimalType.Cat, JustText = "Outbound JSON", JustNumber = 42 };
            var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("tests/echo-with-headers", content);
            return Results.Ok(await response.Content.ReadFromJsonAsync<TestTypes>());
         })
         .WithSummary("Outbound JSON request - should log request/response bodies");

      grp.MapGet("/outbound/form-urlencoded", async (IHttpClientFactory factory) =>
         {
            var client = factory.CreateClient("RandomApiClient");

            var formContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
               ["Username"] = "testuser",        // Match FormUrlDto fields!
               ["Password"] = "secret123",
               ["Note"] = "test note"
            });

            var response = await client.PostAsync("tests/form-urlencoded", formContent);
            var body = await response.Content.ReadAsStringAsync();
      
            return Results.Json(new { 
               statusCode = (int)response.StatusCode,
               body 
            });
         })
         .WithSummary("Outbound FormUrlEncodedContent");
      
      grp.MapGet("/outbound/form-as-string", async (IHttpClientFactory factory) =>
         {
            var client = factory.CreateClient("RandomApiClient");
      
            // Use FormUrlEncodedContent instead of StringContent for proper form encoding
            var formContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
               ["grant_type"] = "password",
               ["username"] = "testuser",
               ["password"] = "secret123",
               ["scope"] = "openid"
            });

            var response = await client.PostAsync("tests/form-urlencoded", formContent);
      
            // Handle non-success responses gracefully
            if (!response.IsSuccessStatusCode)
            {
               var errorBody = await response.Content.ReadAsStringAsync();
               return Results.Json(new { 
                  success = false, 
                  statusCode = (int)response.StatusCode,
                  error = errorBody 
               });
            }
      
            return Results.Ok(await response.Content.ReadFromJsonAsync<FormUrlDto>());
         })
         .WithSummary("Outbound form-urlencoded - now using FormUrlEncodedContent for proper binding");


      grp.MapGet("/outbound/multipart", async (IHttpClientFactory factory) =>
         {
            var client = factory.CreateClient("RandomApiClient");

            // Create multipart content
            var content = new MultipartFormDataContent();
            content.Add(new StringContent("Test description"), "Description");
            content.Add(new ByteArrayContent("fake file content"u8.ToArray()), "File", "test.txt");

            var response = await client.PostAsync("tests/multipart", content);
      
            if (!response.IsSuccessStatusCode)
            {
               var errorBody = await response.Content.ReadAsStringAsync();
               return Results.Json(new { 
                  success = false, 
                  statusCode = (int)response.StatusCode,
                  error = errorBody 
               });
            }
      
            return Results.Ok(await response.Content.ReadAsStringAsync());
         })
         .WithSummary("Outbound multipart - should show [field] and [OMITTED: file]");

      grp.MapGet("/outbound/large", async (IHttpClientFactory factory) =>
         {
            var client = factory.CreateClient("RandomApiClient");
            var response = await client.GetAsync("tests/large-json");
            return Results.Ok(new { statusCode = (int)response.StatusCode });
         })
         .WithSummary("Outbound large response - should show [OMITTED: exceeds-limit]");

      grp.MapGet("/outbound/binary", async (IHttpClientFactory factory) =>
         {
            var client = factory.CreateClient("RandomApiClient");
            var response = await client.GetAsync("tests/binary");
            return Results.Ok(new { statusCode = (int)response.StatusCode, length = response.Content.Headers.ContentLength });
         })
         .WithSummary("Outbound binary response - should show [OMITTED: non-text]");

      // ═══════════════════════════════════════════════════════════════════
      // EDGE CASES
      // ═══════════════════════════════════════════════════════════════════

      grp.MapPost("/chunked", async (HttpRequest req) =>
         {
            using var reader = new StreamReader(req.Body);
            var body = await reader.ReadToEndAsync();
            return Results.Ok(new { length = body.Length });
         })
         .WithSummary("Chunked transfer encoding test");

      grp.MapGet("/slow", async (CancellationToken ct) =>
         {
            await Task.Delay(2000, ct);
            return Results.Ok(new { delayed = true });
         })
         .WithSummary("Slow endpoint - check ElapsedMs in logs");
   }
}

// ═══════════════════════════════════════════════════════════════════
// DTOs
// ═══════════════════════════════════════════════════════════════════

public record FormUrlDto(string? Username, string? Password, string? Note);

public class MultipartDto
{
   public IFormFile? File { get; init; }
   public string? Description { get; init; }
}

public record QueryDto(int Page = 1, int PageSize = 10, string? Search = null, string[]? Tags = null);

public record LoginDto(string Username, string Password, bool RememberMe = false);

public record PaymentDto(string? Pan, string? Cvv, string? CardholderName, decimal Amount);
