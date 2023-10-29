using System.Diagnostics;
using Juice.BgService.Scheduled;
using Juice.BgService.Tests.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Juice.BgService.Tests
{
    public class RecurringService1 : ScheduledService, IManagedService<CustomServiceModel>
    {
        public RecurringService1(IServiceProvider serviceProvider) : base(serviceProvider.GetRequiredService<ILogger<RecurringService1>>())
        {
            ScheduleOptions.OccursInterval(TimeSpan.FromSeconds(3));
        }

        public void Configure(CustomServiceModel model)
        {
            _logger.LogInformation("Configure {model}", JsonConvert.SerializeObject(model));
        }
        public override Task<(bool Healthy, string Message)> HealthCheckAsync() =>
            Task.FromResult((true, ""));

        public override async Task<(bool Succeeded, string? Message)> InvokeAsync()
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            _logger.LogInformation("Begin invoke");
            var jobId = Guid.NewGuid();
            using (_logger.BeginScope(new List<KeyValuePair<string, object>>
            {
                new KeyValuePair<string, object>("JobId", jobId),
                new KeyValuePair<string, object>("JobDescription", "Invoke recurring task")
            }))
            {
                _logger.LogInformation("Hello... next invoke {description} time is {time}. Instances count: {count}", Description, NextProcessing, globalCounter);
                for (var i = 0; i < 10000; i++)
                {
                    if (_stopRequest!.IsCancellationRequested) { break; }
                    _logger.LogInformation("Task {i}", i);
                }
                _logger.LogInformation("End");
            }

            using (_logger.BeginScope(new List<KeyValuePair<string, object>>
            {
                new KeyValuePair<string, object>("JobId", jobId),
                new KeyValuePair<string, object>("JobDescription", "Invoke recurring task"),
                new KeyValuePair<string, object>("JobState", "Succeeded")
            }))
            {
                _logger.LogInformation("End");
            }

            stopWatch.Stop();
            _logger.LogInformation("Invoked take {time}", stopWatch.Elapsed);
            return (true, default);
        }

    }
}
