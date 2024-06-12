using Microsoft.Deployment.WindowsInstaller;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Windows;
using WinForms = System.Windows.Forms;
using System.IO;

namespace CustomActions
{
    public class CustomActions
    {

        [CustomAction]
        public static ActionResult CopyConfigFileToInstallDir(Session session)
        {
            try
            {
                string programDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                string targetFolder = Path.Combine(programDataFolder, "Vantage Connector");

                if(Directory.Exists(targetFolder))
                {
                    Directory.Delete(targetFolder, true);
                }    
                Directory.CreateDirectory(targetFolder);
                var sourcePath = session["FILEPATH"];
                FileInfo sourceFile = new FileInfo(sourcePath);
                string destinationFilePath = Path.Combine(targetFolder, sourceFile.Name);
                sourceFile.CopyTo(destinationFilePath, true); // Set the second argument to true to overwrite if needed
                session.Log("Copied Vantage Config File");
            }
            catch (Exception ex)
            {
                session.Log("Exception occurred as Message: {0}\r\n StackTrace: {1}", ex.Message, ex.StackTrace);
                return ActionResult.Failure;
            }
            return ActionResult.Success;
        }

        [CustomAction]
        public static ActionResult OpenFileChooser(Session session)
        {
            session.Log("Here is on OpenFileChooser");

            try
            {
                session.Log("Begin OpenFileChooser Custom Action");
                var task = new Thread(() => GetFile(session));
                task.SetApartmentState(ApartmentState.STA);
                task.Start();
                task.Join();
                session.Log("End OpenFileChooser Custom Action");
            }
            catch (Exception ex)
            {
                session.Log("Exception occurred as Message: {0}\r\n StackTrace: {1}", ex.Message, ex.StackTrace);
                return ActionResult.Failure;
            }
            return ActionResult.Success;
        }

        private static void GetFile(Session session)
        {
            var fileDialog = new WinForms.OpenFileDialog { Filter = "JSON Files (*.json)|*.json" };
            if (fileDialog.ShowDialog() == WinForms.DialogResult.OK)
            {

                session["FILEPATH"] = fileDialog.FileName;
            }
        }
    }
}
