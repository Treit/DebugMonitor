using DebugMonitor;

var cts = new CancellationTokenSource();
StatsReporter.StartMonitoring(cts.Token);
var tasks = new List<Task>();

ThreadPool.SetMinThreads(500, 500);

while (true)
{
    tasks.Clear();

    for (int i = 0; i < 520; i++)
    {
        var t = Task.Run(async () =>
        {
            Thread.Sleep(5000);
            await Task.Delay(2000);
        });

        tasks.Add(t);
    }

    await Task.WhenAll(tasks);

}
