﻿using ActiveDirectorySearcher.DTOs;
using CommonUtils;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using VantageConnectorService.DTOs;

namespace VantageConnectorService
{
    internal class ServiceClient
    {
        private VantageConfig vantageConfig;

        public ServiceClient(VantageConfig vantageConfig)
        {
            this.vantageConfig = vantageConfig;
        }
        #region Dummy Region
        public Setting[] DummySettings()
        {

            // Specify the path to the JSON file
            string filePath = "C:\\TempUsman\\CloneConnectorService\\VantageConnectorService\\VantageConnectorService\\settings.json";

            // Read the JSON file into a string
            string jsonString = File.ReadAllText(filePath);
            var settings = JsonConvert.DeserializeObject<Setting[]>(jsonString);
            return settings;
        }

        public async Task<bool> DummyDequeSettings(Setting[] settings)
        {
            var list = settings.Skip(1).ToList();
            string filePath = "C:\\TempUsman\\CloneConnectorService\\VantageConnectorService\\VantageConnectorService\\settings.json";

            JsonSerializerOptions options = new JsonSerializerOptions
            {
                Converters = { new JsonStringEnumConverter() },
                WriteIndented = true
            };

            var json = await SerializerHelper.GetSerializedObject(list, options);

            File.WriteAllText(filePath, json);
            return true;
        }

        #endregion
        public async Task<Setting[]> GetSettings()
        {

            var url = $"{vantageConfig.host}/active-directory/settings?domainId={vantageConfig.domainId}";
            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var settingResponse = JsonConvert.DeserializeObject<SettingResponse>(content);
                return settingResponse?.settings ?? throw new Exception("setting response can't be null");
            }
            else
            {
                throw new Exception("Error in GetSetting Request");
            }
        }

        public async Task CallBackService(CallbackRequestBody callbackRequest)
        {
            var apiUrl = $"{vantageConfig.host}/active-directory/callback?domainId={vantageConfig.domainId}";

            JsonSerializerOptions options = new JsonSerializerOptions
            {
                Converters = { new JsonStringEnumConverter() },
                WriteIndented = true
            };

            var json = await SerializerHelper.GetSerializedObject(callbackRequest, options);
            using var client = new HttpClient();
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PutAsync(apiUrl, content);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Request failed with status code {response.StatusCode} and ResponseBody {content}");
            }
        }

        public async Task UploadLogFile(string filePath, string domainId, CancellationToken cancellationToken)
        {
            string url = $"{vantageConfig.host}/secure/upload/file";

            using (var client = new HttpClient())
            {
                using (var content = new MultipartFormDataContent())
                {
                    byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
                    var fileContent = new ByteArrayContent(fileBytes);
                    fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");
                    content.Add(fileContent, "file", $"{domainId}_ADControllerDebugLog_{DateTime.Now.ToString("yyyyMMddHHmmss")}.log");

                    var response = await client.PostAsync(url, content, cancellationToken);

                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception($"Failed to upload log file. Status code: {response.StatusCode}");
                    }
                }
            }
        }
    }
}
