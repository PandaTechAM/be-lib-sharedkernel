using CommissionCalculator.DTO;
using FluentValidation;
using FluentValidation.Validators;

namespace SharedKernel.ValidatorAndMediatR.Validators.Files;
public sealed class CommissionRangeValidator<T> : PropertyValidator<T, CommissionRule?>
{
   public override string Name => "CommissionRangeValidator";

   public override bool IsValid(ValidationContext<T> context, CommissionRule? rule)
   {
      var startRule = rule!.CommissionRangeConfigs.FirstOrDefault(r => r is { RangeStart: 0, RangeEnd: > 0 });
      if (startRule == null)
      {
         context.AddFailure("There should be at least one rule where From = 0.");
         return false;
      }

      if (startRule.MaxCommission != 0 && startRule.MaxCommission < startRule.MinCommission)
      {
         context.AddFailure("MaxCommission should be greater than or equal to MinCommission.");
         return false;
      }

      var verifiedRules = 1;

      var lastTo = startRule.RangeEnd;

      while (true)
      {
         var nextRule = rule.CommissionRangeConfigs.FirstOrDefault(r => r.RangeStart == lastTo);
         if (nextRule is null && lastTo != 0)
         {
            context.AddFailure($"Gap detected. No rule found for 'From = {lastTo}'.");
            return false;
         }

         if (nextRule is not null && nextRule.RangeStart == nextRule.RangeEnd)
         {
            context.AddFailure("Invalid rule. 'From' and 'To' cannot be equal.");
            return false;
         }

         if (nextRule is not null && nextRule.MaxCommission != 0 && nextRule.MaxCommission < nextRule.MinCommission)
         {
            context.AddFailure("MaxCommission should be greater than or equal to MinCommission.");
            return false;
         }

         if (lastTo == 0)
         {
            break;
         }

         verifiedRules++;

         lastTo = nextRule!.RangeEnd;
      }

      if (verifiedRules != rule.CommissionRangeConfigs.Count)
      {
         context.AddFailure("There is some nested or gap ranges in the rules.");
         return false;
      }

      return true; //check
   }
}
