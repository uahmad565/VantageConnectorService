using ActiveDirectorySearcher.DTOs;
using CommonUtils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
        //dummy
        public Setting[] DummySettings()
        {

            // Specify the path to the JSON file
            string filePath = "C:\\TempUsman\\CloneConnectorService\\VantageConnectorService\\VantageConnectorService\\settings.json";

            // Read the JSON file into a string
            string jsonString = File.ReadAllText(filePath);
            var settings = JsonConvert.DeserializeObject<Setting[]>(jsonString);
            return settings;

        }
        public async Task<Setting[]> GetSetting()
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

        public async Task CallBackService(string domainId, CallbackRequestBody callbackRequest, CancellationToken cancellationToken)
        {
            var apiUrl = $"{vantageConfig.host}/settings?domainId={vantageConfig.domainId}";
            var json = await SerializerHelper.GetSerializedObject(callbackRequest);
            using var client = new HttpClient();
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PutAsync(apiUrl, content, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Request failed with status code {response.StatusCode} and ResponseBody {content}");
            }
        }
    }
}
