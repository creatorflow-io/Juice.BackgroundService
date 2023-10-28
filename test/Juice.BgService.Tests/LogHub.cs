using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Juice.BgService.Tests
{
    public class LogHub : Hub
    {
        public async Task LoggingAsync(Guid serviceId, string? jobId, string message, LogLevel level, string? contextual, string[] scopes)
        {
            try
            {
                await Clients.Others.SendAsync("LoggingAsync", serviceId, jobId, message, level, contextual, scopes);
            }
            catch { }
        }

        public async Task StateAsync(Guid serviceId, string? jobId, string state, string message)
        {
            try
            {
                await Clients.Others.SendAsync("StateAsync", serviceId, jobId, state, message);
            }
            catch { }
        }
    }
}
