using Ganss.Xss;

namespace SharedKernel.ValidatorAndMediatR.Validators;

internal static class XssSanitizer
{
   private static readonly HtmlSanitizer Sanitizer = new();

   static XssSanitizer()
   {
      var allowedTags = new[]
      {
         "p",
         "strong",
         "span",
         "b",
         "i",
         "u",
         "em",
         "br",
         "div",
         "ul",
         "ol",
         "li",
         "blockquote",
         "h1",
         "h2",
         "h3",
         "h4",
         "h5",
         "h6",
         "table",
         "thead",
         "tbody",
         "tr",
         "th",
         "td",
         "img",
         "a"
      };
      Sanitizer.AllowedTags.UnionWith(allowedTags);

      var allowedAttributes = new[]
      {
         "class",
         "style",
         "href",
         "src",
         "alt",
         "title",
         "width",
         "height",
         "align"
      };
      Sanitizer.AllowedAttributes.UnionWith(allowedAttributes);

      // Enable inline styles
      Sanitizer.AllowCssCustomProperties = true;
      Sanitizer.AllowedCssProperties.UnionWith([
         "color",
         "font-size",
         "font-weight",
         "font-style",
         "text-decoration",
         "background-color",
         "border",
         "padding",
         "margin",
         "text-align",
         "line-height",
         "white-space",
         "word-break",
         "width",
         "height",
         "max-width",
         "max-height"
      ]);

      Sanitizer.AllowedSchemes.Add("https");
      Sanitizer.AllowedSchemes.Add("http");
   }

   public static string Sanitize(string input)
   {
      return string.IsNullOrWhiteSpace(input) ? input : Sanitizer.Sanitize(input);
   }
}