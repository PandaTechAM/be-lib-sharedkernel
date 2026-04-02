# Pandatech.SharedKernel

Opinionated ASP.NET Core 10 infrastructure kernel. Not a general-purpose NuGet — meant to be forked and customized per project.

## Build & Test

```bash
dotnet build src/SharedKernel/SharedKernel.csproj
dotnet test test/SharedKernel.Tests/SharedKernel.Tests.csproj
```

- .NET 10 SDK required (see `global.json` for exact version)
- C# 14 features used: extension members, collection expressions, primary constructors
- CI publishes to NuGet on push to `main` via GitHub Actions

## Project Structure

```
src/SharedKernel/          # Library source
  Constants/               # EndpointConstants (/above-board base path)
  Extensions/              # DI registration & middleware extension methods
  Helpers/                 # AssemblyRegistry, ValidationHelper, PhoneUtil, etc.
  JsonConverters/          # Enum-as-string, DateOnly format converters
  Logging/                 # Serilog setup + HTTP request/response logging middleware
    Middleware/             # RequestLoggingMiddleware, OutboundLoggingHandler, RedactionHelper
  Maintenance/             # Distributed maintenance mode (HybridCache + poller)
  OpenApi/                 # Multi-document Swagger/Scalar UI + config
  Resilience/              # Polly retry, circuit breaker, timeout pipelines
  ValidatorAndMediatR/     # CQRS interfaces (ICommand/IQuery) + FluentValidation pipeline
    Behaviors/             # MediatR validation pipeline behaviors
    Validators/            # String, file, XSS validators + file presets
test/SharedKernel.Tests/   # xUnit tests (PhoneUtil, ValidationHelper, TimeZone)
SharedKernel.Demo/         # Working reference implementation showing full setup
```

## Key Conventions

### Startup Registration Order

Builder phase (`WebApplicationBuilder`):
1. `LogStartAttempt()` + `AssemblyRegistry.Add()`
2. `.ConfigureWithPandaVault()` (secrets, non-Local only)
3. `.AddSerilog(LogBackend.*)` (logging)
4. `.AddResponseCrafter()` (exception standardization)
5. `.AddOpenApi()`, `.AddMaintenanceMode()`, `.AddOpenTelemetry()`
6. `.AddMinimalApis()`, `.AddControllers()`, `.AddMediatrWithBehaviors()` (all take assemblies)
7. `.AddResilienceDefaultPipeline()`, `.AddSignalR()` or `.AddDistributedSignalR()`
8. `.AddCors()`, `.AddOutboundLoggingHandler()`, `.AddHealthChecks()`

App phase (`WebApplication`) — middleware order matters:
1. `.UseRequestLogging()` (must be first to capture full request lifecycle)
2. `.UseMaintenanceMode()` (before ResponseCrafter)
3. `.UseResponseCrafter()`
4. `.UseCors()`
5. `.MapMinimalApis()`, `.MapHealthCheckEndpoints()`, `.MapPrometheusExporterEndpoints()`
6. `.EnsureHealthy()` (blocks startup if unhealthy)
7. `.ClearAssemblyRegistry()` (frees memory)
8. `.UseOpenApi()`, `.MapControllers()`

### CQRS Pattern

Use `ICommand<T>` / `ICommand` for writes, `IQuery<T>` / `IQuery` for reads. Handlers: `ICommandHandler<,>`, `IQueryHandler<,>`. FluentValidation validators are auto-discovered and run as MediatR pipeline behaviors.

### Reserved Paths

All infrastructure endpoints live under `/above-board/`:
- `GET /above-board/ping` — returns "pong"
- `GET /above-board/health` — full health check JSON
- `GET /above-board/prometheus` — metrics scrape
- `PUT /above-board/maintenance` — set maintenance mode

These paths are excluded from request logging and pass through maintenance mode.

### Environment Conventions

- `IsLocal()` — developer machine, console-only logging, PandaVault skipped
- `IsDevelopment()` — console + file logging
- `IsQa()`, `IsStaging()` — file logging, PandaVault enabled
- `IsProduction()` — file logging only, restricted CORS origins

### Configuration (appsettings.json)

Required keys:
- `RepositoryName` — used for log file paths
- `ConnectionStrings:PersistentStorage` — base path for log files
- `Security:AllowedCorsOrigins` — comma/semicolon-separated origins (production only)
- `DefaultTimeZone` — e.g. "Caucasus Standard Time"
- `OpenApi` section — documents, security schemes, contact info

Optional:
- `OTEL_EXPORTER_OTLP_ENDPOINT` env var — enables OTLP export

### Logging

- Request/response bodies captured up to 16KB, with automatic sensitive-key redaction
- Redaction is key-based: headers and JSON property names containing `auth`, `token`, `pass`, `secret`, `cookie`, `pan`, `cvv`, `ssn`, etc. are redacted
- Log backends: `ElasticSearch` (ECS format), `Loki` (Grafana JSON), `CompactJson`
- Paths ignored: `/swagger`, `/openapi`, `/above-board/*`, `/favicon.ico`
- OutboundLoggingHandler logs HttpClient calls with same redaction rules

### Resilience

Two pipeline variants sharing the same configuration constants:
- **General** (`ResiliencePipelineProvider<string>`) — for non-HttpClient use
- **HTTP** (`IHttpClientBuilder.AddResilienceDefaultPipeline()`) — respects Retry-After headers

Policies: 429 retry (5 attempts), 5xx/408 retry (7 attempts), circuit breaker (50% failure / 30s window / 200 min requests), 8s timeout per attempt.

### Maintenance Mode

Three modes: `Disabled`, `EnabledForClients` (blocks non-admin routes), `EnabledForAll` (blocks everything except `/above-board/*`). State synchronized across instances via HybridCache + background poller (7s interval). Admin routes are identified by `/api/admin` and `/hub/admin` path prefixes.

## Dependency Notes

- **MediatR** pinned to `[12.5.0]` — last free version (13+ is commercial)
- **Pandatech.\*** packages are internal PandaTech libraries (ResponseCrafter, DistributedCache, Crypto, PandaVaultClient, etc.)
- Analyzers (Pandatech.Analyzers, SonarAnalyzer) are build-only, not shipped to consumers

## Code Style

- 3-space indentation
- File-scoped namespaces
- C# 14 extension members for clean builder APIs
- `partial class` + `[LoggerMessage]` for high-performance structured logging
- `FrozenSet<T>` for static lookup data
