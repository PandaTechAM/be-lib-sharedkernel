using Microsoft.AspNetCore.Http;

namespace SharedKernel.Extensions;

public static class HttpContextExtensions
{
   public static void MarkAsPrivateEndpoint(this HttpContext context)
   {
      context.Response.Headers.Append("X-Private-Endpoint", "1");
   }

   public static void MarkAsPrivateEndpoint(this HttpResponse response)
   {
      response.Headers.Append("X-Private-Endpoint", "1");
   }

   public static void MarkAsPrivateEndpoint(this HttpContextAccessor contextAccessor)
   {
      contextAccessor.HttpContext?.Response.Headers.Append("X-Private-Endpoint", "1");
   }
}