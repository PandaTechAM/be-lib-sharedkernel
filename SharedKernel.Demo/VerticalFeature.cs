using FluentMinimalApiMapper;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.ValidatorAndMediatR.Validators;
using SharedKernel.ValidatorAndMediatR.Validators.Files;
using ICommand = SharedKernel.ValidatorAndMediatR.ICommand;

namespace SharedKernel.Demo;

public class VerticalFeature : IEndpoint
{
   public void AddRoutes(IEndpointRouteBuilder app)
   {
      app.MapPost("vertical",
            async ([AsParameters] VerticalCommand command, [FromServices] ISender sender) =>
            {
               await sender.Send(command);
               return Results.Ok();
            })
         .WithTags("vertical")
         .DisableAntiforgery();
   }
}

public class VerticalCommand : ICommand
{
   public IFormFile? Avatar { get; init; }
   public IFormFileCollection? Docs { get; init; }
}

public class VerticalCommandHandler : IRequestHandler<VerticalCommand>
{
   public Task Handle(VerticalCommand request, CancellationToken cancellationToken)
   {
      return Task.CompletedTask;
   }
}

public class VerticalCommandValidator : AbstractValidator<VerticalCommand>
{
   public VerticalCommandValidator()
   {
      RuleFor(x => x.Avatar)
         .HasMaxSizeMb(3)
         .ExtensionIn(".jpg", ".png");

      RuleFor(x => x.Docs)
         .MaxCount(4)
         .EachHasMaxSizeMb(4)
         .EachExtensionIn(CommonFileSets.Documents)
         .TotalSizeMaxMb(10);
   }
}