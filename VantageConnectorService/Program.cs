using Topshelf;
using VantageConnectorService;

var exitCode = HostFactory.Run(x =>
{
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