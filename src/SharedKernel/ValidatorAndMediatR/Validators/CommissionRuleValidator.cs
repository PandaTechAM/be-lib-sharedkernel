using CommissionCalculator.DTO;
using FluentValidation;
using FluentValidation.Validators;
using SharedKernel.ValidatorAndMediatR.Validators.Files;

namespace SharedKernel.ValidatorAndMediatR.Validators;

public sealed class CommissionRuleValidator<T> : PropertyValidator<T, CommissionRule?>
{
   public override string Name => "CommissionRuleValidator";

   public override bool IsValid(ValidationContext<T> context, CommissionRule? rule)
   {
      if (rule == null || rule.CommissionRangeConfigs.Count == 0)
      {
         context.AddFailure("The ranges list cannot be null or empty.");
         return false;
      }

      if (rule.CommissionRangeConfigs.Any(r =>
             r is { Type: CommissionType.Percentage, CommissionAmount: < -10 or > 10 }))
      {
         context.AddFailure(
            "For 'Percentage' CommissionType, the CommissionAmount should be between -10 and 10. Commissions over 1000% are not allowed.");
         return false;
      }

      if (rule.CommissionRangeConfigs.Count == 1)
      {
         if (rule.CommissionRangeConfigs[0].RangeStart != 0 || rule.CommissionRangeConfigs[0].RangeEnd != 0)
         {
            context.AddFailure("In case of one range, both 'From' and 'To' should be 0.");
            return false;
         }

         if (rule.CommissionRangeConfigs[0].MaxCommission == 0 || rule.CommissionRangeConfigs[0].MaxCommission >=
             rule.CommissionRangeConfigs[0].MinCommission)
         {
            return true; //check
         }

         context.AddFailure("MaxCommission should be greater than or equal to MinCommission.");
         return false;
      }

      var rangeValidator = new CommissionRangeValidator<T>();
      var rangeValidatorResult = rangeValidator.IsValid(context, rule);

      return rangeValidatorResult;
   }
}