# Pandatech.SharedKernel

Opinionated ASP.NET Core 10 infrastructure library for PandaTech projects. It consolidates logging, OpenAPI,
validation, CORS, SignalR, telemetry, health checks, maintenance mode, and resilience into a single package so every
service starts from the same baseline.

The package is publicly available but is designed for internal use. If you want to adopt it, fork the repository and
customize it to your own conventions.

Requires **.NET 10.0**. Uses C# 14 extension members throughout and cannot be downgraded to earlier TFMs.

---

## Table of Contents

1. [Installation](#installation)
2. [Quick Start](#quick-start)
3. [Assembly Registry](#assembly-registry)
4. [OpenAPI](#openapi)
5. [Logging](#logging)
6. [MediatR and FluentValidation](#mediatr-and-fluentvalidation)
7. [CORS](#cors)
8. [Resilience Pipelines](#resilience-pipelines)
9. [Controllers](#controllers)
10. [SignalR](#signalr)
11. [OpenTelemetry](#opentelemetry)
12. [Health Checks](#health-checks)
13. [Maintenance Mode](#maintenance-mode)
14. [Utilities](#utilities)

---

## Installation

```bash
dotnet add package Pandatech.SharedKernel
```

---

## Quick Start

A complete `Program.cs` using every major feature:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.LogStartAttempt();
AssemblyRegistry.Add(typeof(Program).Assembly);

builder
   .ConfigureWithPandaVault()
   .AddSerilog(LogBackend.ElasticSearch)
   .AddResponseCrafter(NamingConvention.ToSnakeCase)
   .AddOpenApi()
   .AddMaintenanceMode()
   .AddOpenTelemetry()
   .AddMinimalApis(AssemblyRegistry.ToArray())
   .AddControllers(AssemblyRegistry.ToArray())
   .AddMediatrWithBehaviors(AssemblyRegistry.ToArray())
   .AddResilienceDefaultPipeline()
   .AddDistributedSignalR("localhost:6379", "app_name")
   .AddCors()
   .AddOutboundLoggingHandler()
   .AddHealthChecks();

var app = builder.Build();

app
   .UseRequestLogging()
   .UseMaintenanceMode()
   .UseResponseCrafter()
   .UseCors()
   .MapMinimalApis()
   .EnsureHealthy()
   .MapHealthCheckEndpoints()
   .MapPrometheusExporterEndpoints()
   .ClearAssemblyRegistry()
   .UseOpenApi()
   .MapControllers();

app.LogStartSuccess();
app.Run();
```

---

## Assembly Registry

`AssemblyRegistry` is a thread-safe static list used to pass your project's assemblies from the builder phase to the
app phase without repeating `typeof(Program).Assembly` everywhere.

```csharp
// Add once at startup
AssemblyRegistry.Add(typeof(Program).Assembly);

// Pass to any method that needs to scan for handlers, validators, or endpoints
builder.AddMediatrWithBehaviors(AssemblyRegistry.ToArray());

// Clear after app is built to free memory — the scanning is complete
app.ClearAssemblyRegistry();
```

---

## OpenAPI

Wraps `Microsoft.AspNetCore.OpenApi` with SwaggerUI and Scalar, supporting multiple API documents, custom security
schemes, and enum string descriptions.

### Registration

```csharp
builder.AddOpenApi();
var app = builder.Build();
app.UseOpenApi();
```

Custom schema transformers can be added via the options callback:

```csharp
builder.AddOpenApi(options =>
{
    options.AddSchemaTransformer<MyCustomTransformer>();
});
```

### Configuration

```json
{
    "OpenApi": {
        "DisabledEnvironments": [
            "Production"
        ],
        "SecuritySchemes": [
            {
                "HeaderName": "Authorization",
                "Description": "Bearer access token."
            },
            {
                "HeaderName": "Client-Type",
                "Description": "Identifies the client type, e.g. '2'."
            }
        ],
        "Documents": [
            {
                "Title": "Admin Panel",
                "Description": "Internal administrative endpoints.",
                "GroupName": "admin-v1",
                "Version": "v1",
                "ForExternalUse": false
            },
            {
                "Title": "Integration",
                "Description": "Public integration endpoints.",
                "GroupName": "integration-v1",
                "Version": "v1",
                "ForExternalUse": true
            }
        ],
        "Contact": {
            "Name": "Pandatech",
            "Url": "https://pandatech.it",
            "Email": "info@pandatech.it"
        }
    }
}
```

### UI URLs

| UI      | URL                       | Notes                                    |
|---------|---------------------------|------------------------------------------|
| Swagger | `/swagger`                | All documents                            |
| Swagger | `/swagger/integration-v1` | External documents only (ForExternalUse) |
| Scalar  | `/scalar/admin-v1`        | One URL per document                     |
| Scalar  | `/scalar/integration-v1`  | One URL per document                     |

`ForExternalUse: true` creates a dedicated Swagger URL you can share with external partners while keeping internal
documents private. All documents still appear on the main `/swagger` page.

---

## Logging

Wraps Serilog with structured output, request/response logging middleware, outbound HTTP logging, and automatic log
cleanup.

### Registration

```csharp
// Synchronous sinks — safe for up to ~1000 req/s per pod
builder.AddSerilog(LogBackend.Loki);

// Asynchronous sinks — better throughput, small risk of losing logs on hard crash
builder.AddSerilog(
    logBackend: LogBackend.ElasticSearch,
    logAdditionalProperties: new Dictionary<string, string>
    {
        ["ServiceName"] = "my-service"
    },
    daysToRetain: 14,
    asyncSinks: true
);
```

### Log Backends

| Value           | Output format                                     |
|-----------------|---------------------------------------------------|
| `None`          | Console only, no file output                      |
| `ElasticSearch` | ECS JSON to file (forward with Filebeat/Logstash) |
| `Loki`          | Loki JSON to file (forward with Promtail)         |
| `CompactJson`   | Compact JSON to file                              |

### Environment behavior

| Environment      | Console | File |
|------------------|---------|------|
| Local            | Yes     | No   |
| Development / QA | Yes     | Yes  |
| Production       | No      | Yes  |

### Configuration

```json
{
    "Serilog": {
        "MinimumLevel": {
            "Default": "Information",
            "Override": {
                "Microsoft": "Information",
                "System": "Information"
            }
        }
    },
    "RepositoryName": "my-service",
    "ConnectionStrings": {
        "PersistentStorage": "/persistence"
    }
}
```

Log files are stored under `{PersistentStorage}/{RepositoryName}/{env}/logs/`. The `LogCleanupHostedService` runs
every 12 hours and deletes files older than `daysToRetain`.

### Request logging middleware

```csharp
app.UseRequestLogging();  // logs method, path, status code, elapsed ms, redacted headers/body
```

Paths under `/swagger`, `/openapi`, `/above-board`, and `/favicon.ico` are silently skipped. Sensitive header names
(auth, token, cookie, pan, cvv, etc.) and matching JSON properties are redacted automatically. Bodies over 16 KB are
omitted.

### Outbound logging

Captures outbound `HttpClient` requests with the same redaction rules:

```csharp
// Register the handler
builder.AddOutboundLoggingHandler();

// Attach to a specific HttpClient
builder.Services
   .AddHttpClient("MyClient", c => c.BaseAddress = new Uri("https://example.com"))
   .AddOutboundLoggingHandler();
```

### Startup logging

```csharp
builder.LogStartAttempt();   // prints banner to console at startup
app.LogStartSuccess();        // prints success banner with elapsed init time
```

---

## MediatR and FluentValidation

Registers MediatR with a validation pipeline behavior that runs all FluentValidation validators before the handler.
Validation failures throw `BadRequestException` from `Pandatech.ResponseCrafter`.

### Registration

```csharp
builder.AddMediatrWithBehaviors(AssemblyRegistry.ToArray());
```

### CQRS interfaces

```csharp
// Commands
public record CreateUserCommand(string Email) : ICommand<UserDto>;
public class CreateUserHandler : ICommandHandler<CreateUserCommand, UserDto> { ... }

// Queries
public record GetUserQuery(Guid Id) : IQuery<UserDto>;
public class GetUserHandler : IQueryHandler<GetUserQuery, UserDto> { ... }
```

### FluentValidation extensions

**String validators**

```csharp
RuleFor(x => x.Email).IsEmail();
RuleFor(x => x.Phone).IsPhoneNumber();           // Panda format: (374)91123456
RuleFor(x => x.Contact).IsEmailOrPhoneNumber();
RuleFor(x => x.Payload).IsValidJson();
RuleFor(x => x.Content).IsXssSanitized();
```

**Single file (`IFormFile`)**

```csharp
RuleFor(x => x.Avatar)
   .HasMaxSizeMb(6)
   .ExtensionIn(".jpg", ".jpeg", ".png");
```

**File collection (`IFormFileCollection`)**

```csharp
RuleFor(x => x.Docs)
   .MaxCount(10)
   .EachHasMaxSizeMb(10)
   .EachExtensionIn(CommonFileSets.Documents)
   .TotalSizeMaxMb(50);
```

**File presets**

```csharp
CommonFileSets.Images               // .jpg .jpeg .png .webp .heic .heif .svg .avif
CommonFileSets.Documents            // .pdf .txt .csv .json .xml .yaml .md .docx .xlsx .pptx ...
CommonFileSets.ImagesAndAnimations  // Images + .gif
CommonFileSets.ImagesAndDocuments   // Images + Documents
CommonFileSets.ImportFiles          // .csv .xlsx
```

---

## CORS

Development and non-production environments allow all origins. Production restricts to the configured list and
automatically adds both `www` and non-`www` variants.

### Registration

```csharp
builder.AddCors();
app.UseCors();
```

### Production configuration

```json
{
    "Security": {
        "AllowedCorsOrigins": "https://example.com,https://api.example.com"
    }
}
```

The list accepts comma- or semicolon-separated URLs. Invalid entries are logged and filtered out.

---

## Resilience Pipelines

Built on Polly via `Microsoft.Extensions.Http.Resilience`. Provides retry, circuit breaker, and timeout policies for
`HttpClient` calls.

### Options

**1. Global — applies to all registered HttpClients:**

```csharp
builder.AddResilienceDefaultPipeline();
```

**2. Per-client:**

```csharp
builder.Services.AddHttpClient("MyClient")
   .AddResilienceDefaultPipeline();
```

**3. Manual — for wrapping arbitrary async calls:**

```csharp
public class MyService(ResiliencePipelineProvider<string> provider)
{
    public async Task CallAsync()
    {
        var pipeline = provider.GetDefaultPipeline();
        var result = await pipeline.ExecuteAsync(() => _client.GetAsync("/endpoint"));
    }
}
```

### Default pipeline policies

| Policy          | Configuration                                          |
|-----------------|--------------------------------------------------------|
| Retry (429)     | 5 retries, exponential backoff, respects `Retry-After` |
| Retry (5xx/408) | 7 retries, exponential backoff from 800ms              |
| Circuit breaker | Opens at 50% failure rate over 30 s, min 200 requests  |
| Timeout         | 8 seconds per attempt                                  |

---

## Controllers

For applications using classic MVC controllers alongside minimal APIs:

```csharp
builder.AddControllers(AssemblyRegistry.ToArray());
app.MapControllers();
```

Controller and action names are automatically kebab-cased (`UserProfile` → `user-profile`).

---

## SignalR

**Local SignalR (single instance):**

```csharp
builder.AddSignalR();
```

**Distributed SignalR backed by Redis (multi-instance):**

```csharp
builder.AddDistributedSignalR("localhost:6379", "app_name");
```

Both variants include:

- `SignalRLoggingHubFilter` — logs hub method calls with redacted arguments and elapsed time
- `SignalRExceptionFilter` — from `Pandatech.ResponseCrafter`, standardizes error responses
- MessagePack protocol for compact binary serialization

---

## OpenTelemetry

```csharp
builder.AddOpenTelemetry();
app.MapPrometheusExporterEndpoints();
```

### What is included

- ASP.NET Core metrics and traces
- HttpClient metrics and traces
- Entity Framework Core traces
- Runtime metrics
- Prometheus scraping endpoint at `/above-board/prometheus`
- Health metrics at `/above-board/prometheus/health`

### OTLP export

Set the following in your environment config or as an environment variable to enable OTLP export:

```json
{
    "OTEL_EXPORTER_OTLP_ENDPOINT": "http://localhost:4317"
}
```

---

## Health Checks

```csharp
builder.AddHealthChecks();

app.EnsureHealthy();           // runs health checks at startup; throws if anything is unhealthy
app.MapHealthCheckEndpoints(); // registers /above-board/ping and /above-board/health
```

`EnsureHealthy` skips MassTransit bus checks during startup (those take time to connect). The ping endpoint returns
`"pong"` as plain text. The health endpoint returns the full AspNetCore.HealthChecks.UI JSON format.

Additional health check registrations follow the standard `builder.Services.AddHealthChecks().Add...()` pattern — the
library does not wrap those.

---

## Maintenance Mode

Three-mode global switch. Requires `Pandatech.DistributedCache` to synchronize state across instances.

| Mode                | Effect                                                             |
|---------------------|--------------------------------------------------------------------|
| `Disabled`          | Normal operation                                                   |
| `EnabledForClients` | All routes blocked except `/api/admin/*` and `/hub/admin/*`        |
| `EnabledForAll`     | All routes blocked except `/above-board/*` and `OPTIONS` preflight |

### Registration

```csharp
builder.AddMaintenanceMode();
app.UseMaintenanceMode();       // place before UseResponseCrafter and UseCors
```

### Controlling maintenance mode

Map the built-in endpoint and protect it with your own authorization:

```csharp
app.MapMaintenanceEndpoint();
// PUT /above-board/maintenance   body: { "mode": 1 }
```

Or protect with a shared secret query parameter (useful before auth is in place):

```csharp
app.MapMaintenanceEndpoint(querySecret: "my-secret");
// PUT /above-board/maintenance?secret=my-secret
```

Programmatic control from application code:

```csharp
public class AdminService(MaintenanceState state)
{
    public Task EnableMaintenanceAsync(CancellationToken ct)
        => state.SetModeAsync(MaintenanceMode.EnabledForClients, ct);
}
```

---

## Utilities

### ValidationHelper

Static regex-based validators with a 50ms timeout per expression.

```csharp
ValidationHelper.IsEmail("user@example.com");
ValidationHelper.IsUri("https://example.com", allowNonSecure: false);
ValidationHelper.IsGuid("12345678-1234-1234-1234-123456789012");
ValidationHelper.IsPandaFormattedPhoneNumber("(374)91123456");
ValidationHelper.IsArmeniaSocialSecurityNumber("1234567890");
ValidationHelper.IsArmeniaIdCard("123456789");
ValidationHelper.IsArmeniaPassportNumber("AB1234567");
ValidationHelper.IsArmeniaTaxCode("12345678");
ValidationHelper.IsArmeniaStateRegistryNumber("123.456.78901");
ValidationHelper.IsIPv4("192.168.1.1");
ValidationHelper.IsIPv6("2001:db8::1");
ValidationHelper.IsIpAddress("192.168.1.1");
ValidationHelper.IsJson("{\"key\":\"value\"}");
ValidationHelper.IsCreditCardNumber("4111111111111111");
ValidationHelper.IsUsSocialSecurityNumber("123-45-6789");
ValidationHelper.IsUsername("user123");
```

### LanguageIsoCodeHelper

```csharp
LanguageIsoCodeHelper.IsValidLanguageCode("hy-AM");   // true
LanguageIsoCodeHelper.GetName("hy-AM");                // "Armenian (Armenia)"
LanguageIsoCodeHelper.GetCode("Armenian (Armenia)");   // "hy-AM"
```

Covers 170+ language-region combinations. The lookup table is initialized once at startup.

### PhoneUtil

Normalizes Armenian phone numbers to `+374XXXXXXXX` format from a variety of input formats:

```csharp
PhoneUtil.TryFormatArmenianMsisdn("(374)91123456", out var formatted);  // "+37491123456"
PhoneUtil.TryFormatArmenianMsisdn("+374 91 12 34 56", out var formatted); // "+37491123456"
PhoneUtil.TryFormatArmenianMsisdn("091123456", out var formatted);        // "+37491123456"
```

Returns `false` and the original input if the number cannot be parsed as an Armenian MSISDN.

### UrlBuilder

```csharp
var url = UrlBuilder.Create("https://api.example.com/users")
   .AddParameter("page", "1")
   .AddParameter("size", "20")
   .Build();
// https://api.example.com/users?page=1&size=20
```

### TimeZone extensions

```csharp
// Set once at startup from appsettings DefaultTimeZone
builder.MapDefaultTimeZone();

// Convert any DateTime to the configured zone
var local = someUtcDateTime.ToDefaultTimeZone();
```

### IHostEnvironment extensions

```csharp
env.IsLocal();
env.IsQa();
env.IsLocalOrDevelopment();
env.IsLocalOrDevelopmentOrQa();
env.GetShortEnvironmentName();  // "local" | "dev" | "qa" | "staging" | ""
```

### HttpContext extensions

```csharp
// Mark a response as private (adds X-Private-Endpoint: 1 header)
context.MarkAsPrivateEndpoint();
```

### Collection extensions

```csharp
// IEnumerable / IQueryable
var filtered = items.WhereIf(condition, x => x.IsActive);

// In operator
if (status.In(Status.Active, Status.Pending)) { ... }
```

### Dictionary extensions (zero-allocation via CollectionsMarshal)

```csharp
dict.GetOrAdd(key, defaultValue);
dict.TryUpdate(key, newValue);
```

### JsonConverters

| Converter                 | Behavior                                                   |
|---------------------------|------------------------------------------------------------|
| `EnumConverterFactory`    | Accepts enum by name or integer; serializes as name string |
| `CustomDateOnlyConverter` | Parses and writes `DateOnly` in `dd-MM-yyyy` format        |

Register via `JsonSerializerOptions.Converters` or your `ResponseCrafter` setup.

### MethodTimingStatistics

Development-only benchmarking helper. Not for production use (marked with `#warning`).

```csharp
var ts = Stopwatch.GetTimestamp();
DoWork();
MethodTimingStatistics.RecordExecution("DoWork", ts);
MethodTimingStatistics.LogAll(logger);
```

---

## PandaVault

```csharp
builder.ConfigureWithPandaVault();
```

Loads secrets from PandaVault on all non-Local environments. On Local, the call is a no-op so local `appsettings.json`
is used unchanged.

---

## Related Packages

| Package                            | Purpose                                                   |
|------------------------------------|-----------------------------------------------------------|
| `Pandatech.ResponseCrafter`        | Consistent API error responses                            |
| `Pandatech.DistributedCache`       | Redis-backed hybrid cache (required for maintenance mode) |
| `Pandatech.Crypto`                 | Cryptographic utilities                                   |
| `Pandatech.FluentMinimalApiMapper` | Minimal API endpoint mapping                              |

---

## License

MIT