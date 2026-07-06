using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.ValidatorAndMediatR.Behaviors;

namespace SharedKernel.ValidatorAndMediatR;

/// <summary>
///     Registers MediatR and the FluentValidation validation pipeline behavior.
/// </summary>
public static class MediatrExtension
{
    /// <summary>
    ///     Registers MediatR handlers and FluentValidation validators from the given assemblies, and wires up the
    ///     validation pipeline behavior.
    /// </summary>
    public static WebApplicationBuilder AddMediatrWithBehaviors(this WebApplicationBuilder builder,
        Assembly[] assemblies)
    {
        builder.Services
            .AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(assemblies))
            .AddValidatorsFromAssemblies(assemblies, includeInternalTypes: true)
            .AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return builder;
    }
}
