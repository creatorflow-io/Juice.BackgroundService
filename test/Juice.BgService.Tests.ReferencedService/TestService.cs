
using Microsoft.Extensions.Logging;

namespace Juice.BgService.Tests.ReferencedService
{
    public class TestService : BackgroundService
    {
        public TestService(ILogger<TestService> logger) : base(logger)
        {
        }
        public override Task<(bool Healthy, string Message)> HealthCheckAsync() => Task.FromResult((true, string.Empty));
        protected override Task ExecuteAsync()
        {
            _logger.LogInformation("TestService is running");
            return Task.CompletedTask;
        }
    }
}
