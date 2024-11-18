# Pandatech.SharedKernel

Welcome to the `Pandatech.SharedKernel` NuGet package, a centralized library designed to streamline development across all PandaTech projects. This package consolidates shared configurations, utilities, and extensions into a single, reusable resource.

Though this package is primarily intended for internal use, it is publicly available for anyone who may find it useful. We recommend forking or copying the classes in this repository and creating your own package to suit your needs.

By leveraging this shared kernel, we aim to:

- Reduce the amount of boilerplate code required to start a new project.
- Ensure consistency across all PandaTech projects.
- Simplify the process of updating shared configurations and utilities.

## Scope

This package currently supports:

- **OpenAPI Configuration** with SwaggerUI and Scalar.


## OpenAPI

`Microsoft.AspNetCore.OpenApi` is the new standard for creating OpenAPI JSON files. We have adopted this library instead of Swashbuckle for generating OpenAPI definitions. While using this new library, we have integrated `SwaggerUI` and `Scalar` to provide user-friendly interfaces in addition to the JSON files.

### Key Features

- **Multiple API Documents:** Easily define and organize multiple API documentation groups.
- **Enum String Values:** Enum string values are automatically displayed in the documentation, simplifying integration for external partners.
- **Customizable SwaggerUI:** Add custom styles and JavaScript to tailor the UI.
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
    },
    "SwaggerUi": {
        "InjectedCssPaths": [
            "/assets/css/panda-style.css"
        ],
        "InjectedJsPaths": [
            "/assets/js/docs.js"
        ]
    },
    "ScalarUi": {
        "FaviconPath": "/assets/images/favicon.svg"
    }
}
```

### Notes

- **For External Use:** If you set `ForExternalUse: true` for a document, it will be available both within the regular SwaggerUI and a separate SwaggerUI instance. This allows you to provide a dedicated URL to external partners while keeping internal documents private.
- **Scalar UI Limitations:** Scalar currently does not support multiple documents within a single URL. Consequently, all documents in Scalar will be separated into individual URLs. Support for multiple documents is expected in future Scalar updates.

### Example URLs

Based on the above configuration, the UI will be accessible at the following URLs:

- **Swagger (all documents):** [http://localhost/swagger](http://localhost/swagger)
- **Swagger (external document only):** [http://localhost/doc/integration-v1](http://localhost/doc/integration-v1)
- **Scalar (admin document):** [http://localhost/scalar/admin-v1](http://localhost/scalar/admin-v1)
- **Scalar (integration document):** [http://localhost/scalar/integration-v1](http://localhost/scalar/integration-v1)