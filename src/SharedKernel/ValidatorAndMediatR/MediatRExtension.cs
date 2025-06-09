using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.ValidatorAndMediatR.Behaviors;

namespace SharedKernel.ValidatorAndMediatR;

public static class MediatrExtension
{
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