using System.Text;

namespace VantageConnectorService.Helpers
{
    public class CustomLogger
    {
        private static object fileLock=new object();
        private readonly string filePath;

        public string FilePath => filePath;

        public CustomLogger(string fileName)
        {
            var basePath = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory);

            filePath = Path.Combine(basePath ?? "", "Logs", fileName);
            Initialize();
        }

        public void WriteInfo(string message) => Write(message, MessageType.Info);

        public void WriteWarn(string message) => Write(message, MessageType.Warn);

        public void WriteError(string message) => Write(message, MessageType.Error);

        public void WriteJson(object obj, string title)
        {
            try
            {
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(obj, Newtonsoft.Json.Formatting.Indented);
                Write($"[{title}]{Environment.NewLine}{json}", MessageType.Info);
            }
            catch
            {
            }
        }

        public void WriteException(Exception ex)
        {
            var sb = new StringBuilder();

            while (ex != null)
            {
                sb.AppendLine(ex.Message);
                sb.AppendLine(ex.StackTrace);
                sb.AppendLine("----------------------------------------------------------------------");
                ex = ex.InnerException;
            }

            Write(sb.ToString(), MessageType.Error);
        }

        private void Initialize()
        {
            var directory = Path.GetDirectoryName(this.filePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        private void Write(string message, MessageType messageType)
        {
            lock(fileLock)
            {
                var formattedMessage = $"[{DateTime.Now:yyyy-MM-dd:HH:mm:ss.fff}] [{messageType.ToString().ToUpper()}]: {message}{Environment.NewLine}";
                File.AppendAllText(this.filePath, formattedMessage);
            }
        }

        private enum MessageType
        {
            Info, Warn, Error
        }
    }
}
