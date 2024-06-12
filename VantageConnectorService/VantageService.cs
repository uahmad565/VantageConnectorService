using ActiveDirectorySearcher.DTOs;
using CommonUtils;
using CommonUtils.GlobalObjects;
using System.Timers;
using VantageConnectorService.DTOs;
using VantageConnectorService.Factory;
using VantageConnectorService.GlobalObjects;

namespace VantageConnectorService
{
    internal class VantageService
    {
        private bool _isTaskRunning = false;
        private System.Timers.Timer _timer;
        private CancellationTokenSource? _cancellationToken;
        private ServiceClient _serviceClient;

        private VantageConfig _vantageConfig;
        private ADSync? _aDSync = null;

        public VantageService()
        {
            _cancellationToken = new();
            _vantageConfig = VantageConfigFactory.Create();
            _serviceClient = new ServiceClient(_vantageConfig);
            GlobalFileHandler.Initialize();

            //if ADSync Schedule specified ever then start syncing
            if (StartNewADSyncIfSyncSettingSpecified())
                GlobalLogManager.Logger.Info("ADSync Timer started on creating Vantage Service");
        }

        private bool StartNewADSyncIfSyncSettingSpecified()
        {
            var loadedSyncSetting = GlobalFileHandler.ReadJSON<Setting>(GlobalFileHandler.SyncSettingFileName);
            if (loadedSyncSetting != null)
            {
                _aDSync = ADSyncFactory.Create(loadedSyncSetting.data, _vantageConfig);
                _aDSync.OnStart();
                return true;
            }
            return false;
        }
        public void Start()
        {
            //get SettingInterval
            var settingInterval = GlobalFileHandler.ReadJSON<double>(GlobalFileHandler.SettingGettingInterval);
            if (settingInterval == default)
                settingInterval = GlobalDefault.DefaultSettingGettingInterval;
            _timer = new System.Timers.Timer(settingInterval * 60 * 1000) { AutoReset = true };
            _timer.Elapsed += TimerElapsed;
            _timer.Start();
        }
        public void Stop()
        {
            _aDSync?.OnStop();
            _cancellationToken?.Cancel();
            _timer?.Stop();
            _timer?.Dispose();
        }

        private async void TimerElapsed(object? sender, ElapsedEventArgs e)
        {
            if (_isTaskRunning)
                return;
            try
            {
                _isTaskRunning = true;
                var settings = await _serviceClient.GetSettings();
                //var settings = _serviceClient.DummySettings(); ;

                if (settings.Length == 0)
                {
                    GlobalLogManager.Logger.Info($"Vantage-Service setting not found");
                    return;
                }
                var currentSetting = settings[0];
                GlobalLogManager.Logger.Info($"Going to fetch Setting for Vantage Service{Environment.NewLine}{await SerializerHelper.GetSerializedObject(currentSetting, new() { WriteIndented = true })}");

                if ((currentSetting.name == SettingType.SyncData || currentSetting.name == SettingType.SyncAllData || currentSetting.name == SettingType.StopAgent) && _aDSync != null)
                {
                    _aDSync.OnStop();
                }

                try
                {
                    switch (currentSetting.name)
                    {
                        case SettingType.SyncSettings:
                            {
                                _aDSync?.OnStop();
                                await GlobalFileHandler.WriteJSON<Setting>(currentSetting, GlobalFileHandler.SyncSettingFileName);
                                _aDSync = ADSyncFactory.Create(currentSetting.data, _vantageConfig);
                                _aDSync.OnStart();
                                break;
                            }
                        case SettingType.SyncData:
                            {
                                var loadedSyncSetting = GlobalFileHandler.ReadJSON<Setting>(GlobalFileHandler.SyncSettingFileName);
                                if (loadedSyncSetting == null) throw new Exception($"{GlobalFileHandler.SyncSettingFileName} should have SyncSetting to proceed {currentSetting.name}");
                                using var immediateADSync = ADSyncFactory.Create(loadedSyncSetting.data, _vantageConfig, true);
                                await immediateADSync.ProcessObjects();
                                break;
                            }
                        case SettingType.SyncAllData:
                            {
                                var loadedSyncSetting = GlobalFileHandler.ReadJSON<Setting>(GlobalFileHandler.SyncSettingFileName);
                                if (loadedSyncSetting == null) throw new Exception($"{GlobalFileHandler.SyncSettingFileName} should have SyncSetting to proceed {currentSetting.name}");
                                GlobalFileHandler.EmptyAllReplicationFiles();
                                using var immediateADSync = ADSyncFactory.Create(loadedSyncSetting.data, _vantageConfig, true);
                                await immediateADSync.ProcessObjects();
                                break;
                            }
                        case SettingType.StopAgent: // Halt All tasks focus here
                            {
                                await GlobalFileHandler.WriteJSON<bool>(true, GlobalFileHandler.UtilityStatus);
                                Stop();
                                break;
                            }
                        case SettingType.UploadADConnectorDebugLogFile:
                            {
                                await UploadADConnectoryLogFile(_vantageConfig.domainId);
                                break;
                            }
                        case SettingType.SettingGettingInterval:
                            {
                                await GlobalFileHandler.WriteJSON<double>(currentSetting.data.interval, GlobalFileHandler.SettingGettingInterval);
                                _timer.Interval = currentSetting.data.interval * 60 * 1000; //convert to minute
                                break;
                            }
                    }
                    await CallbackStatus(new Commanddetail { uuid = currentSetting.uuid, error = CommandDetailType.Success, errorDescription = currentSetting.name.ToString() + " Successful Operation." });
                }
                catch (Exception ex)
                {
                    await CallbackStatus(new Commanddetail { uuid = currentSetting.uuid, error = CommandDetailType.Failed, errorDescription = currentSetting.name.ToString() + " Failed Operation." });
                    GlobalLogManager.Logger.Error(ex);
                }

                //resume if ADSync stopped for a while
                if ((currentSetting.name == SettingType.SyncData || currentSetting.name == SettingType.SyncAllData) && _aDSync != null)
                {
                    StartNewADSyncIfSyncSettingSpecified();
                }
                //await _serviceClient.DummyDequeSettings(settings);
            }
            catch (Exception vantageTickException)
            {
                GlobalLogManager.Logger.Error(vantageTickException);
            }
            finally
            {

                _isTaskRunning = false;
            }
        }


        #region private methods
        private async Task CallbackStatus(Commanddetail commandDetail)
        {
            CallbackRequestBody body = new CallbackRequestBody();
            body.commandDetails = new Commanddetail[]
            {
                    commandDetail
            };
            body.commandUuids = new string[] {
                    commandDetail.uuid
                };
            await _serviceClient.CallBackService(body);
        }
        private async Task UploadADConnectoryLogFile(string domainId)
        {
            await _serviceClient.UploadLogFile(GlobalLogManager.FilePath, domainId, _cancellationToken.Token);
        }
        #endregion
    }
}
