using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace SharedKernel.Resilience;

/// <summary>
///     Extension methods for registering the shared default resilience pipeline.
/// </summary>
public static class ResilienceExtensions
{
    /// <summary>
    ///     Registers the default resilience pipeline for general (non-HttpClient) use.
    /// </summary>
    public static WebApplicationBuilder AddResilienceDefaultPipeline(this WebApplicationBuilder builder)
    {
        builder.Services.AddResiliencePipeline(ResilienceDefaultPipelineProvider.DefaultPipelineName,
            pipelineBuilder =>
            {
                pipelineBuilder.AddRetry(ResilienceDefaultPipelineProvider.DefaultNetworkRetryOptions)
                    .AddRetry(ResilienceDefaultPipelineProvider.TooManyRequestsRetryOptions)
                    .AddCircuitBreaker(ResilienceDefaultPipelineProvider.DefaultCircuitBreakerOptions)
                    .AddTimeout(ResilienceDefaultPipelineProvider.AttemptTimeout);
            });
        return builder;
    }

    /// <summary>
    ///     Adds the default resilience pipeline to an <see cref="IHttpClientBuilder" />.
    /// </summary>
    public static IHttpResiliencePipelineBuilder AddResilienceDefaultPipeline(this IHttpClientBuilder builder)
    {
        return builder.AddResilienceHandler("DefaultPipeline",
            resilienceBuilder =>
            {
                resilienceBuilder.AddRetry(ResilienceDefaultPipelineProvider.HttpTooManyRequestsRetryOptions)
                    .AddRetry(ResilienceDefaultPipelineProvider.HttpNetworkRetryOptions)
                    .AddCircuitBreaker(ResilienceDefaultPipelineProvider.HttpCircuitBreakerOptions)
                    .AddTimeout(ResilienceDefaultPipelineProvider.AttemptTimeout);
            });
    }
}
