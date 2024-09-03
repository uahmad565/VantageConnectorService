using ActiveDirectorySearcher.DTOs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VantageConnectorService.Factory
{
    public class VantageConfigFactory
    {
        /// <summary>
        /// Creates an instance of the <see cref="VantageConfig"/> class by reading a JSON file from the Program Data folder.
        /// </summary>
        /// <returns>An instance of the <see cref="VantageConfig"/> class.</returns>
        /// <exception cref="Exception">Thrown when no JSON file is found or if the deserialized object is null.</exception>
        public static VantageConfig Create()
        {
            string programDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            string targetFolder = Path.Combine(programDataFolder, "Vantage Connector");
            string[] jsonFiles = Directory.GetFiles(targetFolder, "*.json");
            if (jsonFiles.Length == 0)
                throw new Exception("VantageConfig file not found from Program Data");

            string jsonString = File.ReadAllText(jsonFiles[0]);
            var vantageConfig = JsonConvert.DeserializeObject<VantageConfig>(jsonString) ?? throw new Exception("Seriazlied VantageCofig object cannot be null");
            return vantageConfig;
        }
    }
}
