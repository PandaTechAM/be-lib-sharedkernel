using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;

namespace SharedKernel.Logging.Middleware;

/// <summary>
///     Registration helpers for request/response and outbound HTTP call logging middleware.
/// </summary>
public static class LoggingExtensions
{
    /// <summary>
    ///     Adds the request/response logging middleware to the pipeline, if information-level logging is enabled.
    /// </summary>
    public static WebApplication UseRequestLogging(this WebApplication app)
    {
        if (Log.Logger.IsEnabled(LogEventLevel.Information))
        {
            app.UseMiddleware<RequestLoggingMiddleware>();
        }

        return app;
    }

    /// <summary>
    ///     Registers the outbound HTTP logging handler as a transient service, if information-level logging is enabled.
    /// </summary>
    public static WebApplicationBuilder AddOutboundLoggingHandler(this WebApplicationBuilder builder)
    {
        if (Log.Logger.IsEnabled(LogEventLevel.Information))
        {
            builder.Services.AddTransient<OutboundLoggingHandler>();
        }

        return builder;
    }

    /// <summary>
    ///     Attaches the outbound HTTP logging handler to the client, if information-level logging is enabled.
    /// </summary>
    public static IHttpClientBuilder AddOutboundLoggingHandler(this IHttpClientBuilder builder)
    {
        if (Log.Logger.IsEnabled(LogEventLevel.Information))
        {
            builder.AddHttpMessageHandler<OutboundLoggingHandler>();
        }

        return builder;
    }
}
