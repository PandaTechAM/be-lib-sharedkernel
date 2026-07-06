using System.Collections.Specialized;
using System.Web;

namespace SharedKernel.Helpers;

/// <summary>
///     Fluent builder for constructing URLs with query parameters.
/// </summary>
public static class UrlBuilder
{
    /// <summary>
    ///     Starts a new <see cref="Builder" /> for the given base URL.
    /// </summary>
    public static Builder Create(string baseUrl)
    {
        return new Builder(baseUrl);
    }

    /// <summary>
    ///     Accumulates query parameters against a base URL and builds the final URL string.
    /// </summary>
    public class Builder
    {
        private readonly NameValueCollection _queryParameters;
        private readonly UriBuilder _uriBuilder;

        /// <summary>
        ///     Initializes the builder from the given base URL, preserving any existing query parameters.
        /// </summary>
        public Builder(string baseUrl)
        {
            _uriBuilder = new UriBuilder(baseUrl);
            _queryParameters = HttpUtility.ParseQueryString(_uriBuilder.Query);
        }

        /// <summary>
        ///     Adds or replaces a query parameter and returns this builder for chaining.
        /// </summary>
        public Builder AddParameter(string key, string value)
        {
            _queryParameters[key] = value;
            return this;
        }

        /// <summary>
        ///     Builds the final URL string, including all added query parameters.
        /// </summary>
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
