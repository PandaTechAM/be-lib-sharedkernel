namespace SharedKernel.Tests;

public sealed class TimeZoneMappingTests
{
   [Fact]
   public void Caucasus_and_AsiaYerevan_should_produce_same_local_time_for_same_utc_instants()
   {
      var caucasus = GetTimeZone("Caucasus Standard Time");
      var yerevan = GetTimeZone("Asia/Yerevan");

      var instantsUtc = new[]
      {
         new DateTime(2024, 01, 15, 00, 00, 00, DateTimeKind.Utc),
         new DateTime(2024, 07, 15, 12, 34, 56, DateTimeKind.Utc),
         new DateTime(2025, 12, 18, 08, 00, 00, DateTimeKind.Utc),
      };

      foreach (var utc in instantsUtc)
      {
         var local1 = TimeZoneInfo.ConvertTimeFromUtc(utc, caucasus);
         var local2 = TimeZoneInfo.ConvertTimeFromUtc(utc, yerevan);

         Assert.Equal(local1, local2);
         Assert.Equal(caucasus.GetUtcOffset(utc), yerevan.GetUtcOffset(utc));
      }
   }

   private static TimeZoneInfo GetTimeZone(string id)
   {
      if (TimeZoneInfo.TryFindSystemTimeZoneById(id, out var tz) ||
          TimeZoneInfo.TryConvertIanaIdToWindowsId(id, out var winId) &&
          TimeZoneInfo.TryFindSystemTimeZoneById(winId, out tz) ||
          TimeZoneInfo.TryConvertWindowsIdToIanaId(id, out var ianaId) &&
          TimeZoneInfo.TryFindSystemTimeZoneById(ianaId, out tz))
      {
         return tz;
      }

      throw new TimeZoneNotFoundException($"Could not resolve time zone id '{id}'.");
   }
}