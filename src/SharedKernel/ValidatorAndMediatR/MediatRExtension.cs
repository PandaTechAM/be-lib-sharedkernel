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
      builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(assemblies));
      builder.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviorWithoutResponse<,>));
      builder.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviorWithResponse<,>));
      builder.Services.AddValidatorsFromAssemblies(assemblies);
      return builder;
   }
}