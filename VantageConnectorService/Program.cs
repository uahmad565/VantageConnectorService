using CommonUtils.GlobalObjects;
using Topshelf;
using VantageConnectorService;
using VantageConnectorService.GlobalObjects;
using VantageConnectorService.Helpers;


var exitCode = HostFactory.Run(x =>
{
    x.UseNLog(NLogManager.Instance);
    x.Service<VantageService>(s =>
    {
        s.ConstructUsing(heartbeat => new VantageService());
        s.WhenStarted((VantageService Heartbeat,HostControl hostControl )=> {

            var isStopped = GlobalFileHandler.ReadJSON<bool>(GlobalFileHandler.UtilityStatus);
            if (isStopped == true)
            {
                GlobalLogManager.Logger.Debug("Failed to start as Stop Agent Already called once");
                hostControl.Stop();
                return true;
            }
            Heartbeat.Start();
            return true;
        });
        s.WhenStopped(Heartbeat => Heartbeat.Stop());
    });

    x.RunAsLocalSystem();

    x.SetServiceName("VantageService");
    x.SetDisplayName("Vantage Connector");
    x.SetDescription("Sync Active Directory Objects on regular intervals.");

});

int exitCodeValue = (int)Convert.ChangeType(exitCode, exitCode.GetTypeCode());
Environment.ExitCode = exitCodeValue;