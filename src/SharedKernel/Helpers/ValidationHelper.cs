using System.Net;
using System.Net.Mail;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SharedKernel.Helpers;

/// <summary>
///     Collection of boolean validity checks for common formats (emails, IDs, IPs, phone numbers, etc.).
/// </summary>
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


    /// <summary>
    ///     Return true if the value is an absolute URI, optionally rejecting non-secure (http) schemes.
    /// </summary>
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

    /// <summary>
    ///     Return true if the value is a valid US Social Security Number.
    /// </summary>
    public static bool IsUsSocialSecurityNumber(string number)
    {
        return UsSocialSecurityNumber.IsMatch(number);
    }


    /// <summary>
    ///     Return true if the value is a valid email address.
    /// </summary>
    public static bool IsEmail(string email)
    {
        return MailAddress.TryCreate(email, out _);
    }

    /// <summary>
    ///     Return true if the value is a valid username (5-15 alphanumeric or underscore characters).
    /// </summary>
    public static bool IsUsername(string userName)
    {
        return Username.IsMatch(userName);
    }

    /// <summary>
    ///     Return true if the value is a valid Armenian social security (public services) number.
    /// </summary>
    public static bool IsArmeniaSocialSecurityNumber(string socialCardNumber)
    {
        return ArmeniaSocialSecurityNumber.IsMatch(socialCardNumber);
    }

    /// <summary>
    ///     Return true if the value is a valid Armenian ID card number.
    /// </summary>
    public static bool IsArmeniaIdCard(string idCard)
    {
        return ArmeniaIdCard.IsMatch(idCard);
    }

    /// <summary>
    ///     Return true if the value is a valid Armenian passport number.
    /// </summary>
    public static bool IsArmeniaPassportNumber(string passportNumber)
    {
        return ArmeniaPassportNumber.IsMatch(passportNumber);
    }

    /// <summary>
    ///     Return true if the value is a valid Armenian tax code.
    /// </summary>
    public static bool IsArmeniaTaxCode(string taxCode)
    {
        return ArmeniaTaxCode.IsMatch(taxCode);
    }

    /// <summary>
    ///     Return true if the value is a valid Armenian state registry number.
    /// </summary>
    public static bool IsArmeniaStateRegistryNumber(string stateRegistryNumber)
    {
        return ArmeniaStateRegistryNumber.IsMatch(stateRegistryNumber);
    }

    /// <summary>
    ///     Return true if the value matches Panda's formatted phone number pattern, e.g. "(374)12345678".
    /// </summary>
    public static bool IsPandaFormattedPhoneNumber(string phoneNumber)
    {
        return PandaFormattedPhoneNumber.IsMatch(phoneNumber);
    }

    /// <summary>
    ///     Return true if the value is a valid GUID.
    /// </summary>
    public static bool IsGuid(string guid)
    {
        return Guid.TryParse(guid, out _);
    }

    /// <summary>
    ///     Return true if the value is a valid IPv4 address.
    /// </summary>
    public static bool IsIPv4(string ipv4)
    {
        return IPAddress.TryParse(ipv4, out var address) && address.AddressFamily == AddressFamily.InterNetwork;
    }

    /// <summary>
    ///     Return true if the value is a valid IPv6 address.
    /// </summary>
    public static bool IsIPv6(string ipv6)
    {
        return IPAddress.TryParse(ipv6, out var address) && address.AddressFamily == AddressFamily.InterNetworkV6;
    }

    /// <summary>
    ///     Return true if the value is a valid IPv4 or IPv6 address.
    /// </summary>
    public static bool IsIpAddress(string ipAddress)
    {
        return IPAddress.TryParse(ipAddress, out _);
    }

    /// <summary>
    ///     Return true if the value is well-formed JSON.
    /// </summary>
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

    /// <summary>
    ///     Return true if the value is a numeric string of plausible length that passes the Luhn checksum.
    /// </summary>
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
