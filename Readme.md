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
- **Logging** with Serilog (including ECS, Loki and compact JSON file output, plus automatic log cleanup).
- **MediatR and FluentValidation** configurations.
- **Cors Configuration** with easy configuration options.
- **Resilience Pipelines** for `HttpClient` operations.
- **Controller Extensions** for mapping old-style MVC controllers.
- **SignalR Extensions** for adding simple SignalR or distributed SignalR backed with Redis.
- **OpenTelemetry**: Metrics, traces, and logs with Prometheus support.
- **Health Checks**: Startup validation and endpoints for monitoring.
- **ValidationHelper**: A collection of regex-based validators for common data formats.
- Various **Extensions and Utilities**, including enumerable, string, dictionary and queryable extensions.

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
   .AddSerilog(LogBackend.Loki)
   .AddResponseCrafter(NamingConvention.ToUpperSnakeCase)
   .AddOpenApi()
   .AddOpenTelemetry()
   .AddMapMinimalApis(AssemblyRegistry.ToArray())
   .AddControllers(AssemblyRegistry.ToArray())
   .AddMediatrWithBehaviors(AssemblyRegistry.ToArray())
   .AddResilienceDefaultPipeline()
   .MapDefaultTimeZone()
   .AddDistributedCache(o =>
   {
      o.RedisConnectionString = "redis://localhost:6379";
      o.ChannelPrefix = "app_name:";
   })
   .AddDistributedSignalR("redis://localhost:6379","app_name:") // or .AddSignalR()
   .AddCors()
   .AddHealthChecks();


var app = builder.Build();

