using NLog;

using VantageConnectorService.Helpers;

namespace VantageConnectorService.GlobalObjects
{
    public class GlobalLogManager
    {
        public static Logger Logger { get; }

        static GlobalLogManager()
        {
            Logger = NLogManager.Instance.GetLogger("VD");
        }
        public static string FilePath
        {
            get
            {
                var basePath = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory);
                var filePath = Path.Combine(basePath ?? "", GlobalDefault.LogsFolderName, GlobalDefault.LogFileName);
                return filePath;
            }
        }
    }
}
