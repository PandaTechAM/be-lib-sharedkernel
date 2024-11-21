using System.Text.Json;
using FluentValidation;
using FluentValidation.Validators;

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

      try
      {
         JsonDocument.Parse(value);
         return true;
      }
      catch (JsonException)
      {
         context.AddFailure("The input is not valid JSON.");
         return false;
      }
   }

   protected override string GetDefaultMessageTemplate(string errorCode)
   {
      return "Invalid JSON format.";
   }
}