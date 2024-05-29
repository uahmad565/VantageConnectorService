using Microsoft.Deployment.WindowsInstaller;
using System;
using System.Collections.Generic;
using System.Text;

namespace CustomActions
{
    public class CustomActions
    {
        [CustomAction]
        public static ActionResult SaveUserInput(Session session)
        {
            string email = session["EMAIL"];
            string password = session["PASSWORD"];
            string appdataPath = "C:\\Users\\Caphyon";
            if (!System.IO.Directory.Exists(appdataPath + "\\UserData"))
                System.IO.Directory.CreateDirectory(appdataPath + "\\UserData");
            System.IO.File.WriteAllText(appdataPath + "\\UserData\\InputInfo.txt", email + "," + password);
            return ActionResult.Success;
        }
    }
}
