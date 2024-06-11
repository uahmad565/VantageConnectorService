using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VantageConnectorService.GlobalObjects
{
    public class GlobalDefault
    {
        public static double DefaultSettingGettingInterval { get => 5; } //minutes
        public static string LogsFolderName { get => "Logs"; }
        public static string LogFileName { get => "vantage-connector.log"; }
        public static string NLogConfigFileName { get => "VantageConnectorService.nlog"; }


    }
}
