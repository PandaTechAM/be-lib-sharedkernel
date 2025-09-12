using System.Text;

namespace SharedKernel.Helpers;

public static class PhoneUtil
{
   public static bool TryFormatArmenianMsisdn(string? input, out string? formatted)
   {
      formatted = null;
      if (string.IsNullOrWhiteSpace(input))
      {
         return false;
      }

      var s = input.Trim();

      if (s.StartsWith("(374)", StringComparison.Ordinal))
      {
         var rest = DigitsOnly(s.AsSpan(5));
         if (rest.Length == 8)
         {
            formatted = "+374" + rest;
            return true;
         }

         formatted = input;
         return false;
      }

      var sb = new StringBuilder(s.Length);
      for (var i = 0; i < s.Length; i++)
      {
         var c = s[i];
         if (c is >= '0' and <= '9')
         {
            sb.Append(c);
            continue;
         }

         if (i == 0 && c == '+')
         {
            continue;
         }

         if (c == ' ' || c == '-' || c == '.' || c == '(' || c == ')' || char.IsWhiteSpace(c))
         {
            continue;
         }

         formatted = input;
         return false;
      }

      var digits = sb.ToString();
      var span = digits.AsSpan();
      ReadOnlySpan<char> last8;

      var plusAtStart = s[0] == '+';
      var doubleZeroAtStart = s.StartsWith("00", StringComparison.Ordinal);

      if (plusAtStart)
      {
         if (span.Length == 11 && span.StartsWith("374".AsSpan()))
         {
            last8 = span[3..];
         }
         else
         {
            formatted = input;
            return false;
         }
      }
      else
      {
         // Handle "00" international prefix for Armenia: "00374" + 8 digits => total 13 digits
         if (doubleZeroAtStart && span.Length == 13 && span.StartsWith("00374".AsSpan()))
         {
            last8 = span[5..]; // skip "00" + "374"
         }
         else
         {
            switch (span.Length)
            {
               case 11 when span.StartsWith("374".AsSpan()):
               {
                  last8 = span[3..];
                  break;
               }
               case 9 when span[0] == '0':
               {
                  last8 = span[1..];
                  break;
               }
               case 8:
               {
                  last8 = span;
                  break;
               }
               default:
               {
                  formatted = input;
                  return false;
               }
            }
         }
      }

      if (last8.Length != 8)
      {
         formatted = input;
         return false;
      }

      formatted = $"+374{last8.ToString()}";
      return true;
   }

   private static string DigitsOnly(ReadOnlySpan<char> span)
   {
      var sb = new StringBuilder(span.Length);
      foreach (var c in span)
      {
         if (c is >= '0' and <= '9')
         {
            sb.Append(c);
         }
      }

      return sb.ToString();
   }
}