using ActiveDirectorySearcher;
using ActiveDirectorySearcher.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Timers;
using VantageConnectorService.DTOs;
using VantageConnectorService.Helpers;

namespace VantageConnectorService
{
    public class ADSync : IDisposable
    {
        private bool _isTaskRunning = false;
        private InputCreds _inputCreds;
        private CustomLogger _customLogger;
        public System.Timers.Timer _timer;
        private Progress<Status> progressReporter;
        private List<string> _containers;
        private CancellationTokenSource? _cancellationToken;

        //Scheduler Attributes
        private VantageInterval _vantageInterval;

        public ADSync(InputCreds inputCreds, List<string> containers, VantageInterval interval)
        {
            _cancellationToken = new();
            _inputCreds = inputCreds;
            _containers = containers;
            _vantageInterval = interval;
            _customLogger = new CustomLogger("logs.txt", "");
            progressReporter = new Progress<Status>(st =>
            {
                _customLogger.WriteInfo(st.LogMessage + " Result: " + st.ResultMessage);
            });
        }
        public void OnStart()
        {
            ScheduleTask();
        }

        public void OnStop()
        {
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
            _timer.AutoReset = false;
            _timer.Start();
        }

        private async void Tick()
        {

            if (_isTaskRunning)
            {
                _customLogger.WriteInfo("Replication is already running, skipping this tick...");
                return;
            }

            try
            {
                _isTaskRunning = true;

                await Task.Run(async () =>
                {
                    _customLogger.WriteInfo("Start Fetching Groups");
                    await ActiveDirectoryHelper.ProcessADObjects(_inputCreds, progressReporter, ObjectType.Group, _containers, _cancellationToken.Token);
                    _customLogger.WriteInfo("Start Fetching Users");
                    await ActiveDirectoryHelper.ProcessADObjects(_inputCreds, progressReporter, ObjectType.User, _containers, _cancellationToken.Token);
                    _customLogger.WriteInfo("Finished Replication");

                });
                _cancellationToken?.Token.ThrowIfCancellationRequested();
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException)
                {
                    //MessageBox.Show("Search has been cancelled. " + ex.Message, "Search Cancelled", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    HandleError(ex);
                }
                await ActiveDirectoryHelper.WriteOUReplication();

            }
            finally
            {
                _isTaskRunning = false;
            }
        }

        private async void HandleError(Exception ex)
        {
            try
            {
                var task = Task.Run(() =>
                {
                    CustomLogger customLogger = new CustomLogger("logs.txt", "");
                    customLogger.WriteException(ex);
                });
                await task;
            }
            catch (Exception)
            {
            }
        }

        public void Dispose()
        {
            _timer.Dispose();
        }
    }
}
