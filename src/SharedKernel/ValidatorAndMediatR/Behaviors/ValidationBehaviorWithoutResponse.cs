using FluentValidation;
using MediatR;

namespace SharedKernel.ValidatorAndMediatR.Behaviors;

/// <summary>
///     MediatR pipeline behavior that runs all registered FluentValidation validators for a request with no response
///     and throws an aggregated validation exception on failure.
/// </summary>
/// <param name="validators">The FluentValidation validators registered for <typeparamref name="TRequest" />.</param>
public class ValidationBehaviorWithoutResponse<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest
{
    /// <inheritdoc />
    public async Task<TResponse> Handle(TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
        {
            return await next(cancellationToken);
        }

        var context = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = results.SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count == 0)
        {
            return await next(cancellationToken);
        }

        throw ValidationAggregation.ToBadRequestException(failures);
    }
}
