using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonUtils.GlobalObjects
{
    public class DeleteReplicationGlobalFileHandler
    {
        static DeleteReplicationGlobalFileHandler()
        {
            Initialize();
        }
        #region Public Properties
        public static string DeleteGroupReplicationFileName
        {
            get => "DeleteGroupReplicationTime.txt";
        }

        public static string DeleteUserReplicationFileName
        {
            get => "DeleteUserReplicationTime.txt";
        }
        public static string DeleteOUReplicationFileName
        {
            get => "DeleteOUReplicationTime.txt";
        }
        #endregion

        #region Private 
        public static string DeleteInfoDirectory
        {
            get
            {
                var basePath = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory);
                var infoFolderPath = Path.Combine(basePath ?? "", "DeleteInfo");
                if (!Directory.Exists(infoFolderPath))
                {
                    Directory.CreateDirectory(infoFolderPath);
                }
                return infoFolderPath;
            }
        }

        private static List<string> AllReplicationFilePaths
        {
            get
            {
                string directory = DeleteInfoDirectory;
                return new List<string>(){
                    Path.Combine(directory, DeleteGroupReplicationFileName),
                    Path.Combine(directory, DeleteUserReplicationFileName),
                    Path.Combine(directory, DeleteOUReplicationFileName),
                };
            }
        }


        #endregion

        #region Public Methods

        public static void Initialize()
        {
            foreach (string file in AllReplicationFilePaths)
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
