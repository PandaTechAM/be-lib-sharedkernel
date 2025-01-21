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
      var start = Stopwatch.GetTimestamp();

      // Basic context info
      var hubName = invocationContext.Hub.GetType()
                                     .Name;
      var connectionId = invocationContext.Context.ConnectionId;
      var userId = invocationContext.Context.UserIdentifier;
      var methodName = invocationContext.HubMethodName;

      // Redact arguments
      var serializedArgs = JsonSerializer.Serialize(invocationContext.HubMethodArguments);
      var redactedArgs = RedactionHelper.ParseAndRedactJson(serializedArgs);

      object? result = null;
      Exception? exception = null;

      try
      {
         // Invoke the actual hub method
         result = await next(invocationContext);
      }
      catch (Exception ex)
      {
         exception = ex;
      }

      var elapsedMs = Stopwatch.GetElapsedTime(start)
                               .TotalMilliseconds;

      if (exception is not null)
      {
         logger.LogError(exception,
            "[SignalR] Hub {HubName}, ConnId {ConnectionId}, UserId {UserId} - Method {MethodName} threw an exception after {ElapsedMs}ms. " +
            "Inbound Args: {Args}",
            hubName,
            connectionId,
            userId,
            methodName,
            elapsedMs,
            redactedArgs);
         throw exception;
      }

      // Redact return value, if any
      var redactedResult = string.Empty;
      if (result is not null)
      {
         var serializedResult = JsonSerializer.Serialize(result);
         var redactedObj = RedactionHelper.ParseAndRedactJson(serializedResult);
         redactedResult = JsonSerializer.Serialize(redactedObj);
      }

      logger.LogInformation(
         "[SignalR] Hub {HubName}, ConnId {ConnectionId}, UserId {UserId} - Method {MethodName} completed in {ElapsedMs}ms. " +
         "Inbound Args: {Args}, Outbound Result: {Result}",
         hubName,
         connectionId,
         userId,
         methodName,
         elapsedMs,
         redactedArgs,
         redactedResult);

      return result;
   }
}