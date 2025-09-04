using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace SharedKernel.Logging.Middleware;

internal sealed class SignalRLoggingHubFilter(ILogger<SignalRLoggingHubFilter> logger) : IHubFilter
{
   public async ValueTask<object?> InvokeMethodAsync(HubInvocationContext invocationContext,
      Func<HubInvocationContext, ValueTask<object?>> next)
   {
      var start = Stopwatch.GetTimestamp();

      // capture context
      var hubName = invocationContext.Hub.GetType()
                                     .Name;
      var connId = invocationContext.Context.ConnectionId;
      var userId = invocationContext.Context.UserIdentifier;
      var methodName = invocationContext.HubMethodName;

      // serialize + redact
      var rawArgsJson = JsonSerializer.Serialize(invocationContext.HubMethodArguments);
      var redactedArgsObj = RedactionHelper.RedactBody("application/json", rawArgsJson);

      // invoke the actual method
      var result = await next(invocationContext);

      var elapsedMs = Stopwatch.GetElapsedTime(start)
                               .TotalMilliseconds;

      var scope = new Dictionary<string, object?>
      {
         ["Args"] = redactedArgsObj,
         ["Hub"] = hubName,
         ["ConnId"] = connId,
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
            elapsedMs
         );
      }

      return result;
   }

   public async Task OnConnectedAsync(HubLifetimeContext context,
      Func<HubLifetimeContext, Task> next)
   {
      var hubName = context.Hub.GetType()
                           .Name;
      var connectionId = context.Context.ConnectionId;
      var userId = context.Context.UserIdentifier;

      using (logger.BeginScope(new
             {
                Hub = hubName,
                ConnId = connectionId,
                UserId = userId
             }))
      {
         logger.LogInformation("[Connected] SignalR {Hub}, ConnId={ConnId}, UserId={UserId} connected.",
            hubName,
            connectionId,
            userId);
      }

      await next(context);
   }

   public async Task OnDisconnectedAsync(HubLifetimeContext context,
      Exception? exception,
      Func<HubLifetimeContext, Exception?, Task> next)
   {
      var hubName = context.Hub.GetType()
                           .Name;
      var connectionId = context.Context.ConnectionId;
      var userId = context.Context.UserIdentifier;

      using (logger.BeginScope(new
             {
                Hub = hubName,
                ConnId = connectionId,
                UserId = userId
             }))
      {
         logger.LogInformation(
            "[Disconnected] SignalR {Hub}, ConnId={ConnId}, UserId={UserId} disconnected gracefully.",
            hubName,
            connectionId,
            userId
         );
      }

      await next(context, exception);
   }
}