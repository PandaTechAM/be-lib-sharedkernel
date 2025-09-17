using FluentValidation.Results;
using ResponseCrafter.HttpExceptions;

namespace SharedKernel.ValidatorAndMediatR.Behaviors;

internal static class ValidationAggregation
{
   public static (string? Message, Dictionary<string, string>? Errors) ToMessageAndErrors(
      IEnumerable<ValidationFailure> failures)
   {
      var globalMessages = new List<string>();
      var errors = new Dictionary<string, string>(StringComparer.Ordinal);

      foreach (var f in failures)
      {
         var prop = (f.PropertyName ?? string.Empty).Trim();

         // Global messages (no property name) -> headline message
         if (string.IsNullOrEmpty(prop))
         {
            if (!string.IsNullOrWhiteSpace(f.ErrorMessage))
            {
               globalMessages.Add(f.ErrorMessage);
            }

            continue;
         }

         // Per-property errors: keep the first message per property for brevity
         if (!errors.ContainsKey(prop))
         {
            errors[prop] = f.ErrorMessage;
         }
      }

      var message = globalMessages.Count > 0
         ? string.Join(" | ", globalMessages.Distinct())
         : null;

      return (message, errors.Count > 0 ? errors : null);
   }

   public static BadRequestException ToBadRequestException(IEnumerable<ValidationFailure> failures)
   {
      var (message, errors) = ToMessageAndErrors(failures);

      if (!string.IsNullOrWhiteSpace(message) && errors is not null)
      {
         return new BadRequestException(message, errors);
      }

      if (!string.IsNullOrWhiteSpace(message))
      {
         return new BadRequestException(message);
      }

      if (errors is not null)
      {
         return new BadRequestException(errors);
      }

      // Should not happen if we had failures, but just in case:
      return new BadRequestException("validation_failed");
   }
}