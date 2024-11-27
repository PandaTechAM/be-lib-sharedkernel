# Pandatech.SharedKernel

Welcome to the `Pandatech.SharedKernel` NuGet package - a centralized library designed to streamline development across
all PandaTech projects. This package consolidates shared configurations, utilities, and extensions into a single,
reusable resource.

Although this package is primarily intended for internal use, it is publicly available for anyone who may find it
useful. We recommend forking or copying the classes in this repository and creating your own package to suit your needs.

By leveraging this shared kernel, we aim to:

- Reduce the amount of boilerplate code required to start a new project.
- Ensure consistency across all PandaTech projects.
- Simplify the process of updating shared configurations and utilities.

## Scope

This package currently supports:

- **OpenAPI Configuration** with SwaggerUI and Scalar.
- **Logging** with Serilog.
- **MediatR and FluentValidation** configurations.
- **Cors Configuration** with easy configuration options.
- **Resilience Pipelines** for `HttpClient` operations.
- **Controller Extensions** for mapping old-style MVC controllers.
- **SignalR Extensions** for adding simple SignalR or distributed SignalR backed with Redis.
- **OpenTelemetry**: Metrics, traces, and logs with Prometheus support.
- **Health Checks**: Startup validation and endpoints for monitoring.
- Various **Extensions and Utilities**, including enumerable, string, and queryable extensions.

## Prerequisites

- .NET 9.0 SDK or higher

## Installation

To install the `Pandatech.SharedKernel` package, use the following command:

```bash
dotnet add package Pandatech.SharedKernel
```

Alternatively, you can add it via the NuGet Package Manager in Visual Studio, VS Code or Rider.

## Full SharedKernel Demo

This section demonstrates how to use the `Pandatech.SharedKernel` package in a fully functional application. It includes
examples of:

- Comprehensive `appsettings.json` configurations.
- Environment-specific settings in `appsettings.{Environment}.json`.
- A complete `Program.cs` implementation with all major features integrated.

Follow this example to set up your project with all the features provided by this library.

### appsettings.json

```json
{
    "OpenApi": {
        "DisabledEnvironments": [
            "Production"
        ],
        "SecuritySchemes": [
            {
                "HeaderName": "Client-Type",
                "Description": "Specifies the client type, e.g., '2'."
            },
            {
                "HeaderName": "Authorization",
                "Description": "Access token for the API."
            }
        ],
        "Documents": [
            {
                "Title": "Administrative Panel Partners",
                "Description": "This document describes the API endpoints for the Administrative Panel Partners.",
                "GroupName": "admin-v1",
                "Version": "v1",
                "ForExternalUse": false
            },
            {
                "Title": "Integration",
                "Description": "Integration API Endpoints",
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

### appsettings.{Environment}.json

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
    "ResponseCrafterVisibility": "Private",
    "DefaultTimeZone": "Caucasus Standard Time",
    "RepositoryName": "be-lib-sharedkernel",
    "ConnectionStrings": {
        "Redis": "localhost:6379",
        "PersistentStorage": "/persistence"
    }
}
```

### Program.cs

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.LogStartAttempt();
AssemblyRegistry.Add(typeof(Program).Assembly);

builder
   .ConfigureWithPandaVault()
   .AddSerilog()
   .AddResponseCrafter(NamingConvention.ToSnakeCase)
   .AddOpenApi()
   .AddOpenTelemetry()
   .AddMapMinimalApis(AssemblyRegistry.ToArray())
   .AddControllers(AssemblyRegistry.ToArray())
   .AddMediatrWithBehaviors(AssemblyRegistry.ToArray())
   .AddResilienceDefaultPipeline()
   .MapDefaultTimeZone()
   .AddRedis(KeyPrefix.AssemblyNamePrefix)
   .AddDistributedSignalR("DistributedSignalR") // or .AddSignalR()
   .AddCors()
   .AddHealthChecks();


var app = builder.Build();

app
   .UseRequestResponseLogging()
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

For a deeper dive into each feature, please refer to the following sections.

## OpenAPI

`Microsoft.AspNetCore.OpenApi` is the new standard for creating OpenAPI JSON files. We have adopted this library instead
of Swashbuckle for generating OpenAPI definitions. Along with this new library, we have integrated `SwaggerUI` and
`Scalar` to provide user-friendly interfaces in addition to the JSON files.

