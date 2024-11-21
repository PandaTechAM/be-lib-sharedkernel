using Microsoft.AspNetCore.Builder;

namespace SharedKernel.Extensions;

public static class TimeZoneExtensions
{
   private static TimeZoneInfo _timeZoneInfo = TimeZoneInfo.Local;

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

   public static DateTime ToDefaultTimeZone(this DateTime dateTime)
   {
      return TimeZoneInfo.ConvertTime(dateTime, _timeZoneInfo);
   }

   public static TimeZoneInfo GetDefaultTimeZone()
   {
      return _timeZoneInfo;
   }

   public static DateTime AdjustTimeForDefaultTimeZone(this DateTime dateTime)
   {
      var dateTimeOffset = new DateTimeOffset(dateTime);

      var sourceOffset = dateTimeOffset.Offset;
      var targetOffset = _timeZoneInfo.GetUtcOffset(dateTime);

      var difference = targetOffset - sourceOffset;
      return dateTime.Add(difference);
   }
}