using Microsoft.AspNetCore.SignalR;
using ResponseCrafter.ExceptionHandlers.SignalR;

namespace SharedKernel.Demo;

public class MessageHub : Hub
{
   public async Task SendMessage(SendMessageRequest message)
   {
      await Clients.All.SendAsync("ReceiveMessage", "Thanks for the message that I have received");
   }
}

public class SendMessageRequest : IHubArgument
{
   public required string InvocationId { get; set; }
   public required string Message { get; set; }
}