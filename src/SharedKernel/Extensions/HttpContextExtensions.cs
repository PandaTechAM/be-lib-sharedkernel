using Microsoft.AspNetCore.Http;

namespace SharedKernel.Extensions;

/// <summary>
///     Extension methods for flagging a response as a private endpoint, excluding it from request logging.
/// </summary>
public static class HttpContextExtensions
{
    /// <summary>
    ///     Marks the current response as a private endpoint by appending the "X-Private-Endpoint" header.
    /// </summary>
    public static void MarkAsPrivateEndpoint(this HttpContext context)
    {
        context.Response.Headers.Append("X-Private-Endpoint", "1");
    }

    /// <summary>
    ///     Marks the response as a private endpoint by appending the "X-Private-Endpoint" header.
    /// </summary>
    public static void MarkAsPrivateEndpoint(this HttpResponse response)
    {
        response.Headers.Append("X-Private-Endpoint", "1");
    }

    /// <summary>
    ///     Marks the accessor's current response, if any, as a private endpoint.
    /// </summary>
    public static void MarkAsPrivateEndpoint(this IHttpContextAccessor contextAccessor)
    {
        contextAccessor.HttpContext?.Response.Headers.Append("X-Private-Endpoint", "1");
    }

    /// <summary>
    ///     Marks the accessor's current response, if any, as a private endpoint.
    /// </summary>
    public static void MarkAsPrivateEndpoint(this HttpContextAccessor contextAccessor)
    {
        contextAccessor.HttpContext?.Response.Headers.Append("X-Private-Endpoint", "1");
    }
}
