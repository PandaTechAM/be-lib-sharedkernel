namespace SharedKernel.Extensions;

/// <summary>
///     String extension methods.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    ///     Strips parentheses and the leading "+" addition sign from a formatted phone number.
    /// </summary>
    public static string RemovePhoneFormatParenthesesAndAdditionSign(this string phoneString)
    {
        return phoneString.Replace("(", "")
            .Replace(")", "")
            .Replace("+", "");
    }
}
