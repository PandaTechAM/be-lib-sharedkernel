using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace SharedKernel.Logging.Middleware;

internal sealed partial class SignalRLoggingHubFilter(ILogger<SignalRLoggingHubFilter> logger) : IHubFilter
{
   public async ValueTask<object?> InvokeMethodAsync(
      HubInvocationContext invocationContext,
      Func<HubInvocationContext, ValueTask<object?>> next)
   {
      var timestamp = Stopwatch.GetTimestamp();

      var hubName = invocationContext.Hub.GetType().Name;
      var methodName = invocationContext.HubMethodName;
      var connectionId = invocationContext.Context.ConnectionId;
      var userId = invocationContext.Context.UserIdentifier;

      var rawArgsJson = JsonSerializer.Serialize(invocationContext.HubMethodArguments);
      var redactedArgs = RedactionHelper.RedactBody("application/json", rawArgsJson);

      var result = await next(invocationContext);

      var elapsedMs = Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds;

      LogInvoke(hubName, methodName, elapsedMs, "SignalR", connectionId, userId, LogFormatting.ToJsonString(redactedArgs));

      return result;
   }

   public async Task OnConnectedAsync(HubLifetimeContext context, Func<HubLifetimeContext, Task> next)
   {
      var (hub, connId, userId) = ExtractContext(context);
      LogConnected(hub, "SignalR", connId, userId);
      await next(context);
   }

   public async Task OnDisconnectedAsync(
      HubLifetimeContext context,
      Exception? exception,
      Func<HubLifetimeContext, Exception?, Task> next)
   {
      var (hub, connId, userId) = ExtractContext(context);
      LogDisconnected(hub, "SignalR", connId, userId);
      await next(context, exception);
   }

   private static (string Hub, string? ConnId, string? UserId) ExtractContext(HubLifetimeContext ctx) =>
      (ctx.Hub.GetType().Name, ctx.Context.ConnectionId, ctx.Context.UserIdentifier);

   // All named placeholders become structured properties in Serilog / Elasticsearch.
   // Eliminates the BeginScope dictionary allocation and the LogInformation args-array allocation.

   [LoggerMessage(Level = LogLevel.Information,
      Message = "[SignalR] {Hub}.{HubMethod} completed in {ElapsedMs}ms | {Kind} connId={ConnId} userId={UserId} args={Args}")]
   private partial void LogInvoke(
      string hub,
      string hubMethod,
      double elapsedMs,
      string kind,
      string? connId,
      string? userId,
      string args);

   [LoggerMessage(Level = LogLevel.Information,
      Message = "[Connected] SignalR {Hub} | {Kind} connId={ConnId} userId={UserId}")]
   private partial void LogConnected(
      string hub,
      string kind,
      string? connId,
      string? userId);

   [LoggerMessage(Level = LogLevel.Information,
      Message = "[Disconnected] SignalR {Hub} | {Kind} connId={ConnId} userId={UserId}")]
   private partial void LogDisconnected(
      string hub,
      string kind,
      string? connId,
      string? userId);
}