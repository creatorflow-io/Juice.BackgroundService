using Juice.BgService.Extensions;
using Microsoft.Extensions.Logging;

namespace Juice.BgService.Scheduled
{
    public abstract class ScheduledService : BackgroundService
    {
        public DateTimeOffset? NextProcessing => _nextProcessing;
        private DateTimeOffset? _nextProcessing = null;

        public bool SkipWaiting { get; protected set; }

        private CancellationTokenSource? _waitCancel;


        protected ScheduledServiceOptions _scheduleOptions = new ScheduledServiceOptions();
        public virtual ScheduledServiceOptions ScheduleOptions => _scheduleOptions;

        public ScheduledService(ILogger logger) : base(logger)
        {
        }

        protected override async Task ExecuteAsync()
        {
            var lastLog = DateTimeOffset.Now;
            _nextProcessing = ScheduleOptions.Frequencies.NextOccursAt(null, true);
            _waitCancel = CancellationTokenSource.CreateLinkedTokenSource(_stopRequest!.Token);

            while (!_stopRequest.IsCancellationRequested)
            {
                try
                {
                    if (State == ServiceState.Stopping || State == ServiceState.Restarting || State == ServiceState.RestartPending)
                    {
                        break;
                    }
                    if (_nextProcessing.HasValue && _nextProcessing <= DateTimeOffset.Now)
                    {
                        _nextProcessing = ScheduleOptions.Frequencies.NextOccursAt(_nextProcessing);

                        State = ServiceState.Processing;
                        var invokeState = await InvokeAsync();
                        if (!invokeState.Succeeded)
                        {
                            _logger.FailedToInvoke(invokeState.Message, default!);
                        }

                        if (ScheduleOptions.Frequencies.Any(f => f.Occurs == OccursType.Once))
                        {
                            ScheduleOptions.Frequencies.Where(f => f.Occurs == OccursType.Once)
                                .Select(f => { f.Occurred(); return f; });


                            if (ScheduleOptions.Frequencies.All(f => f.Occurs == OccursType.Once || f.IsOccurred))
                            {

                                SetState(ServiceState.Stopping, "Service was configured occurs one time.", "");
                                break;
                            }

                        }

                        if (SkipWaiting)
                        {
                            SkipWaiting = false;
                            _nextProcessing = DateTimeOffset.Now;
                            await Task.Delay(300, _stopRequest.Token);
                            continue;
                        }
                        else
                        if (_nextProcessing <= DateTimeOffset.Now)
                        {
                            _nextProcessing = ScheduleOptions.Frequencies.NextOccursAt(_nextProcessing);
                        }

                        await WaitAsync(_waitCancel.Token);
                    }
                    else if (_nextProcessing.HasValue)
                    {
                        await WaitAsync(_waitCancel.Token);
                    }
                    else
                    {
                        _nextProcessing = ScheduleOptions.Frequencies.NextOccursAt(null, true);
                        await WaitAsync(_waitCancel.Token);
                    }
                }
                catch (TaskCanceledException)
                {
                }
                catch (Exception ex)
                {
                    _logger.FailedToInvoke(ex.Message, ex);
                }
            }
            if ((_shutdown?.IsCancellationRequested ?? false) || State == ServiceState.Stopping
                || State == ServiceState.Restarting || State == ServiceState.RestartPending)
            {
                State = ServiceState.Stopped;
            }
            else
            {
                State = ServiceState.StoppedUnexpectedly;
            }
        }

        private async Task WaitAsync(CancellationToken token)
        {
            if (State == ServiceState.Stopping || State == ServiceState.Restarting || State == ServiceState.RestartPending)
            {
                return;
            }
            State = ServiceState.Scheduled;
            try
            {
                if (_nextProcessing.HasValue)
                {
                    while (_nextProcessing.Value > DateTimeOffset.Now)
                    {
                        var sleep = _nextProcessing.Value - DateTimeOffset.Now;
                        var delay = Math.Max(Math.Min(sleep.TotalMilliseconds, 5000), 100);
                        await Task.Delay((int)delay, token);
                    }
                }
                else
                {
                    await Task.Delay(5000, token);
                }
            }
            catch (Exception)
            {
                if (!token.IsCancellationRequested)
                {
                    throw;
                }
            }
        }

        public void CancelWaiting()
        {
            _waitCancel?.Cancel();
        }

        public abstract Task<(bool Succeeded, string? Message)> InvokeAsync();

        protected override void Dispose(bool disposing)
        {
            _waitCancel?.Dispose();
            base.Dispose(disposing);
        }
    }

}
