using System.Diagnostics;
using Juice.BgService.Scheduled;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Juice.BgService.Tests
{
    public class RecurringService<TModel> : ScheduledService, IManagedService<TModel>
        where TModel : class, IServiceModel
    {
        public RecurringService(IServiceProvider serviceProvider) : base(serviceProvider.GetRequiredService<ILogger<RecurringService<TModel>>>())
        {
            ScheduleOptions.OccursInterval(TimeSpan.FromSeconds(30));
        }

        public void Configure(TModel model)
        {
            _logger.LogInformation("Configure {0}", JsonConvert.SerializeObject(model));
        }
        public override Task<(bool Healthy, string Message)> HealthCheckAsync() =>
            Task.FromResult((true, ""));

        public override async Task<(bool Succeeded, string? Message)> InvokeAsync()
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            _logger.LogInformation("Begin invoke");
            var jobId = Guid.NewGuid().ToString();
            using (_logger.BeginScope(new List<KeyValuePair<string, object>>
            {
                new KeyValuePair<string, object>("TraceId", jobId),
                new KeyValuePair<string, object>("Operation", "Invoke recurring task")
            }))
            {
                for (var i = 0; i < 10; i++)
                {
                    if (_stopRequest!.IsCancellationRequested) { break; }
                    using (_logger.BeginScope($"Task {i}"))
                    {
                        for (var j = 0; j < 100; j++)
                        {
                            _logger.LogInformation("Processing {0}", j);
                        }
                        using (_logger.BeginScope(new List<KeyValuePair<string, object>>
                        {
                            new KeyValuePair<string, object>("Contextual", "success")
                        }))
                        {
                            _logger.LogError("Task {0} failed", i);
                        }
                        using (_logger.BeginScope("Succeeded"))
                        {
                            _logger.LogInformation("Task {0} succeeded", i);
                        }
                    }
                }
                using (_logger.BeginScope(new List<KeyValuePair<string, object>>
                {
                    new KeyValuePair<string, object>("OperationState", "Succeeded")
                }))
                {
                    _logger.LogInformation("End");
                }

            }

            stopWatch.Stop();
            _logger.LogInformation("Invoked take {time}", stopWatch.Elapsed);
            _logger.LogInformation("Hello... next invoke {0} time is {1}. Instances count: {2}", Description, NextProcessing, globalCounter);

            return (true, default);
        }

    }
}
