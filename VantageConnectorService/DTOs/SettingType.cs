using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VantageConnectorService.DTOs
{
    public enum SettingType
    {
        SyncSettings,
        SyncData,
        SyncAllData,
        StopAgent,
        UploadADConnectorDebugLogFile,
        SettingGettingInterval
    }
}
