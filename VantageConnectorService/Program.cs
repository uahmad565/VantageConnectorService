using ActiveDirectorySearcher.DTOs;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using Topshelf;
using VantageConnectorService;
using VantageConnectorService.DTOs;
using VantageConnectorService.Factory;

try
{
    VantageConfig config = VantageConfigFactory.Create();

    ServiceClient service = new ServiceClient(config);
    Setting[] setting = service.DummySettings();
    ADSync syncObj = ADSyncFactory.Create(setting[0].data, config);
    syncObj.OnStart();
}
catch(Exception ex)
{
    Console.WriteLine(ex.Message);
}


/*
var exitCode = HostFactory.Run(x =>
{
    x.UseNLog(NLogManager.Instance);
    x.Service<VantageService>(s =>
    {
        s.ConstructUsing(heartbeat => new VantageService());
        s.WhenStarted(Heartbeat => Heartbeat.Start());
        s.WhenStopped(Heartbeat => Heartbeat.Stop());
    });

    x.RunAsLocalSystem();

    x.SetServiceName("VantageService");
    x.SetDisplayName("Vantage Connector");
    x.SetDescription("Sync Active Directory Objects on regular intervals.");

});

int exitCodeValue = (int)Convert.ChangeType(exitCode, exitCode.GetTypeCode());
Environment.ExitCode = exitCodeValue;
*/