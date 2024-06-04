using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public CommandDetailType error { get; set; }
        public string errorDescription { get; set; }
    }

}


