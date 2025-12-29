using System.Net;
using System.Net.Mail;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SharedKernel.Helpers;

public static class ValidationHelper
{
   private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(50);


   private static readonly Regex Username =
      new(@"^[a-zA-Z0-9_]{5,15}$",
         RegexOptions.ExplicitCapture | RegexOptions.Compiled,
         RegexTimeout);


   private static readonly Regex PandaFormattedPhoneNumber =
      new(@"^\(\d{1,5}\)\d{4,15}$",
         RegexOptions.ExplicitCapture | RegexOptions.Compiled,
         RegexTimeout);

   private static readonly Regex UsSocialSecurityNumber =
      new(
         @"^4[0-9]{12}(?:[0-9]{3})?$",
         RegexOptions.ExplicitCapture | RegexOptions.Compiled,
         RegexTimeout);

   private static readonly Regex ArmeniaSocialSecurityNumber =
      new(@"^(\d{10}|Տ\d{3}\/\d{5}|S\d{3}A\d{5})$",
         RegexOptions.ExplicitCapture | RegexOptions.Compiled,
         RegexTimeout);

   private static readonly Regex ArmeniaIdCard =
      new(@"^\d{9}$",
         RegexOptions.ExplicitCapture | RegexOptions.Compiled,
         RegexTimeout);

   private static readonly Regex ArmeniaPassportNumber =
      new(@"^([A-Z]{2}\d{7}|\d{9})$",
         RegexOptions.ExplicitCapture | RegexOptions.Compiled,
         RegexTimeout);

   private static readonly Regex ArmeniaTaxCode =
      new(@"^\d{8}$",
         RegexOptions.ExplicitCapture | RegexOptions.Compiled,
         RegexTimeout);

   private static readonly Regex ArmeniaStateRegistryNumber =
      new(@"^\d{3}\.\d{3}\.\d{5,10}$",
         RegexOptions.ExplicitCapture | RegexOptions.Compiled,
         RegexTimeout);


   public static bool IsUri(string uri, bool allowNonSecure = true)

   {
      Uri.TryCreate(uri, UriKind.Absolute, out var parsedUri);

      if (parsedUri is null)
      {
         return false;
      }

      if (!allowNonSecure && parsedUri.Scheme == Uri.UriSchemeHttp)
      {
         return false;
      }

      return true;
   }

   public static bool IsUsSocialSecurityNumber(string number)
   {
      return UsSocialSecurityNumber.IsMatch(number);
   }


   public static bool IsEmail(string email)
   {
      return MailAddress.TryCreate(email, out _);
   }

   public static bool IsUsername(string userName)
   {
      return Username.IsMatch(userName);
   }

   public static bool IsArmeniaSocialSecurityNumber(string socialCardNumber)
   {
      return ArmeniaSocialSecurityNumber.IsMatch(socialCardNumber);
   }

   public static bool IsArmeniaIdCard(string idCard)
   {
      return ArmeniaIdCard.IsMatch(idCard);
   }

   public static bool IsArmeniaPassportNumber(string passportNumber)
   {
      return ArmeniaPassportNumber.IsMatch(passportNumber);
   }

   public static bool IsArmeniaTaxCode(string taxCode)
   {
      return ArmeniaTaxCode.IsMatch(taxCode);
   }

   public static bool IsArmeniaStateRegistryNumber(string stateRegistryNumber)
   {
      return ArmeniaStateRegistryNumber.IsMatch(stateRegistryNumber);
   }

   public static bool IsPandaFormattedPhoneNumber(string phoneNumber)
   {
      return PandaFormattedPhoneNumber.IsMatch(phoneNumber);
   }

   public static bool IsGuid(string guid)
   {
      return Guid.TryParse(guid, out _);
   }

   public static bool IsIPv4(string ipv4)
   {
      return IPAddress.TryParse(ipv4, out var address) && address.AddressFamily == AddressFamily.InterNetwork;
   }

   public static bool IsIPv6(string ipv6)
   {
      return IPAddress.TryParse(ipv6, out var address) && address.AddressFamily == AddressFamily.InterNetworkV6;
   }

   public static bool IsIpAddress(string ipAddress)
   {
      return IPAddress.TryParse(ipAddress, out _);
   }

   public static bool IsJson(string json)
   {
      if (string.IsNullOrWhiteSpace(json))
      {
         return false;
      }

      try
      {
         using var doc = JsonDocument.Parse(json);
         return true;
      }
      catch (JsonException)
      {
         return false;
      }
   }

   public static bool IsCreditCardNumber(string? value)
   {
      if (string.IsNullOrWhiteSpace(value))
      {
         return false;
      }

      var len = value.Length;
      if (len is < 13 or > 19)
      {
         return false;
      }

      var sum = 0;
      var doubleIt = false;

      for (var i = len - 1; i >= 0; i--)
      {
         var ch = value[i];
         if (ch is < '0' or > '9')
         {
            return false;
         }

         var d = ch - '0';

         if (doubleIt)
         {
            d *= 2;
            if (d > 9)
            {
               d -= 9;
            }
         }

         sum += d;
         doubleIt = !doubleIt;
      }

      return sum % 10 == 0;
   }
}