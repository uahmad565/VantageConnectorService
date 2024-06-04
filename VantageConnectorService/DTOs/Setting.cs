using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VantageConnectorService.DTOs
{
    public class SettingResponse
    {
        public Setting[] settings { get; set; }
        public int code { get; set; }
        public string codeDescription { get; set; }
    }

    public class Setting
    {
        public SettingData data { get; set; }
        public SettingType name { get; set; }
        public string uuid { get; set; }
    }

    public class SettingData
    {
        public string domainName { get; set; }
        public string domainController { get; set; }
        public int port { get; set; }
        public bool configFromServer { get; set; }
        public string domainUserName { get; set; }
        public string password { get; set; }
        public string[] dataType { get; set; }
        public string selectedOUs { get; set; }
        public int settingGettingInterval { get; set; }
        public string syncFrequency { get; set; }
        public int syncHours { get; set; }
        public int syncMinutes { get; set; }
        public Syncdays syncDays { get; set; }
    }

    public class Syncdays
    {
        public bool monday { get; set; }
        public bool tuesday { get; set; }
        public bool wednesday { get; set; }
        public bool thursday { get; set; }
        public bool friday { get; set; }
        public bool saturday { get; set; }
        public bool sunday { get; set; }
    }
}

