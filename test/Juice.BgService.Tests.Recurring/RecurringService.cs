﻿using System.Diagnostics;
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
            ScheduleOptions.OccursInterval(TimeSpan.FromSeconds(3));
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
                new KeyValuePair<string, object>("JobId", jobId),
                new KeyValuePair<string, object>("JobDescription", "Invoke recurring task")
            }))
            {
                _logger.LogInformation("Hello... next invoke {0} time is {1}. Instances count: {2}", Description, NextProcessing, globalCounter);
                for (var i = 0; i < 10000; i++)
                {
                    if (_stopRequest.IsCancellationRequested) { break; }
                    _logger.LogInformation("Task {0}", i);
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
