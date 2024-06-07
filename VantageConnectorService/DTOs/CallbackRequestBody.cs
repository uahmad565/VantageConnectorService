
using System.Text.Json.Serialization;

namespace VantageConnectorService.DTOs
{
    public enum CommandDetailType
    {
        Success,
        Failed,
        InProgress
    }
    public class CallbackRequestBody
    {
        public string[] commandUuids { get; set; }
        public Commanddetail[] commandDetails { get; set; }
    }

    public class Commanddetail
    {
        public string uuid { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public CommandDetailType error { get; set; }
        
        public string errorDescription { get; set; }
    }

}


