using ActiveDirectorySearcher.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using VantageConnectorService.DTOs;
using VantageConnectorService.Factory;

namespace VantageConnectorService
{
    internal class VantageService
    {
        private readonly System.Timers.Timer _timer;
        private VantageConfig _vantageConfig;
        Dictionary<SettingType, bool> currentTasks = new Dictionary<SettingType, bool>()
        {
            {SettingType.SyncSettings,false },
            {SettingType.SyncData,false },
            {SettingType.SyncAllData,false },
            {SettingType.StopAgent,false },
            {SettingType.UploadADConnectorDebugLogFile,false }
        };

        public VantageService()
        {
            _timer = new System.Timers.Timer(60000) { AutoReset = true };
            _timer.Elapsed += TimerElapsed;
        }

        private async void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (currentTasks[SettingType.SyncData] || currentTasks[SettingType.SyncAllData] || currentTasks[SettingType.StopAgent])
                return;
            ServiceClient serviceClient = new(_vantageConfig);
            var settings = serviceClient.DummySettings();
            if (settings.Length == 0)
                return;
            var currentSetting = settings[0];


            switch (currentSetting.name)
            {
                case SettingType.SyncSettings:
                    currentTasks[SettingType.SyncData] = true;
                    //callback
                    break;
                case SettingType.SyncData:
                    currentTasks[SettingType.SyncData] = true;
                    if (currentTasks[SettingType.SyncSettings])
                    {
                        //stop Sync Setting
                    }
                    //one Time Sync
                    //restart Sync Setting if currentTasks[SettingType.SyncSettings]
                    currentTasks[SettingType.SyncData] = false;
                    //callback
                    break;
                case SettingType.SyncAllData:
                    currentTasks[SettingType.SyncAllData] = true;
                    if (currentTasks[SettingType.SyncSettings])
                    {
                        //stop Sync Setting
                    }
                    //one Time Sync
                    //restart Sync Setting if currentTasks[SettingType.SyncSettings]
                    //callback
                    break;
                case SettingType.StopAgent: // Halt All tasks focus here
                    //Halt Everything -> SyncSetting, SyncData, SyncAllData
                    //callback
                    break;
                case SettingType.UploadADConnectorDebugLogFile:
                    //Create new Task 
                    await Task.Run(() => UploadADConnectoryLogFile());
                    //callback
                    break;
            }

            bool AnyTaskRunning(SettingType settingTypeList)
            {
                return false;
            }
            string[] lines = new string[] { DateTime.Now.ToString() };
            System.IO.File.AppendAllLines(@"C:\TempUsman\abc.txt", lines);
        }
        public void Start()
        {
            _vantageConfig = VantageConfigFactory.Create();
            _timer.Start();
        }
        public void Stop()
        {
            _timer.Stop();
        }

        #region private methods
        private async Task UploadADConnectoryLogFile()
        {
            string filePath = @"C:\TempUsman\Info\92b02f3d-4da2-43ab-9efe-2187c24bff01_ADControllerDebugLog_20240509003935.log";  // Specify the path to your file
            string url = "https://ns-server.vantagemdm.com/secure/upload/file";  // Your endpoint URL

            using (var client = new HttpClient())
            {
                using (var content = new MultipartFormDataContent())
                {
                    byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
                    var fileContent = new ByteArrayContent(fileBytes);
                    fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");
                    content.Add(fileContent, "file", Path.GetFileName(filePath));

                    var response = await client.PostAsync(url, content);

                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine("File uploaded successfully.");
                    }
                    else
                    {
                        Console.WriteLine($"Failed to upload file. Status code: {response.StatusCode}");
                    }
                }
            }
        }
        #endregion
    }
}
