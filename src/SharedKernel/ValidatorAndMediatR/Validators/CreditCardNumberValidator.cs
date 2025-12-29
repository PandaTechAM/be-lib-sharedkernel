using FluentValidation;
using FluentValidation.Validators;
using SharedKernel.Helpers;

namespace SharedKernel.ValidatorAndMediatR.Validators;

public sealed class CreditCardNumberValidator<T> : PropertyValidator<T, string?>
{
   public override string Name => "CreditCardNumberValidator";

   public override bool IsValid(ValidationContext<T> context, string? value)
   {
      if (value is null)
      {
         return true;
      }

      if (ValidationHelper.IsCreditCardNumber(value))
      {
         return true;
      }

      context.AddFailure("credit_card_number_format_is_not_valid");
      return false;
   }

   protected override string GetDefaultMessageTemplate(string errorCode)
   {
      return "credit_card_number_format_is_not_valid";
   }
}