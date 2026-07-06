using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Constants;

namespace SharedKernel.Maintenance;

/// <summary>
///     Extension methods for registering and enabling maintenance mode middleware.
/// </summary>
public static class WebAppExtensions
{
    /// <summary>
    ///     Registers the services required for maintenance mode support.
    /// </summary>
    public static WebApplicationBuilder AddMaintenanceMode(this WebApplicationBuilder builder)
    {
        if (builder.Services.Any(sd => sd.ServiceType == typeof(MaintenanceModeMarker)))
        {
            return builder;
        }

        builder.Services.AddSingleton<MaintenanceModeMarker>();
        builder.Services.AddSingleton<MaintenanceState>();
        builder.Services.AddHostedService<MaintenanceCachePoller>();

        return builder;
    }

    /// <summary>
    ///     Enables the maintenance mode middleware in the request pipeline.
    /// </summary>
    public static WebApplication UseMaintenanceMode(this WebApplication app)
    {
        var marker = app.Services.GetService<MaintenanceModeMarker>()
                     ?? throw new InvalidOperationException(
                         "You must call AddMaintenanceMode on the WebApplicationBuilder before calling UseMaintenanceMode on the WebApplication.");

        if (marker.MiddlewareRegistered)
        {
            return app;
        }

        app.UseMiddleware<MaintenanceMiddleware>();
        marker.MiddlewareRegistered = true;

        return app;
    }

    /// <summary>
    ///     Maps PUT {EndpointConstants.BasePath}/maintenance.
    ///     Set <paramref name="querySecret" /> to enable a shared-secret check; leave it null to use your own authorization
    ///     instead.
    /// </summary>
    public static RouteHandlerBuilder MapMaintenanceEndpoint(this IEndpointRouteBuilder app,
        string? querySecret = null)
    {
        var marker = app.ServiceProvider.GetService<MaintenanceModeMarker>();

        if (marker is not { MiddlewareRegistered: true })
        {
            throw new InvalidOperationException("Call UseMaintenanceMode before MapMaintenanceEndpoint.");
        }

        if (string.IsNullOrWhiteSpace(querySecret))
        {
            return app.MapPut(EndpointConstants.BasePath + "/maintenance",
                    async ([FromServices] MaintenanceState state,
                        [FromBody] MaintenanceModeRequest req,
                        CancellationToken ct) =>
                    {
                        await state.SetModeAsync(req.Mode, ct);
                        return TypedResults.Ok(new MaintenanceModeResponse("Mode set to " + req.Mode,
                            DateTimeOffset.UtcNow));
                    })
                .WithTags(EndpointConstants.TagName)
                .WithSummary("Set maintenance mode");
        }

        return app.MapPut(EndpointConstants.BasePath + "/maintenance",
                async ([FromServices] MaintenanceState state,
                    [FromBody] MaintenanceModeRequest req,
                    [FromQuery] string secret,
                    CancellationToken ct) =>
                {
                    if (!string.Equals(secret, querySecret, StringComparison.Ordinal))
                    {
                        return Results.Unauthorized();
                    }

                    await state.SetModeAsync(req.Mode, ct);
                    return TypedResults.Ok(new MaintenanceModeResponse("Mode set to " + req.Mode,
                        DateTimeOffset.UtcNow));
                })
            .WithTags(EndpointConstants.TagName)
            .WithSummary("Set maintenance mode")
            .Produces(StatusCodes.Status401Unauthorized);
    }

    internal sealed class MaintenanceModeMarker
    {
        public bool MiddlewareRegistered { get; set; }
    }
}

/// <summary>
///     Request payload for setting the maintenance mode.
/// </summary>
/// <param name="Mode">The maintenance mode to apply.</param>
public sealed record MaintenanceModeRequest(MaintenanceMode Mode);

/// <summary>
///     Response returned after updating the maintenance mode.
/// </summary>
/// <param name="Message">A human-readable confirmation message.</param>
/// <param name="UpdatedAt">The timestamp when the mode was updated.</param>
public sealed record MaintenanceModeResponse(string Message, DateTimeOffset UpdatedAt);
