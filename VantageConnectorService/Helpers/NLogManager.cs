using NLog.Config;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NLog.Targets;
using VantageConnectorService.GlobalObjects;

namespace VantageConnectorService.Helpers
{
    internal class NLogManager
    {
        // A Logger dispenser for the current assembly (Remember to call Flush on application exit)
        public static LogFactory Instance { get { return _instance.Value; } }
        private static Lazy<LogFactory> _instance = new Lazy<LogFactory>(BuildLogFactory);

        // 
        // Use a config file located next to our current assembly dll 
        // eg, if the running assembly is c:\path\to\MyComponent.dll 
        // the config filepath will be c:\path\to\MyComponent.nlog 
        // 
        // WARNING: This will not be appropriate for assemblies in the GAC 
        // 
        [Obsolete]
        private static LogFactory BuildLogFactory()
        {
            // Use name of current assembly to construct NLog config filename 

            var basePath = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory);
            var configFilePath = Path.Combine(basePath ?? "", GlobalDefault.NLogConfigFileName);
            if (string.IsNullOrEmpty(configFilePath)) throw new Exception("NLog Config file path not found!");

            LogFactory logFactory = new LogFactory();
            logFactory.Configuration = new XmlLoggingConfiguration(configFilePath, true, logFactory);
            return logFactory;
        }
    }
}
