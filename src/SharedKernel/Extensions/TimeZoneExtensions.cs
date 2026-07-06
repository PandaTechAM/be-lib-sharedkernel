using Microsoft.AspNetCore.Builder;

namespace SharedKernel.Extensions;

/// <summary>
///     Extension methods for configuring and applying the application's default time zone.
/// </summary>
public static class TimeZoneExtensions
{
    private static TimeZoneInfo _timeZoneInfo = TimeZoneInfo.Local;

    /// <summary>
    ///     Reads the default time zone id from configuration and caches it for subsequent conversions.
    /// </summary>
    public static WebApplicationBuilder MapDefaultTimeZone(this WebApplicationBuilder builder)
    {
        var defaultTimeZoneId = builder.Configuration.GetDefaultTimeZone();

        if (string.IsNullOrWhiteSpace(defaultTimeZoneId))
        {
            return builder;
        }

        _timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(defaultTimeZoneId);


        return builder;
    }

    /// <summary>
    ///     Converts the given <see cref="DateTime" /> to the configured default time zone.
    /// </summary>
    public static DateTime ToDefaultTimeZone(this DateTime dateTime)
    {
        return TimeZoneInfo.ConvertTime(dateTime, _timeZoneInfo);
    }

    /// <summary>
    ///     Returns the currently configured default time zone.
    /// </summary>
    public static TimeZoneInfo GetDefaultTimeZone()
    {
        return _timeZoneInfo;
    }

    /// <summary>
    ///     Shifts the given <see cref="DateTime" /> by the offset difference between its own offset and the
    ///     default time zone's offset, adjusting the wall-clock value without changing its <c>Kind</c>.
    /// </summary>
    public static DateTime AdjustTimeForDefaultTimeZone(this DateTime dateTime)
    {
        var dateTimeOffset = new DateTimeOffset(dateTime);

        var sourceOffset = dateTimeOffset.Offset;
        var targetOffset = _timeZoneInfo.GetUtcOffset(dateTime);

        var difference = targetOffset - sourceOffset;
        return dateTime.Add(difference);
    }
}
