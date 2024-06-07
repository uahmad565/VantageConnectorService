using ActiveDirectorySearcher;
using ActiveDirectorySearcher.DTOs;
using VantageConnectorService.DTOs;
using VantageConnectorService.GlobalObjects;
using VantageConnectorService.Helpers;

namespace VantageConnectorService
{
    public class ADSync : IDisposable
    {
        private bool _isTaskRunning = false;
        private CancellationTokenSource? _cancellationToken;
        private System.Timers.Timer _timer;
        private Progress<Status> progressReporter;
        //dependencies
        private readonly InputCreds _inputCreds;
        private readonly List<string> _containers;
        private readonly VantageInterval? _vantageInterval;
        private readonly List<ObjectType> _objectTypes;
        private readonly int _recordsToSyncInSingleRequest;

        public ADSync(InputCreds inputCreds, List<string> containers, VantageInterval? interval, List<ObjectType> objectTypes,int recordsToSyncInSingleRequest)
        {
            _cancellationToken = new();
            progressReporter = new Progress<Status>(st =>
            {
                string message = st.LogMessage + (string.IsNullOrEmpty(st.ResultMessage) ? "" : (" Result: " + st.ResultMessage));
                GlobalLogManager.Logger.WriteInfo(message);
            });

            _inputCreds = inputCreds;
            _containers = containers;
            _vantageInterval = interval;
            _objectTypes = objectTypes;
            _recordsToSyncInSingleRequest = recordsToSyncInSingleRequest;

        }
        public void OnStart()
        {
            ActiveDirectoryHelper.LoadOUReplication();
            ScheduleTask();
        }

        public void OnStop()
        {
            _cancellationToken?.Cancel();
            _timer?.Stop();
            _timer?.Dispose();
        }
        private void ScheduleTask()
        {
            DateTime now = DateTime.Now;
            DateTime targetTime = new DateTime(now.Year, now.Month, now.Day, _vantageInterval.SyncHour, _vantageInterval.SyncMinute, 0);//*test hour should be 24
            if (now > targetTime)
            {
                targetTime = targetTime.AddDays(1);
            }

            TimeSpan initialDelay = targetTime - now;
            _timer = new System.Timers.Timer(initialDelay.TotalMilliseconds);
            _timer.Elapsed += (sender, args) => TimerElapsed();
            _timer.AutoReset = false;
            _timer.Start();
        }

        private void TimerElapsed()
        {
            if (_vantageInterval.IsDaily || (_vantageInterval.DaysOfWeek.Contains(DateTime.Now.DayOfWeek)))
            {
                Tick();
            }
            Reschedule();
        }

        private void Reschedule()
        {
            DateTime now = DateTime.Now;
            DateTime nextRunTime;
            if (_vantageInterval.IsDaily)
            {
                nextRunTime = now.AddDays(1);
            }
            else
            {
                nextRunTime = now;
                do
                {
                    nextRunTime = nextRunTime.AddDays(1);
                } while (!_vantageInterval.DaysOfWeek.Contains(nextRunTime.DayOfWeek));
            }

            nextRunTime = new DateTime(nextRunTime.Year, nextRunTime.Month, nextRunTime.Day, _vantageInterval.SyncHour, _vantageInterval.SyncMinute, 0);
            TimeSpan interval = nextRunTime - now;
            _timer.Interval = interval.TotalMilliseconds;
            _timer.Start();
        }

        private async void Tick()
        {

            if (_isTaskRunning)
            {
                GlobalLogManager.Logger.WriteInfo("Replication Sync is already running, skipping this tick...");
                return;
            }

            try
            {
                _isTaskRunning = true;

                await ProcessObjects();
                _cancellationToken?.Token.ThrowIfCancellationRequested();
            }
            catch (Exception ex)
            {
                HandleError(ex);
                await ActiveDirectoryHelper.WriteOUReplication();

            }
            finally
            {
                _isTaskRunning = false;
            }
        }

        public async Task ProcessObjects()
        {
            await Task.Run(async () =>
            {
                if (_objectTypes.Contains(ObjectType.Group))
                {
                    GlobalLogManager.Logger.WriteInfo("Start Fetching Groups");
                    await ActiveDirectoryHelper.ProcessADObjects(_inputCreds, progressReporter, ObjectType.Group, _containers, _recordsToSyncInSingleRequest, _cancellationToken.Token);
                }
                if (_objectTypes.Contains(ObjectType.User))
                {
                    GlobalLogManager.Logger.WriteInfo("Start Fetching Users");
                    await ActiveDirectoryHelper.ProcessADObjects(_inputCreds, progressReporter, ObjectType.User, _containers, _recordsToSyncInSingleRequest, _cancellationToken.Token);
                }
                if (_objectTypes.Contains(ObjectType.OU))
                {

                }
                GlobalLogManager.Logger.WriteInfo("Finished Replication");
            });
        }
        private async void HandleError(Exception ex)
        {
            try
            {
                GlobalLogManager.Logger.WriteException(ex);
            }
            catch (Exception)
            {
            }
        }

        public void Dispose()
        {
            if (_timer != null)
            {
                _timer.Dispose();
            }
        }
    }
}
