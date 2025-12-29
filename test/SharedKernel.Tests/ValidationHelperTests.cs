using SharedKernel.Helpers;

namespace SharedKernel.Tests;

public class ValidationHelperTests
{
   [Theory]
   [InlineData("example@example.com", true)]
   [InlineData("firstname.lastname@example.com", true)]
   [InlineData("email@subdomain.example.com", true)]
   [InlineData("firstname+lastname@example.com", true)]
   [InlineData("1234567890@example.com", true)]
   [InlineData("email@123.123.123.123", true)]
   [InlineData("email@[123.123.123.123]", true)]
   [InlineData("plainaddress@example.com", true)]
   [InlineData("email@localhost", true)]
   [InlineData("firstname-lastname@example.com", true)]
   [InlineData("plainaddress", false)]
   [InlineData("@missingusername.com", false)]
   [InlineData("username@.com", false)]
   [InlineData("username@.com.com", false)]
   [InlineData("username@server..com", true)]
   [InlineData(".username@example.com", false)]
   [InlineData("username@example.com.", true)]
   [InlineData("username@.example.com", false)]
   [InlineData("username@.example..com", false)]
   [InlineData("username@example..com", true)]
   [InlineData("username@example.c", true)]
   [InlineData("username@example#.com", true)]
   [InlineData("username@example@com.com", false)]
   public void IsEmailTests(string email, bool expected)
   {
      var isValid = ValidationHelper.IsEmail(email);
      Assert.Equal(expected, isValid);
   }

   [Theory]
   [InlineData("username", true)]
   [InlineData("username@", false)]
   [InlineData("12345", true)]
   [InlineData("user", false)]
   public void IsUsernameTests(string username, bool expected)
   {
      var isValid = ValidationHelper.IsUsername(username);
      Assert.Equal(expected, isValid);
   }

   [Theory]
   [InlineData("123-45-6789", false)]
   [InlineData("987-65-4321", false)]
   [InlineData("555-55-5555", false)]
   [InlineData("111-11-1111", false)]
   [InlineData("123-45-678", false)] // Missing a digit
   [InlineData("9876-65-4321", false)] // Extra digit before first hyphen
   [InlineData("555-555-5555", false)] // Extra digit between hyphens
   [InlineData("00-00-00002", false)] // Extra digit at the end
   [InlineData("1111-11-1111", false)] // Extra digit at the start
   [InlineData("123-45-6789a", false)] // Letter at the end
   [InlineData("a23-45-6789", false)] // Letter at the start
   [InlineData("123-45-67890", false)] // Extra digit at the end
   [InlineData("123456789", false)] // No hyphens
   [InlineData("xyz-ab-cdef", false)] // Not even close to SSN format
   public void IsUsSocialSecurityNumberTests(string number, bool expected)
   {
      var isValid = ValidationHelper.IsUsSocialSecurityNumber(number);
      Assert.Equal(expected, isValid);
   }

      [Theory]
      [InlineData("4111111111111111", true)] // Visa
      [InlineData("5555555555554444", true)] // MasterCard
      [InlineData("378282246310005", true)] // American Express
      [InlineData("6011111111111117", true)] // Discover
      [InlineData("3530111333300000", true)] // JCB
      [InlineData("411111111111111", false)] // too short
      [InlineData("55555555555544444", false)] // too long
      [InlineData("37828224631005", false)] // too short
      [InlineData("601111111111111", false)] // too short
      [InlineData("353011133330000", false)] // too short
      [InlineData("1234567890123456", false)] // fails Luhn
      [InlineData("abcdefabcdefabc", false)] // non-numeric
      [InlineData("4111-1111-1111-1111", false)] // separators not allowed
      [InlineData("4111 1111 1111 1111", false)] // separators not allowed
      [InlineData("4578900000014055", true)]
      [InlineData("4578906000014055", false)]
      
      public void IsCreditCardNumberTests(string number, bool expected)
      {
         Assert.Equal(expected, ValidationHelper.IsCreditCardNumber(number));
      }