app
   .UseRequestLogging()
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
- **Swagger (external document only):
  ** [http://localhost/swagger/integration-v1](http://localhost/swagger/integration-v1)
- **Scalar (admin document):** [http://localhost/scalar/admin-v1](http://localhost/scalar/admin-v1)
- **Scalar (integration document):** [http://localhost/scalar/integration-v1](http://localhost/scalar/integration-v1)

## Logging

### Key Features

- **Serilog Integration:** Simplified setup for structured logging using Serilog.
- **Log Backend Option** Choose between:
    - `LogBackend.None` (disables file logging completely),
    - `LogBackend.ElasticSearch` (ECS formatter to file), or
    - `LogBackend.Loki` (Loki formatter to file), or
    - `LogBackend.CompactJson` (compact JSON format to file).
- **Environment-Specific Configuration:**
    - **Local:** Logs to console.
    - **Production:** Logs to file (in ECS or Loki format depending on the backend).
    - **Other Environments:** Logs to both console and file.
- **Automatic Log Cleanup:** Log files are automatically cleaned up based on the configured retention period.
- **Log File Location:** Logs are stored in a persistent path defined in your configuration, organized by repository
  name and environment, under the `logs` directory.
- **Filtering:** Excludes unwanted logs from Hangfire Dashboard, Swagger, outbox DB commands, and MassTransit health
  checks.
- **Request Logging:** Middleware that logs incoming requests and outgoing responses while redacting sensitive
  information and large payloads.
- **Outbound Logging Handler:** For capturing outbound `HttpClient` requests (including headers and bodies) with the
  same redaction rules.

### Adding Logging to Your Project

Use the `AddSerilog` extension when building your `WebApplicationBuilder`. You can specify:

- `logBackend`: One of `None`, `ElasticSearch` (ECS file format), `Loki` (Loki JSON file format) or `CompactJson`.
- `daysToRetain`: Number of days to keep log files. Older files are automatically removed by the background hosted
  service.

In your middleware pipeline, add the request and response logging middleware:

```csharp
// 1) Synchronous logging with 7-day retention (default). 
builder.AddSerilog(LogBackend.Loki);

// 2) Asynchronous logging with 14-day retention and extra properties.
//    Suitable for high-load (~1000+ RPS per pod) scenarios where slight risk of log loss is acceptable 
//    in exchange for better performance.
builder.AddSerilog(
    logBackend: LogBackend.Loki,
    logAdditionalProperties: new Dictionary<string, string>
    {
        ["ServiceName"] = "MyApp",
        ["Environment"] = "Staging"
    },
    daysToRetain: 14,
    asyncSinks: true
);
```

> - **Asynchronous Sinks (asyncSinks: true):** Recommended for very high-traffic environments (e.g., 1000+ requests per
    second per pod) where performance is critical and the possibility of losing a small amount of log data (e.g., on
    sudden process termination) is acceptable. <br><br>
>- **Synchronous Sinks (asyncSinks: false):** Recommended if you can handle up to ~1000 requests per second per pod and
   must
   retain every log entry without fail. This might incur slightly more overhead but ensures maximum reliability.

Configure minimal Serilog settings in your environment JSON files as needed, for example in
`appsettings.{Environment}.json`:

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

### Log Cleanup

When you call `builder.AddSerilog(..., daysToRetain: X)`, a `LogCleanupHostedService` is automatically registered. This
hosted service runs periodically to delete log files older than the specified retention period.

### Usage Notes

- **No Direct Sinks to External Systems:** By default, logs are written to local files with ECS or Loki JSON format. You
  can
  later push these files to external systems (e.g., via Filebeat, Logstash, Promtail, or any specialized agent).
- **Optional Enrichment:** You can pass a `Dictionary<string, string>` to `AddSerilog` to add extra log properties
  globally:
    ```csharp
    builder.AddSerilog(
        logBackend: LogBackend.Loki,
        logAdditionalProperties: new Dictionary<string, string>
        {
            {"ServiceName", "MyService"},
            {"ServiceVersion", "1.0.0"}
         }
    );    
    ```

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
app.UseRequestLogging();
// Other middleware
app.LogStartSuccess();
app.Run();
```

### Outbound Logging with HttpClient

In addition to the `RequestLoggingMiddleware` for inbound requests, you can now log **outbound** HTTP calls via an
`OutboundLoggingHandler`. This handler captures request and response data (including headers and bodies), automatically
redacting sensitive information (e.g., passwords, tokens).

#### Usage

1. **Register the handler** in your `WebApplicationBuilder`:
   ```csharp
   builder.AddOutboundLoggingHandler();
   ```
2. **Attach** the handler to any HttpClient registration:
    ```csharp
    builder.Services
       .AddHttpClient("RandomApiClient", client =>
       {
           client.BaseAddress = new Uri("http://localhost");
       })
       .AddOutboundLoggingHandler();
    ```
3. **Check logs:** Outbound requests and responses are now logged with redacted headers and bodies, just like inbound
   traffic.

> Note: The same redaction rules apply to inbound and outbound calls. Update RedactionHelper if you need to modify the
> behavior (e.g., adding new sensitive keywords).

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
    - IsEmail(): Validates that a string is a valid email address. Native one is not working correctly.
    - IsPhoneNumber(): Validates that a string is a valid phone number. Format requires area code to be in `()`.
    - IsEmailOrPhoneNumber(): Validates that a string is either a valid email address or a valid phone number.

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
    - Metrics: `url/above-board/prometheus`
    - Health Metrics: `url/above-board/prometheus/health`

3. OTLP Configuration:
   To configure the OTLP exporter, ensure the following entries are present in your appsettings{Environment}.json or as
   environment variables:
    ```json
    {
        "OTEL_EXPORTER_OTLP_ENDPOINT": "http://localhost:4317"
    }
    ```
4. Included Features:
    - ASP.NET Core metrics
    - HTTP client telemetry
    - Distributed tracing
    - Logging
    - Prometheus exporter
    - OTLP exporter
    - EF Core telemetry

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

## ValidationHelper

The `ValidationHelper` class is a highly performant and robust C# class designed to simplify complex regex validations
for
various data formats. With 100% test coverage and a focus on security through a 50ms regex execution timeout, it's an
ideal solution for applications requiring reliable and efficient data validation.

```csharp
using Pandatech.RegexBox;

// URI validation
bool isValidUri = ValidationHelper.IsUri("http://example.com", allowNonSecure: false);

// US Social Security Number validation
bool isValidSsnUs = ValidationHelper.IsUsSocialSecurityNumber("123-45-6789");

// Email validation
bool isValidEmail = ValidationHelper.IsEmail("user@example.com");

// Username validation
bool isValidUsername = ValidationHelper.IsUsername("user123");

// Armenian Social Security Number validation
bool isValidSsnAm = ValidationHelper.IsArmeniaSocialSecurityNumber("12345678912");

//ArmenianIDCard validation
bool isValidArmenianIdCard = ValidationHelper.IsArmeniaIdCard("AN1234567");

// Armenian Passport validation
bool isValidArmenianPassport = ValidationHelper.IsArmeniaPassport("AN1234567");

// Armenian Tax code validation
bool isValidArmenianTaxCode = ValidationHelper.IsArmeniaTaxCode("12345678");

// Panda Formatted Phone Number validation
bool isValidPhoneNumber = ValidationHelper.IsPandaFormattedPhoneNumber("(374)94810553");

// Armenian State Registration Number validation
bool isValidArmenianStateRegistrationNumber = ValidationHelper.IsArmeniaStateRegistryNumber("123.456.78");

// Panda formatted phone number validation

bool isValidPandaFormattedPhoneNumber = ValidationHelper.IsPandaFormattedPhoneNumber("(374)94810553");

// Guid validation
bool isValidGuid = ValidationHelper.IsGuid("12345678-1234-1234-1234-123456789012");

// IPv4 validation
bool isValidIpv4 = ValidationHelper.IsIPv4("192.168.1.1");

// IPv6 validation
bool isValidIpv6 = ValidationHelper.IsIPv6("2001:0db8:85a3:0000:0000:8a2e:0370:7334");

// Any IP validation
bool isValidIp = ValidationHelper.IsIpAddress("192.168.1.1");

// Json validation
bool isValidJson = ValidationHelper.IsJson("{\"name\":\"John\", \"age\":30}");

// and many more...
```

## Additional Extensions and NuGet Packages

This package includes various extensions and utilities to aid development:

- **Enumerable Extensions:** Additional LINQ methods for collections.
- **Host Environment Extensions:** Methods to simplify environment checks (e.g., `IsLocal()`, `IsQa()`).
- **Queryable Extensions:** Extensions for IQueryable, such as conditional `WhereIf`.
- **Dictionary Extensions:** Utility methods for dictionary manipulation in a performant way like `GetOrAdd` and
  `TryUpdate`.
- **String Extensions:** Utility methods for string manipulation.
- **Time Zone Extensions:** Methods to handle default time zones within your application. Use `.MapDefaultTimeZone()`,
  which
  retrieves DefaultTimeZone from `appsettings.json` and sets it as the default time zone.
- **UrlBuilder:** A utility for building URLs with query parameters.
- **Language ISO Code Helper:** Validate, query, and retrieve information about ISO language codes.

### Related NuGet Packages

- **Pandatech.Crypto:** Provides cryptographic utilities.
- **Pandatech.FluentMinimalApiMapper:** Simplifies mapping in minimal APIs.
- **Pandatech.ResponseCrafter:** A utility for crafting consistent API responses.
- **Pandatech.DistributedCache:** A distributed cache provider for Redis.
- **Pandatech.FileExporter:** A utility for exporting files.

## License

MIT License
