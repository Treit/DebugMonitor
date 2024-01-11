using System.Text;
using System.Text.Json;

Console.OutputEncoding = Encoding.UTF8;

Monitor(() =>
{
    return new PipeStream(".", "testpipe");
});

static void Monitor(Func<IConnectedStream> streamProvider)
{
    var last = new DebugStats(-1, -1, 0.0);
    var stats = last;
    var live = false;

    while (true)
    {
        live = false;
        ProcessMessage(live, last, stats);

        try
        {
            using var inputStream = streamProvider();
            using var sr = new StreamReader(inputStream.DataStream);

            if (inputStream.IsConnected)
            {
                live = true;

                while (sr.ReadLine() is string line)
                {
                    stats = JsonSerializer.Deserialize<DebugStats>(line);
                    ProcessMessage(live, last, stats);
                    last = stats;
                }
            }
        }
        catch (Exception e) when (e is TimeoutException or IOException)
        {
            live = false;
        }

        Thread.Sleep(250);
    }

}

static void ProcessMessage(bool live, DebugStats last, DebugStats stats)
{
    var threads = stats.RunningThreadPoolThreads;
    var exceptions = stats.ExceptionCount;

    if (!live || stats != last)
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write($"{threads}");
        Console.ResetColor();
        Console.Write(" running thread pool threads.");
        Console.Write($" {stats.ExceptionCount} total exceptions.");
        Console.Write($" {stats.ExceptionsPerSecond} exceptions per second.");
        Console.CursorVisible = false;

        if (!live)
        {
            Console.Clear();
            Console.Write("<Program not running>\n💀");
        }
        else if (threads > 16 && threads < 100)
        {
            Console.Write("\n🤨");
        }
        else if (threads >= 100 && threads < 1000)
        {
            Console.Write("\n😬");
        }
        else if (threads >= 1000)
        {
            Console.Write("\n😱");
        }
        else
        {
            Console.Write("\n😊");
        }

        Console.ResetColor();
    }
}

public record struct DebugStats(
    int RunningThreadPoolThreads,
    int ExceptionCount,
    double ExceptionsPerSecond
);
