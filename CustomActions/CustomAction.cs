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
        public static ActionResult SaveUserInput(Session session)
        {
            string configPath = session["LUCENEFOLDER"];
            string appdataPath = "C:\\Users\\Caphyon";
            if (!System.IO.Directory.Exists(appdataPath + "\\UserData"))
                System.IO.Directory.CreateDirectory(appdataPath + "\\UserData");
            System.IO.File.WriteAllText(appdataPath + "\\UserData\\InputInfo.txt", "FilePath: " + configPath);
            return ActionResult.Success;
        }

        [CustomAction]
        public static ActionResult OpenFileChooser(Session session)
        {
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
                string appdataPath = "C:\\Users\\Caphyon";
                if (!System.IO.Directory.Exists(appdataPath + "\\UserData"))
                    System.IO.Directory.CreateDirectory(appdataPath + "\\UserData");
                System.IO.File.WriteAllText(appdataPath + "\\UserData\\InputInfo.txt", "FilePath: " + fileDialog.FileName);

                session["FILEPATH"] = fileDialog.FileName;
            }
        }
    }
}
