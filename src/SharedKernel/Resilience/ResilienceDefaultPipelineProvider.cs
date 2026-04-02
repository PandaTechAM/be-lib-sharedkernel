using System.Globalization;
using System.Net;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Polly.CircuitBreaker;
using Polly.Registry;
using Polly.Retry;

namespace SharedKernel.Resilience;

public static class ResilienceDefaultPipelineProvider
{
   internal const string DefaultPipelineName = "DefaultPipeline";

   // Shared constants — single source of truth for both pipeline variants
   private const int TooManyRequestsMaxRetries = 5;
   private const int NetworkMaxRetries = 7;
   private const double CircuitBreakerFailureRatio = 0.5;
   private const int CircuitBreakerMinThroughput = 200;
   private static readonly TimeSpan TooManyRequestsDelay = TimeSpan.FromMilliseconds(3000);
   private static readonly TimeSpan NetworkRetryDelay = TimeSpan.FromMilliseconds(800);
   private static readonly TimeSpan CircuitBreakerSamplingDuration = TimeSpan.FromSeconds(30);
   private static readonly TimeSpan CircuitBreakerBreakDuration = TimeSpan.FromSeconds(45);
   internal static readonly TimeSpan AttemptTimeout = TimeSpan.FromSeconds(8);

   // ── General pipeline options (for ResiliencePipeline) ──────────────────────

   internal static RetryStrategyOptions TooManyRequestsRetryOptions =>
      new()
      {
         MaxRetryAttempts = TooManyRequestsMaxRetries,
         BackoffType = DelayBackoffType.Exponential,
         UseJitter = true,
         Delay = TooManyRequestsDelay,
         ShouldHandle = new PredicateBuilder()
            .Handle<HttpRequestException>(ex => ex.StatusCode == HttpStatusCode.TooManyRequests)
      };

   internal static RetryStrategyOptions DefaultNetworkRetryOptions =>
      new()
      {
         MaxRetryAttempts = NetworkMaxRetries,
         BackoffType = DelayBackoffType.Exponential,
         UseJitter = true,
         Delay = NetworkRetryDelay,
         ShouldHandle = new PredicateBuilder()
            .Handle<HttpRequestException>(ex => ex.StatusCode == HttpStatusCode.RequestTimeout ||
                                                (int)ex.StatusCode! >= 500)
      };

   internal static CircuitBreakerStrategyOptions DefaultCircuitBreakerOptions =>
      new()
      {
         FailureRatio = CircuitBreakerFailureRatio,
         SamplingDuration = CircuitBreakerSamplingDuration,
         MinimumThroughput = CircuitBreakerMinThroughput,
         BreakDuration = CircuitBreakerBreakDuration,
         ShouldHandle = new PredicateBuilder()
                        .Handle<HttpRequestException>()
                        .Handle<TaskCanceledException>()
      };

   // ── HTTP pipeline options (for IHttpClientBuilder) ─────────────────────────

   internal static HttpRetryStrategyOptions HttpTooManyRequestsRetryOptions =>
      new()
      {
         MaxRetryAttempts = TooManyRequestsMaxRetries,
         BackoffType = DelayBackoffType.Exponential,
         UseJitter = true,
         Delay = TooManyRequestsDelay,
         ShouldHandle = args =>
            ValueTask.FromResult(
               args.Outcome.Exception is HttpRequestException { StatusCode: HttpStatusCode.TooManyRequests } ||
               args.Outcome.Result is { StatusCode: HttpStatusCode.TooManyRequests }),
         DelayGenerator = args =>
         {
            if (args.Outcome.Result is null || !args.Outcome.Result.Headers.TryGetValues("Retry-After", out var values))
            {
               return ValueTask.FromResult<TimeSpan?>(null);
            }

            var retryAfterValue = values.FirstOrDefault();

            if (int.TryParse(retryAfterValue, out var retryAfterSeconds))
            {
               return ValueTask.FromResult<TimeSpan?>(TimeSpan.FromSeconds(retryAfterSeconds));
            }

            if (!DateTimeOffset.TryParseExact(retryAfterValue,
                   "R",
                   CultureInfo.InvariantCulture,
                   DateTimeStyles.None,
                   out var retryAfterDate))
            {
               return ValueTask.FromResult<TimeSpan?>(null);
            }

            var retryDelay = retryAfterDate - DateTimeOffset.UtcNow;
            return ValueTask.FromResult<TimeSpan?>(retryDelay > TimeSpan.Zero ? retryDelay : TimeSpan.MinValue);
         }
      };

   internal static HttpRetryStrategyOptions HttpNetworkRetryOptions =>
      new()
      {
         MaxRetryAttempts = NetworkMaxRetries,
         BackoffType = DelayBackoffType.Exponential,
         UseJitter = true,
         Delay = NetworkRetryDelay,
         ShouldHandle = args =>
         {
            if (args.Outcome.Exception is HttpRequestException httpEx)
            {
               return ValueTask.FromResult((int)httpEx.StatusCode! >= 500 ||
                                           (int)httpEx.StatusCode! == 408);
            }

            return ValueTask.FromResult(args.Outcome.Result is not null &&
                                        (args.Outcome.Result.StatusCode == HttpStatusCode.RequestTimeout ||
                                         (int)args.Outcome.Result.StatusCode >= 500));
         }
      };

   internal static HttpCircuitBreakerStrategyOptions HttpCircuitBreakerOptions =>
      new()
      {
         FailureRatio = CircuitBreakerFailureRatio,
         SamplingDuration = CircuitBreakerSamplingDuration,
         MinimumThroughput = CircuitBreakerMinThroughput,
         BreakDuration = CircuitBreakerBreakDuration,
         ShouldHandle = args =>
         {
            if (args.Outcome.Exception is HttpRequestException or TaskCanceledException)
            {
               return ValueTask.FromResult(true);
            }

            return args.Outcome.Result is not null
               ? ValueTask.FromResult(!args.Outcome.Result.IsSuccessStatusCode)
               : ValueTask.FromResult(false);
         }
      };

   // ── Public API ─────────────────────────────────────────────────────────────

   public static ResiliencePipeline GetDefaultPipeline(
      this ResiliencePipelineProvider<string> resiliencePipelineProvider)
   {
      return resiliencePipelineProvider.GetPipeline(DefaultPipelineName);
   }
}