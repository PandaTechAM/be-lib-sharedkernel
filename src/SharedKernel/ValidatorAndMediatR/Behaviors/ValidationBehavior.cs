using FluentValidation;
using MediatR;
using ResponseCrafter.HttpExceptions;

namespace SharedKernel.ValidatorAndMediatR.Behaviors;

/// <summary>
///     MediatR pipeline behavior that runs all registered FluentValidation validators for a request and throws a
///     <see cref="ResponseCrafter.HttpExceptions.BadRequestException" /> on failure.
/// </summary>
/// <param name="validators">The FluentValidation validators registered for <typeparamref name="TRequest" />.</param>
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    private readonly IValidator<TRequest>[] _validators =
        validators as IValidator<TRequest>[] ?? validators.ToArray();

    /// <inheritdoc />
    public async Task<TResponse> Handle(TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (_validators.Length == 0)
        {
            return await next(cancellationToken);
        }

        var context = new ValidationContext<TRequest>(request);
        var failures = (await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken))
            ))
            .SelectMany(r => r.Errors)
            .Where(e => e is not null)
            .ToList();

        if (!failures.Any())
        {
            return await next(cancellationToken);
        }

        var errorMap = failures
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key,
                g => g.Select(e => e.ErrorMessage)
                    .Distinct()
                    .First());

        throw new BadRequestException(errorMap);
    }
}
