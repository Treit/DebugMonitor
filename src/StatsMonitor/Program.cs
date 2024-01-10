
using System.IO.Pipes;
using System.Text;
using System.Text.Json;

Console.OutputEncoding = Encoding.UTF8;

Process(() =>
{
    return new PipeStream(".", "testpipe");
});

static void Process(Func<IConnectedStream> streamProvider)
{
    var last = new DebugStats(-1, -1);
    var stats = last;

    while (true)
    {
        var live = false;
        ProcessMessage(live, last, stats);

        try
        {
            using (var inputStream = streamProvider())
            {
                using (StreamReader sr = new StreamReader(inputStream.DataStream))
                {
                    while (inputStream.IsConnected)
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
            }
        }
        catch (TimeoutException)
        {
            live = false;
        }

        Thread.Sleep(250);
    }

}

static void ProcessMessage(bool live, DebugStats last, DebugStats stats)
{
    var threads = stats.RunningThreadPoolThreads;

    if (!live || stats != last)
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write($"{threads}");
        Console.ResetColor();
        Console.Write(" running thread pool threads.");
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
    int ExceptionCount
);

interface IConnectedStream : IDisposable
{
    public Stream DataStream { get; }
    public bool IsConnected { get; }
}

sealed class PipeStream : IConnectedStream, IDisposable
{
    bool _disposed;
    readonly NamedPipeClientStream _clientStream;

    public PipeStream(string host, string pipeName)
    {
        var npcs = new NamedPipeClientStream(host, pipeName, PipeDirection.In);
        _clientStream = npcs;
        _clientStream.Connect();
    }

    public Stream DataStream
    {
        get
        {
            ThrowIfDisposed();
            return _clientStream;
        }
    }

    public bool IsConnected => _clientStream.IsConnected;

    public void Dispose()
    {
        _clientStream.Dispose();
        _disposed = true;
    }

    void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(PipeStream));
        }
    }
}