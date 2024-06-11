using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonUtils.GlobalObjects
{
    public class GlobalFileHandler
    {
        #region Public Properties
        public static string GroupReplicationFileName
        {
            get => "GroupReplicationTime.txt";
        }

        public static string UserReplicationFileName
        {
            get => "UserReplicationTime.txt";
        }
        public static string OUReplicationFileName
        {
            get => "OUReplicationTime.txt";
        }

        public static string OU_UserGroupsReplicationFileName
        {
            get => "OU_UserGroupsReplicationTime.txt";
        }

        public static string SyncSettingFileName
        {
            get => "SyncSetting.json";
        }

        public static string SettingGettingInterval
        {
            get => "SettingGettingInterval.json";
        }
        #endregion

        #region Private 
        public static string InfoDirectory
        {
            get
            {
                var basePath = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory);
                var infoFolderPath = Path.Combine(basePath ?? "", "Info");
                if (!Directory.Exists(infoFolderPath))
                {
                    Directory.CreateDirectory(infoFolderPath);
                }
                return infoFolderPath;
            }
        }

        private static List<string> AllFilePaths
        {
            get
            {
                string directory = InfoDirectory;
                var tempList = AllReplicationFilePaths;
                tempList.Add(Path.Combine(directory, GlobalFileHandler.SyncSettingFileName));
                tempList.Add(Path.Combine(directory, GlobalFileHandler.SettingGettingInterval));
                return tempList;
            }
        }

        private static List<string> AllReplicationFilePaths
        {
            get
            {
                string directory = InfoDirectory;
                return new List<string>(){
                    Path.Combine(directory, GlobalFileHandler.GroupReplicationFileName),
                    Path.Combine(directory, GlobalFileHandler.UserReplicationFileName),
                    Path.Combine(directory, GlobalFileHandler.OUReplicationFileName),
                    Path.Combine(directory, GlobalFileHandler.OU_UserGroupsReplicationFileName),
                };
            }
        }

        public static async Task WriteJSON<T>(T obj, string fileName)
        {
            string directory = InfoDirectory;
            var json = await SerializerHelper.GetSerializedObject(obj);
            await File.WriteAllTextAsync(Path.Combine(directory, fileName), json);
        }

        public static T ReadJSON<T>(string fileName)
        {
            string directory = InfoDirectory;

            string fileJson = File.ReadAllText(Path.Combine(directory, fileName));
            if (!string.IsNullOrEmpty(fileJson))
            {
                T obj = JsonConvert.DeserializeObject<T>(fileJson);
                return obj;
            }
            return default(T);
        }

        #endregion

        #region Public Methods

        public static void Initialize()
        {
            foreach (string file in AllFilePaths)
            {
                if (!File.Exists(file))
                {
                    File.Create(file).Dispose();
                }
            }
        }

        public static void EmptyAllReplicationFiles()
        {
            foreach (string file in AllReplicationFilePaths)
            {
                File.WriteAllText(file, string.Empty);
            }
        }
        #endregion

    }
}
