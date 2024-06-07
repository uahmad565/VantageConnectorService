using ActiveDirectorySearcher.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VantageConnectorService.Helpers;

namespace VantageConnectorService.GlobalObjects
{
    public class GlobalLogManager
    {
        public static string FilePath
        {
            get
            {
                if (_instance == null) throw new Exception(ExceptionMessage());
                return _instance.FilePath;
            }
        }

        private static CustomLogger? _instance = null;
        public static CustomLogger Logger
        {
            get
            {
                return _instance ?? throw new Exception(ExceptionMessage());
            }
        }
        public static void Initialize(string fileName)
        {
            if (_instance != null) //make sure initialize only once
                return;
            _instance = new CustomLogger(fileName);
        }

        private static string ExceptionMessage()
        {
            return "Global Logger Manager is not initialized";
        }

    }
}
