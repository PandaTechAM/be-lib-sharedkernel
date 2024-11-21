using System.Collections.Specialized;
using System.Web;

namespace SharedKernel.Helpers;

public static class UrlBuilder
{
   public static Builder Create(string baseUrl)
   {
      return new Builder(baseUrl);
   }

   public class Builder
   {
      private readonly NameValueCollection _queryParameters;
      private readonly UriBuilder _uriBuilder;

      public Builder(string baseUrl)
      {
         _uriBuilder = new UriBuilder(baseUrl);
         _queryParameters = HttpUtility.ParseQueryString(_uriBuilder.Query);
      }

      public Builder AddParameter(string key, string value)
      {
         _queryParameters[key] = value;
         return this;
      }

      public string Build()
      {
         _uriBuilder.Query = _queryParameters.ToString();
         if (_uriBuilder.Uri.IsDefaultPort)
         {
            _uriBuilder.Port = -1;
         }

         return _uriBuilder.ToString();
      }
   }
}