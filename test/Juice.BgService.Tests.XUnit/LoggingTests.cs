﻿using FluentAssertions;
using Juice.BgService.Extensions.Logging;
using Juice.Extensions.DependencyInjection;
using Juice.XUnit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Juice.BgService.Tests.XUnit
{
    public class LoggingTests
    {
        private readonly ITestOutputHelper _output;

        public LoggingTests(ITestOutputHelper output)
        {
            _output = output;
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
        }

        [IgnoreOnCIFact(DisplayName = "Log file should by JobId"), TestPriority(999)]
        public async Task LogFile_should_by_jobIdAsync()
        {
            var resolver = new DependencyResolver
            {
                CurrentDirectory = AppContext.BaseDirectory
            };

            var logOptions = new FileLoggerOptions();
            resolver.ConfigureServices(services =>
            {
                var configService = services.BuildServiceProvider().GetRequiredService<IConfigurationService>();
                var configuration = configService.GetConfiguration();
                services.AddSingleton(provider => _output);
                services.AddLogging(builder =>
                {
                    builder.ClearProviders()
                    .AddTestOutputLogger()
                    .AddBgServiceFileLogger(configuration.GetSection("Logging:File"))
                    .AddConfiguration(configuration.GetSection("Logging"));
                });
                configuration.GetSection("Logging:File").Bind(logOptions);
            });
            var logPath = logOptions.Directory;
            var logTime = logOptions.BufferTime.Add(TimeSpan.FromMilliseconds(200));
            logPath.Should().NotBeNullOrEmpty();

            var serviceProvider = resolver.ServiceProvider;
            var logger = serviceProvider.GetRequiredService<ILogger<LoggingTests>>();

            var guid = Guid.NewGuid().ToString();
            using (logger.BeginScope(new Dictionary<string, object>
            {
                ["JobId"] = guid,
                ["JobDescription"] = "xUnit",
            }))
            {
                logger.LogInformation("Test {0}", "Start");
                logger.LogInformation("Test {0}", "Procssing");
            }

            await Task.Delay(logTime);
            var logFile = Path.Combine(logPath, "General", $"{guid} - xUnit.log");
            _output.WriteLine(logFile);
            File.Exists(logFile).Should().BeTrue();

            var newid = Guid.NewGuid().ToString();

            using (logger.BeginScope(new Dictionary<string, object>
            {
                ["JobId"] = newid,
                ["JobDescription"] = "xUnit",
                ["JobState"] = "Succeeded"
            }))
            {
                logger.LogInformation("Test {0}", "End");
            }

            await Task.Delay(logTime);
            var logFile2 = Path.Combine(logPath, "General", $"{newid} - xUnit_Succeeded.log");
            _output.WriteLine(logFile2);
            File.Exists(logFile2).Should().BeTrue();
            File.Exists(logFile).Should().BeTrue();

            using (logger.BeginScope(new Dictionary<string, object>
            {
                ["JobId"] = guid,
                ["JobDescription"] = "xUnit",
                ["JobState"] = "Succeeded"
            }))
            {
                logger.LogInformation("Test {0}", "End");
            }

            await Task.Delay(logTime);
            var logFile3 = Path.Combine(logPath, "General", $"{guid} - xUnit_Succeeded.log");
            _output.WriteLine(logFile3);
            File.Exists(logFile3).Should().BeTrue();
            File.Exists(logFile).Should().BeFalse();

            using (logger.BeginScope(new Dictionary<string, object>
            {
                ["JobId"] = guid,
                ["JobDescription"] = "xUnit",
                ["JobState"] = "Succeeded"
            }))
            {
                logger.LogInformation("Test {0}", "Rerun");
                logger.LogInformation("Test {0}", "End");
            }

            await Task.Delay(logTime);
            var logFile4 = Path.Combine(logPath, "General", $"{guid} - xUnit_Succeeded (1).log");
            _output.WriteLine(logFile4);
            File.Exists(logFile4).Should().BeTrue();
            File.Exists(logFile3).Should().BeTrue();
        }

    }
}
