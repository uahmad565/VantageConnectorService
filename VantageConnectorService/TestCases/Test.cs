//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using VantageConnectorService.DTOs;
//using VantageConnectorService.Factory;
//using VantageConnectorService.GlobalObjects;
//using Newtonsoft.Json;
//using System.Net.Http.Headers;
//using ActiveDirectorySearcher.DTOs;

//namespace VantageConnectorService.TestCases
//{
//    class Program
//    {
//        static void Main(string[] args)
//        {
//            try
//            {
//                MainAsync(args).GetAwaiter().GetResult();

//                //syncObj.OnStart();
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine(ex.Message);
//            }

//        }

//        static async Task MainAsync(string[] args)
//        {
//            GlobalFileHandler.Initialize();

//            VantageConfig config = VantageConfigFactory.Create();
//            GlobalLogManager.Initialize($"{config.domainId}_ADControllerDebugLog_{DateTime.Now.ToString("yyyyMMddHHmmss")}");
//            config.host = @"https://ns-server.vantagemdm.com";
//            ServiceClient service = new ServiceClient(config);
//            Setting[] setting = service.DummySettings();
//            ADSync syncObj = ADSyncFactory.Create(setting[0].data, config, false);
//            syncObj.Tick();
//            //service.DummyDequeSettings(setting);
//            await Task.Delay(1000 * 60 * 60);

//        }


//    }
//}
