using FluentValidation;
using FluentValidation.Validators;
using SharedKernel.Helpers;

namespace SharedKernel.ValidatorAndMediatR.Validators;

public class JsonValidator<T> : PropertyValidator<T, string>
{
   public override string Name => "JsonValidator";

   public override bool IsValid(ValidationContext<T> context, string? value)
   {
      if (value is null)
      {
         return true;
      }

      var isJson = ValidationHelper.IsJson(value);
      if (isJson)
      {
         return true;
      }

      context.AddFailure("The input is not valid JSON.");
      return false;
   }

   protected override string GetDefaultMessageTemplate(string errorCode)
   {
      return "Invalid JSON format.";
   }
}