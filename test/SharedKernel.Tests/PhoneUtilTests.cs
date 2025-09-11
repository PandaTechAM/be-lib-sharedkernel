using SharedKernel.Helpers;

namespace SharedKernel.Tests;

public class PhoneUtilTests
{
   [Theory]
   [InlineData("+37493910593", "+37493910593")]
   [InlineData("37493910593", "+37493910593")]
   [InlineData("093910593", "+37493910593")]
   [InlineData("93910593", "+37493910593")]
   [InlineData("(374)93910593", "+37493910593")]
   // tolerant separators
   [InlineData("+374 93 910 593", "+37493910593")]
   [InlineData("374-93-910-593", "+37493910593")]
   [InlineData("(374) 93 910 593", "+37493910593")]
   public void Formats_Armenian_Numbers_When_Valid(string input, string expected)
   {
      var ok = PhoneUtil.TryFormatArmenianMsisdn(input, out var formatted);
      Assert.True(ok);
      Assert.Equal(expected, formatted);
   }

   [Theory]
   [InlineData("+12025550199")] // foreign
   [InlineData("441234567890")] // foreign
   [InlineData("+++37493910593")] // invalid
   [InlineData("37493A10593")] // invalid char
   [InlineData("(374)1234567")] // only 7 digits after prefix
   [InlineData("123456789")] // 9 digits but not starting with 0
   public void Returns_Original_When_Not_Armenian(string input)
   {
      var ok = PhoneUtil.TryFormatArmenianMsisdn(input, out var formatted);
      Assert.False(ok);
      Assert.Equal(input, formatted);
   }

   [Fact]
   public void Empty_String_Returns_False_And_Null()
   {
      var ok = PhoneUtil.TryFormatArmenianMsisdn("", out var formatted);
      Assert.False(ok);
      Assert.Null(formatted);
   }

   [Fact]
   public void Whitespace_String_Returns_False_And_Null()
   {
      var ok = PhoneUtil.TryFormatArmenianMsisdn("   ", out var formatted);
      Assert.False(ok);
      Assert.Null(formatted);
   }

   [Fact]
   public void Null_Returns_False_And_Null()
   {
      var ok = PhoneUtil.TryFormatArmenianMsisdn(null, out var formatted);
      Assert.False(ok);
      Assert.Null(formatted);
   }
}