### Key Features

- **Multiple API Documents:** Easily define and organize multiple API documentation groups.
- **Enum String Values:** Enum string values are automatically displayed in the documentation, simplifying integration
  for external partners.
- **Security Schemes:** Configure security headers directly in your OpenAPI settings.

### Adding OpenAPI to Your Project

To enable OpenAPI in your project, add the following code:

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.AddOpenApi();
var app = builder.Build();
app.UseOpenApi();
app.Run();
```

You can also customize the `AddOpenApi` method with options:

```csharp
builder.AddOpenApi(options =>
{
    options.AddSchemaTransformer<CustomSchemaTransformer>();
});
```

### Configuration

Add the following configuration to your `appsettings.json` file:

```json
{
    "OpenApi": {
        "DisabledEnvironments": [
            "Production"
        ],
        "SecuritySchemes": [
            {
                "HeaderName": "Authorization",
                "Description": "Access token for the API."
            }
        ],
        "Documents": [
            {
                "Title": "Admin Panel API",
                "Description": "API for administrative functions.",
                "GroupName": "admin-v1",
                "Version": "v1",
                "ForExternalUse": false
            },
            {
                "Title": "Integration",
                "Description": "Integration API Endpoints",
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

### Notes

- **For External Use:** If you set `ForExternalUse: true` for a document, it will be available both within the regular
  SwaggerUI and a separate SwaggerUI instance. This allows you to provide a dedicated URL to external partners while
  keeping internal documents private.
- **Scalar UI Limitations:** Scalar currently does not support multiple documents within a single URL. Consequently, all
  documents in Scalar will be separated into individual URLs. Support for multiple documents is expected in future
  Scalar updates.

### Example URLs

Based on the above configuration, the UI will be accessible at the following URLs:

- **Swagger (all documents):** [http://localhost/swagger](http://localhost/swagger)
- **Swagger (external document only):** [http://localhost/doc/integration-v1](http://localhost/doc/integration-v1)
- **Scalar (admin document):** [http://localhost/scalar/admin-v1](http://localhost/scalar/admin-v1)
- **Scalar (integration document):** [http://localhost/scalar/integration-v1](http://localhost/scalar/integration-v1)

## Logging

### Key Features

- **Serilog Integration:** Simplified setup for structured logging using Serilog.
- **Environment-Specific Configuration:** Logs are written to the console and/or files based on the environment (Local,
  Development, Production).
- **Elastic Common Schema Formatting:** Logs are formatted using the Elastic Common Schema (ECS) for compatibility with
  Elasticsearch.
- **Request and Response Logging Middleware:** Middleware that logs incoming requests and outgoing responses while
  redacting
  sensitive information.
- **Log Filtering:** Excludes unwanted logs from Hangfire Dashboard, Swagger, and outbox database commands.
- **Distributed:** Designed to work with distributed systems and microservices.

### Adding Logging to Your Project

To enable Serilog logging in your project, add the following code:

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.AddSerilog();
```

In your middleware pipeline, add the request and response logging middleware:

```csharp
var app = builder.Build();
app.UseRequestResponseLogging();
```

In your `appsettings.{Environment}.json` configure `Serilog`.
Example:

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
    "RepositoryName": "be-lib-sharedkernel",
    "ConnectionStrings": {
        "PersistentStorage": "/persistence"
    }
}
```

### Work Specifics

- Environment-Specific Logging:
    - Local Environment: Logs are written to the console.
    - Production: Logs are written to a file.
    - Other Environments: Logs are written to both the console and a file.
- Log File Location: Logs are stored in a persistent path defined in your configuration, organized by repository name
  and environment.
- Sensitive Data Redaction: The middleware automatically redacts sensitive information such as passwords, secrets,
  tokens, and authentication details from headers and bodies.

### Startup Logging

The package provides methods to log application startup events:

- `LogStartAttempt()`: Logs when the application start is attempted.
- `LogStartSuccess()`: Logs when the application has successfully started, including the initialization time.
- `LogModuleRegistrationSuccess(moduleName)`: Logs successful registration of a module.
- `LogModuleUseSuccess(moduleName)`: Logs successful usage of a module.

Example:

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.LogStartAttempt();
// Configure services
var app = builder.Build();
// Configure middleware
app.UseRequestResponseLogging();
// Other middleware
app.LogStartSuccess();
app.Run();
```

## MediatR and FluentValidation Integration

### Key Features

- **MediatR Integration:** Simplifies the implementation of the Mediator pattern for handling commands and queries.
- **Custom Interfaces for CQRS:** Provides ICommand and IQuery interfaces to facilitate the Command Query Responsibility
  Segregation (CQRS) pattern.
- **Validation Behaviors:** Automatically validates requests using FluentValidation before they reach the handler.
- **FluentValidation Extensions:** Includes custom validators for common scenarios like file size, file type, JSON
  validity,
  and XSS sanitization.

### Adding MediatR to Your Project

To enable MediatR with validation behaviors in your project, add the following code:

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.AddMediatrWithBehaviors([typeof(Program).Assembly]);
```

This extension method registers MediatR and adds custom pipeline behaviors for validation. It scans the specified
assemblies for handlers and validators.

### How It Works

**Custom Interfaces for CQRS**
The package provides custom interfaces to distinguish between commands and queries, aligning with the CQRS pattern:

- Commands:
    - `ICommand<TResponse>` and `ICommand`
    - Handled by `ICommandHandler<TCommand, TResponse>` and `ICommandHandler<TCommand>`
- Queries:
    - `IQuery<TResponse>` and `IQuery`
    - Handled by `IQueryHandler<TQuery, TResponse>` and `IQueryHandler<TQuery>`

These interfaces help in organizing your application logic and can be extended in the future, for example, to route read
requests to replicas and write requests to a primary database.

### Validation Behaviors

Two custom pipeline behaviors are added to MediatR:

- `ValidationBehaviorWithResponse<TRequest, TResponse>`: Used for requests that expect a response.
- `ValidationBehaviorWithoutResponse<TRequest, TResponse>`: Used for requests that do not expect a response.

These behaviors automatically validate the incoming requests using FluentValidation validators before they reach the
handler. If validation fails, a `BadRequestException` is thrown with the validation errors.
> `BadRequestException` is from Pandatech.ResponseCrafter NuGet package

### FluentValidation Extensions

The package includes extension methods to simplify common validation scenarios:

- File Validations:
    - HasMaxFileSize(maxFileSizeInMb): Validates that an uploaded file does not exceed the specified maximum size.
    - FileTypeIsOneOf(allowedFileExtensions): Validates that the uploaded file has one of the allowed file
      extensions.
- String Validations:
    - IsValidJson(): Validates that a string is a valid JSON.
    - IsXssSanitized(): Validates that a string is sanitized against XSS attacks.

## Cors

### Key Features

- **Non-Production Environment:** Allows all origins for ease of development and testing.
- **Production Environment**
    - Restricts origins to a specific list defined in the configurations.
    - Automatically handles `www` and non-`www` versions of allowed origins.
    - Supports credentials, all methods and headers, and preflight caching.

### Adding Cors to Your Project

To enable Cors in your project, add the following code:

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.AddCors();
var app = builder.Build();
app.UseCors();
app.Run();
```

### Configuration

Add following to your `appsetings.production.json` file:

```json
{
    "Security": {
        "AllowedCorsOrigins": "https://example.com,https://api.example.com"
    }
}
```

- AllowedCorsOrigins: A comma- or semicolon-separated list of allowed origins. Invalid URLs are automatically filtered
  out.

## Resilience Pipelines

### Key Features

- **Default Resilience Pipeline:** Provides a pre-configured resilience pipeline with retry, circuit breaker, and
  timeout
  policies.
- **Polly Integration:** Utilizes the Microsoft.Extensions.Http.Resilience library which is built on Polly library for
  implementing advanced resilience strategies.
- **Flexible Configuration:** Can be applied globally via WebApplicationBuilder or locally within specific HTTP client
  configurations.
- **HTTP Client Resilience:** Enhances the reliability of HTTP client calls by handling transient faults and network
  issues.

### Adding Resilience Pipelines to Your Project

To enable the default resilience pipeline in your project, you have 3 options:

1. Global Configuration via `WebApplicationBuilder`
   This method applies the resilience pipeline to all HTTP clients registered in your application.
   ```csharp
   var builder = WebApplication.CreateBuilder(args);
   builder.AddResilienceDefaultPipeline();
   ```
2. Local Configuration within HTTP Client Registration
   This method applies the resilience pipeline to a specific HTTP client registration.
   ```csharp
   builder.Services.AddHttpClient("MyHttpClient")
    .AddResilienceDefaultPipeline();
   ```
3. Configuration within HttpClientService.cs
    ```csharp
    public class MyHttpClientService
    {
    private readonly ResiliencePipelineProvider<string> _resiliencePipelineProvider;
    private readonly HttpClient _httpClient;
    
        public MyHttpClientService(ResiliencePipelineProvider<string> resiliencePipelineProvider, HttpClient httpClient)
        {
            _resiliencePipelineProvider = resiliencePipelineProvider;
            _httpClient = httpClient;
        }
    
        public async Task FooAsync()
        {
            var pipeline = _resiliencePipelineProvider.GetDefaultPipeline();
            var response = await pipeline.ExecuteAsync(() => _httpClient.GetAsync("https://example.com"));
        }
    }
    ```

### How It Works

The default resilience pipeline includes the following policies:

- **Retry Policy for 429 (Too Many Requests):** Retries the request up to 5 times with an exponential backoff,
  respecting
  the `Retry-After` header if present.
- **Retry Policy for Network Errors and Timeouts:** Retries network-related failures up to 7 times with an exponential
  backoff.
- **Circuit Breaker Policy:** Breaks the circuit when the failure ratio exceeds 50% within a 30-second sampling
  duration,
  with a minimum throughput of 200 requests.
- **Timeout Policy:** Times out requests that take longer than 8 seconds.

## Controller Extensions

For mapping old style MVC controllers, use `builder.AddControllers()`.
The `AddControllers()` method can also accept assembly names as parameters to scan for controllers.
The `MapControllers()` method maps the controllers to the application.

Example:

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.AddControllers([typeof(Program).Assembly]);
var app = builder.Build();
app.MapControllers();
app.Run();
```

## Telemetry Integration

Integrate OpenTelemetry for observability, including metrics, traces, and logging:

1. Setup:
    ```csharp
    var builder = WebApplication.CreateBuilder(args);
    builder.AddOpenTelemetry();
    var app = builder.Build();
    app.MapPrometheusExporterEndpoints();
    app.Run();
    ```
2. Prometheus Endpoints:
    - Metrics: `url/above-board/metrics`
    - Health Metrics: `url/above-board/metrics/health`
3. Included Features:
    - ASP.NET Core metrics
    - HTTP client telemetry
    - Distributed tracing
    - Logging
    - Prometheus exporter

## HealthChecks

- **Startup Validation:** `app.EnsureHealthy()` performs a health check at startup and terminates the application if it
  is not healthy.
- **Endpoints Mapping:** `app.MapHealthCheckEndpoints()` maps default health check endpoints to the application.
- **Mapped Endpoints:**
    - Ping Endpoint: `url/above-board/ping`
    - Health Check Endpoint: `url/above-board/health`

Example:

```csharp
var app = builder.Build();

app.EnsureHealthy(); // Startup validation
app.MapHealthCheckEndpoints(); // Map health check routes

app.Run();
```

## Additional Extensions and NuGet Packages

This package includes various extensions and utilities to aid development:

- **Enumerable Extensions:** Additional LINQ methods for collections.
- **Host Environment Extensions:** Methods to simplify environment checks (e.g., `IsLocal()`, `IsQa()`).
- **Queryable Extensions:** Extensions for IQueryable, such as conditional `WhereIf`.
- **String Extensions:** Utility methods for string manipulation.
- **Time Zone Extensions:** Methods to handle default time zones within your application. Use `.MapDefaultTimeZone()`,
  which
  retrieves DefaultTimeZone from `appsettings.json` and sets it as the default time zone.
- **UrlBuilder:** A utility for building URLs with query parameters.

### Related NuGet Packages

- **Pandatech.Crypto:** Provides cryptographic utilities.
- **Pandatech.FluentMinimalApiMapper:** Simplifies mapping in minimal APIs.
- **Pandatech.RegexBox:** A collection of useful regular expressions.
- **Pandatech.ResponseCrafter:** A utility for crafting consistent API responses.
- **Pandatech.DistributedCache:** A distributed cache provider for Redis.

## License

MIT License
