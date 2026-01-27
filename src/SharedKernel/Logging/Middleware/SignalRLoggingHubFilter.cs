using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace SharedKernel.Logging.Middleware;

internal sealed class SignalRLoggingHubFilter(ILogger<SignalRLoggingHubFilter> logger) : IHubFilter
{
    public async ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext invocationContext,
        Func<HubInvocationContext, ValueTask<object?>> next)
    {
        var timestamp = Stopwatch.GetTimestamp();

        var hubName = invocationContext.Hub.GetType().Name;
        var connectionId = invocationContext.Context.ConnectionId;
        var userId = invocationContext.Context.UserIdentifier;
        var methodName = invocationContext.HubMethodName;

        // Serialize and redact arguments
        var rawArgsJson = JsonSerializer.Serialize(invocationContext.HubMethodArguments);
        var redactedArgs = RedactionHelper.RedactBody("application/json", rawArgsJson);

        var result = await next(invocationContext);

        var elapsedMs = Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds;

        // Convert to JSON string to prevent Elasticsearch field explosion
        var scope = new Dictionary<string, object?>
        {
            ["Args"] = LogFormatting.ToJsonString(redactedArgs),
            ["Hub"] = hubName,
            ["ConnId"] = connectionId,
            ["UserId"] = userId,
            ["ElapsedMs"] = elapsedMs,
            ["Kind"] = "SignalR"
        };

        using (logger.BeginScope(scope))
        {
            logger.LogInformation(
                "[SignalR] {Hub}.{Method} completed in {ElapsedMilliseconds}ms",
                hubName,
                methodName,
                elapsedMs);
        }

        return result;
    }

    public async Task OnConnectedAsync(HubLifetimeContext context, Func<HubLifetimeContext, Task> next)
    {
        var hubName = context.Hub.GetType().Name;
        var connectionId = context.Context.ConnectionId;
        var userId = context.Context.UserIdentifier;

        using (logger.BeginScope(new Dictionary<string, object?>
        {
            ["Hub"] = hubName,
            ["ConnId"] = connectionId,
            ["UserId"] = userId,
            ["Kind"] = "SignalR"
        }))
        {
            logger.LogInformation(
                "[Connected] SignalR {Hub}, ConnId={ConnId}, UserId={UserId} connected.",
                hubName,
                connectionId,
                userId);
        }

        await next(context);
    }

    public async Task OnDisconnectedAsync(
        HubLifetimeContext context,
        Exception? exception,
        Func<HubLifetimeContext, Exception?, Task> next)
    {
        var hubName = context.Hub.GetType().Name;
        var connectionId = context.Context.ConnectionId;
        var userId = context.Context.UserIdentifier;

        using (logger.BeginScope(new Dictionary<string, object?>
        {
            ["Hub"] = hubName,
            ["ConnId"] = connectionId,
            ["UserId"] = userId,
            ["Kind"] = "SignalR"
        }))
        {
            logger.LogInformation(
                "[Disconnected] SignalR {Hub}, ConnId={ConnId}, UserId={UserId} disconnected gracefully.",
                hubName,
                connectionId,
                userId);
        }

        await next(context, exception);
    }
}