   [Theory]
   [InlineData("http://google.com", true)]
   [InlineData("https://google.com", true)]
   [InlineData("https://google.org", true)]
   [InlineData("https://openai.com", true)]
   [InlineData("http://example.travel", true)]
   [InlineData("https://example.co.uk", true)]
   [InlineData("google.com", false)]
   [InlineData("https://", false)]
   [InlineData("https://example.invalidTLD", true)]
   [InlineData("http://*.google.com", false)]
   [InlineData("http://sub.google.com", true)]
   [InlineData("https://*.openai.com", false)]
   [InlineData("*.google.com", false)]
   [InlineData("http://openai.com/vazgen?a=6456", true)]
   [InlineData("https://react.pandatech.it:8000/fallback?fallbackType=fdasdf", true)]
   public void IsUrlTest(string value, bool expectedResult)
   {
      Assert.Equal(expectedResult, ValidationHelper.IsUri(value));
   }

   [Theory]
   [InlineData("https://google.com", true)]
   [InlineData("https://google.org", true)]
   [InlineData("https://openai.com/vazgen", true)]
   [InlineData("http://example.travel", false)]
   [InlineData("https://example.co.uk", true)]
   [InlineData("google.com", false)]
   [InlineData("https://", false)]
   [InlineData("http://*.google.com", false)]
   [InlineData("http://sub.google.com", false)]
   [InlineData("https://*.openai.com", false)]
   [InlineData("*.google.com", false)]
   [InlineData("http://google.com", false)]
   [InlineData("ftp://google.com", true)]
   [InlineData("https://openai.com/vazgen?a=6456", true)]
   [InlineData("https://react.pandatech.it:8000/fallback?fallbackType=fdasdf", true)]
   public void IsSecureUrlTest(string value, bool expectedResult)
   {
      Assert.Equal(expectedResult, ValidationHelper.IsUri(value, false));
   }


   [Theory]
   [InlineData("0000000000", true)]
   [InlineData("0004000500", true)]
   [InlineData("123456789", false)]
   [InlineData("12345678ա", false)]
   [InlineData("123s567890a", false)]
   [InlineData("S12367890", false)]
   [InlineData("Տ123/45647", true)]
   [InlineData("s123/45647", false)]
   [InlineData("Տ12345647", false)]
   [InlineData("123/45647", false)]
   [InlineData("Տ123/456476", false)]
   [InlineData("S12/545647", false)]
   [InlineData("S023A95647", true)]
   [InlineData("S023A#5647", false)]
   [InlineData("S023A95647#", false)]
   [InlineData("S023A956.7", false)]
   [InlineData("Տ023A95647", false)]
   [InlineData("231654A64", false)]
   [InlineData("Տ0239A5647", false)]
   [InlineData("Տ123/45647S023A95647", false)]
   public void IsArmeniaSocialSecurityNumberTest(string value, bool expectedResult)
   {
      Assert.Equal(expectedResult, ValidationHelper.IsArmeniaSocialSecurityNumber(value));
   }

   [Theory]
   [InlineData("123456789", true)]
   [InlineData("12345678", false)]
   [InlineData("1234567890", false)]
   [InlineData("12345678a", false)]
   [InlineData("123456789a", false)]
   [InlineData("023456780", true)]
   public void IsArmeniaIdCardTest(string value, bool expectedResult)
   {
      Assert.Equal(expectedResult, ValidationHelper.IsArmeniaIdCard(value));
   }

   [Theory]
   [InlineData("AB1234567", true)] // valid format with letters
   [InlineData("XY9876543", true)] // valid format with letters
   [InlineData("123456789", true)] // valid format just numbers
   [InlineData("AB123456", false)] // missing a digit with letters
   [InlineData("A12345678", false)] // only one letter
   [InlineData("AB12345678", false)] // extra digit with letters
   [InlineData("12345678", false)] // missing a digit
   [InlineData("1234567890", false)] // extra digit
   [InlineData("Ab1234567", false)] // lowercase letter
   [InlineData("AB123456Z", false)] // non-numeric character at the end
   [InlineData("A B1234567", false)] // space between characters
   [InlineData("", false)]
   public void IsArmeniaPassportNumberTest(string value, bool expectedResult)
   {
      Assert.Equal(expectedResult, ValidationHelper.IsArmeniaPassportNumber(value));
   }

   [Theory]
   [InlineData("12345678", true)]
   [InlineData("12354608", true)]
   [InlineData("12345678901", false)]
   [InlineData("02345670", true)]
   [InlineData("5478946", false)]
   public void IsArmeniaTaxCodeTest(string value, bool expectedResult)
   {
      Assert.Equal(expectedResult, ValidationHelper.IsArmeniaTaxCode(value));
   }

