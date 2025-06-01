using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace SharedKernel.Logging;

internal sealed class SignalRLoggingHubFilter(ILogger<SignalRLoggingHubFilter> logger) : IHubFilter
{
   public async ValueTask<object?> InvokeMethodAsync(HubInvocationContext invocationContext,
      Func<HubInvocationContext, ValueTask<object?>> next)
   {
      var startTimestamp = Stopwatch.GetTimestamp();

      var hubName = invocationContext.Hub.GetType()
                                     .Name;
      var connectionId = invocationContext.Context.ConnectionId;
      var userId = invocationContext.Context.UserIdentifier;
      var methodName = invocationContext.HubMethodName;

      var serializedArgs = JsonSerializer.Serialize(invocationContext.HubMethodArguments);
      var redactedArgs = RedactionHelper.ParseAndRedactJson(serializedArgs);


      var result = await next(invocationContext);


      var elapsedMs = Stopwatch.GetElapsedTime(startTimestamp)
                               .TotalMilliseconds;

      using (logger.BeginScope(new
             {
                Hub = hubName,
                ConnId = connectionId,
                UserId = userId,
                Method = methodName
             }))
      {
         logger.LogInformation(
            "[Incoming Message] SignalR {Hub}, ConnId={ConnId}, UserId={UserId}, Method={Method}, completed in {ElapsedMs}ms, Args={Args}",
            hubName,
            connectionId,
            userId,
            methodName,
            elapsedMs,
            redactedArgs
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