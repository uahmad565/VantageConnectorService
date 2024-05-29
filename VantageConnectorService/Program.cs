using Topshelf;
using VantageConnectorService;

var exitCode = HostFactory.Run(x =>
{
    x.Service<Heartbeat>(s =>
    {
        s.ConstructUsing(heartbeat => new Heartbeat());
        s.WhenStarted(Heartbeat => Heartbeat.Start());
        s.WhenStopped(Heartbeat => Heartbeat.Stop());
    });

    x.RunAsLocalSystem();

    x.SetServiceName("Heartbeat");
    x.SetDisplayName("Heartbeat a");
    x.SetDescription("A joke service that periodically logs nerdy humor.");

});

int exitCodeValue = (int)Convert.ChangeType(exitCode, exitCode.GetTypeCode());
Environment.ExitCode = exitCodeValue;