   [Theory]
   [InlineData("123.456.78901", true)] // valid format
   [InlineData("987.654.321013249", true)] // valid format
   [InlineData("000.000.00000", true)] // valid format (border case)
   [InlineData("123456.78901", false)] // missing dot
   [InlineData("123.45678901", false)] // missing dot
   [InlineData("123.456.78901212365", false)] // too many numbers after last dot
   [InlineData("12.456.78901", false)] // less numbers before first dot
   [InlineData("123.45.78901", false)] // less numbers between dots
   [InlineData("123.456.7890", false)] // less numbers after last dot
   [InlineData("a23.456.78901", false)] // contains non-numeric character
   [InlineData("123.456.7890a", false)] // contains non-numeric character at the end
   [InlineData("290.110.1269513", true)]
   [InlineData("", false)] // empty string
   public void IsArmeniaStateRegistryNumberTest(string value, bool expectedResult)
   {
      Assert.Equal(expectedResult, ValidationHelper.IsArmeniaStateRegistryNumber(value));
   }

   [Theory]
   [InlineData("(123)45678901", true)]
   [InlineData("987(654)013249", false)]
   [InlineData("374(93910593)", false)]
   [InlineData("12345678901", false)]
   [InlineData("(374374)1234", false)]
   [InlineData("(374)12345123123123213678901", false)]
   [InlineData("(374)123", false)]
   public void IsPandaFormattedPhoneNumberTest(string value, bool expectedResult)
   {
      Assert.Equal(expectedResult, ValidationHelper.IsPandaFormattedPhoneNumber(value));
   }

   [Theory]
   [InlineData("00000000-0000-0000-0000-000000000000", true)]
   [InlineData("C56A4180-65AA-42EC-A945-5FD21DEC0538", true)]
   [InlineData("not-a-guid", false)]
   [InlineData("", false)]
   public void IsTestIsGuid(string input, bool expected)
   {
      var result = ValidationHelper.IsGuid(input);
      Assert.Equal(expected, result);
   }

   [Theory]
   [InlineData("192.168.1.1", true)]
   [InlineData("255.255.255.255", true)]
   [InlineData("256.256.256.256", false)]
   [InlineData("::1", false)]
   [InlineData("not-an-ip", false)]
   [InlineData("", false)]
   public void IsTestIsIPv4(string input, bool expected)
   {
      var result = ValidationHelper.IsIPv4(input);
      Assert.Equal(expected, result);
   }

   [Theory]
   [InlineData("::1", true)]
   [InlineData("2001:0db8:85a3:0000:0000:8a2e:0370:7334", true)]
   [InlineData("192.168.1.1", false)]
   [InlineData("not-an-ip", false)]
   [InlineData("", false)]
   public void IsTestIsIPv6(string input, bool expected)
   {
      var result = ValidationHelper.IsIPv6(input);
      Assert.Equal(expected, result);
   }

   [Theory]
   [InlineData("192.168.1.1", true)]
   [InlineData("::1", true)]
   [InlineData("256.256.256.256", false)]
   [InlineData("not-an-ip", false)]
   [InlineData("", false)]
   public void IsTestIsIpAddress(string input, bool expected)
   {
      var result = ValidationHelper.IsIpAddress(input);
      Assert.Equal(expected, result);
   }

   [Theory]
   [InlineData("{\"name\":\"John\", \"age\":30}", true)] // Valid JSON object
   [InlineData("[{\"name\":\"John\"}, {\"name\":\"Jane\"}]", true)] // Valid JSON array
   [InlineData("\"Just a string\"", true)] // Valid JSON string
   [InlineData("12345", true)] // Valid JSON number
   [InlineData("true", true)] // Valid JSON boolean
   [InlineData("null", true)] // Valid JSON null
   [InlineData("{name:\"John\", age:30}", false)] // Missing quotes around property names
   [InlineData("[{\"name\":\"John\", \"age\":30}", false)] // Missing closing bracket
   [InlineData("{\"name\":\"John\", \"age\":30", false)] // Missing closing brace
   [InlineData("Just a string", false)] // Not a JSON string (missing quotes)
   [InlineData("", false)] // Empty string
   public void IsTestIsJson(string input, bool expected)
   {
      var result = ValidationHelper.IsJson(input);
      Assert.Equal(expected, result);
   }
}