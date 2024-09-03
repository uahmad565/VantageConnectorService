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

        public ADSync(InputCreds inputCreds, List<string> containers, VantageInterval? interval, List<ObjectType> objectTypes, int recordsToSyncInSingleRequest)
        {
            _cancellationToken = new();
            progressReporter = new Progress<Status>(st =>
            {
                string message = st.LogMessage + (string.IsNullOrEmpty(st.ResultMessage) ? "" : (" Result: " + st.ResultMessage));
                GlobalLogManager.Logger.Info(message);
            });

            _inputCreds = inputCreds;
            _containers = containers;
            _vantageInterval = interval;
            _objectTypes = objectTypes;
            _recordsToSyncInSingleRequest = recordsToSyncInSingleRequest;

        }
        public void OnStart()
        {
            _timer = new System.Timers.Timer(TimeConvertHelper.ToMilliseconds(_vantageInterval.IntervalToSyncWithAD));
            _timer.Elapsed += (sender, args) => Tick();
            _timer.AutoReset = true;
            _timer.Start();
        }

        public void OnStop()
        {
            _cancellationToken?.Cancel();
            _timer?.Stop();
            _timer?.Dispose();
        }

        public async void Tick()
        {

            if (_isTaskRunning)
            {
                GlobalLogManager.Logger.Info("Replication Sync is already running, skipping this tick...");
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
                    GlobalLogManager.Logger.Info("Start Fetching Groups");
                    await ActiveDirectoryHelper.ProcessADObjects(_inputCreds, progressReporter, ObjectType.Group, _containers, _recordsToSyncInSingleRequest, _cancellationToken.Token);
                }
                if (_objectTypes.Contains(ObjectType.User))
                {
                    GlobalLogManager.Logger.Info("Start Fetching Users");
                    await ActiveDirectoryHelper.ProcessADObjects(_inputCreds, progressReporter, ObjectType.User, _containers, _recordsToSyncInSingleRequest, _cancellationToken.Token);
                }
                if (_objectTypes.Contains(ObjectType.OU))
                {
                    GlobalLogManager.Logger.Info("Start Fetching OUs");
                    await ActiveDirectoryHelper.ProcessADObjects(_inputCreds, progressReporter, ObjectType.OU, new List<string>(), _recordsToSyncInSingleRequest, _cancellationToken.Token);
                }
                GlobalLogManager.Logger.Info("Finished Sync Replication");
                //Delete Replication Starting
                if (_objectTypes.Contains(ObjectType.Group))
                {
                    GlobalLogManager.Logger.Info("Start Fetching Deleted Groups");
                    await ActiveDirectoryHelper.ProcessDeleteADObjects(_inputCreds, progressReporter, ObjectType.Group, _recordsToSyncInSingleRequest, _cancellationToken.Token);
                }
                if (_objectTypes.Contains(ObjectType.User))
                {
                    GlobalLogManager.Logger.Info("Start Fetching Deleted Users");
                    await ActiveDirectoryHelper.ProcessDeleteADObjects(_inputCreds, progressReporter, ObjectType.User, _recordsToSyncInSingleRequest, _cancellationToken.Token);
                }
                if (_objectTypes.Contains(ObjectType.OU))
                {
                    GlobalLogManager.Logger.Info("Start Fetching Deleted OUs");
                    await ActiveDirectoryHelper.ProcessDeleteADObjects(_inputCreds, progressReporter, ObjectType.OU, _recordsToSyncInSingleRequest, _cancellationToken.Token);
                }
                GlobalLogManager.Logger.Info("Finished Delete Replication");
            });
        }
        private void HandleError(Exception ex)
        {
            try
            {
                GlobalLogManager.Logger.Error(ex);
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
