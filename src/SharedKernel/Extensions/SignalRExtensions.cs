using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using ResponseCrafter.ExceptionHandlers.SignalR;
using Serilog;
using Serilog.Events;
using SharedKernel.Logging.Middleware;
using StackExchange.Redis;

namespace SharedKernel.Extensions;

/// <summary>
///     Extension members for registering SignalR on <see cref="WebApplicationBuilder" />.
/// </summary>
public static class SignalRExtensions
{
    extension(WebApplicationBuilder builder)
    {
        /// <summary>
        ///     Registers SignalR with logging/exception filters and the MessagePack protocol.
        /// </summary>
        public WebApplicationBuilder AddSignalR()
        {
            builder.AddSignalRWithFiltersAndMessagePack();
            return builder;
        }

        /// <summary>
        ///     Registers SignalR backed by a Redis backplane, using the given connection string and channel prefix,
        ///     for distributed scale-out across multiple instances.
        /// </summary>
        public WebApplicationBuilder AddDistributedSignalR(string redisUrl,
            string redisChannelName)
        {
            builder.AddSignalRWithFiltersAndMessagePack()
                .AddStackExchangeRedis(redisUrl,
                    options => { options.Configuration.ChannelPrefix = RedisChannel.Literal(redisChannelName); });


            return builder;
        }

        private ISignalRServerBuilder AddSignalRWithFiltersAndMessagePack()
        {
            return builder.Services
                .AddSignalR(o =>
                {
                    if (Log.Logger.IsEnabled(LogEventLevel.Information))
                    {
                        o.AddFilter<SignalRLoggingHubFilter>();
                    }

                    o.AddFilter<SignalRExceptionFilter>();
                })
                .AddMessagePackProtocol();
        }
    }
}
