using System.Diagnostics;
using System.IO.Pipes;
using System.Text.Json;

namespace DebugMonitor
{

    public record struct DebugStats(
        int RunningThreadPoolThreads,
        int ExceptionCount,
        double ExceptionsPerSecond
    );

    public static class StatsReporter
    {
        private static readonly bool s_enabled;
        private static bool s_running;
        private static DebugStats s_stats = new();
        private static int s_threads;
        private static int s_exceptions;
        private static double s_exceptionsPerSecond;
        private static int s_exceptionsLast;
        private static Timer? s_timer;

        static StatsReporter()
        {
            //s_enabled = Environment.GetEnvironmentVariable("DEBUG_MONITOR") is not null;
            s_enabled = true;

            if (s_enabled)
            {
                var period = 1000;
                AppDomain.CurrentDomain.FirstChanceException += (source, ea) =>
                {
                    Interlocked.Increment(ref s_exceptions);
                };

                s_timer = new Timer(
                    callback: state =>
                    {
                        s_exceptionsPerSecond = ((double)s_exceptions - s_exceptionsLast) / (period / 1000);
                        s_exceptionsLast = s_exceptions;
                    },
                    state: null,
                    dueTime: 0,
                    period: period);
            }
        }

        private static void MonitorStats(CancellationToken ct)
        {
            var sleepTime = 250;

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    s_threads = GetRunningThreadPoolThreads();
                    s_stats = new DebugStats(s_threads, s_exceptions, s_exceptionsPerSecond);
                    Thread.Sleep(sleepTime);
                }
                catch (Exception e)
                {
                    Trace.WriteLine($"FAILED: {e}");
                }
            }
        }

        private static void PipeServer(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                using var pipeServer = new NamedPipeServerStream("testpipe", PipeDirection.Out);
                Console.WriteLine("NamedPipeServerStream object created.");

                // Wait for a client to connect
                Console.Write("Waiting for client connection...");
                pipeServer.WaitForConnection();

                Console.WriteLine("Client connected.");
                try
                {
                    using (StreamWriter sw = new StreamWriter(pipeServer))
                    {
                        sw.AutoFlush = true;

                        while (pipeServer.IsConnected)
                        {
                            try
                            {
                                var str = JsonSerializer.Serialize(s_stats);
                                sw.WriteLine(str);
                                Thread.Sleep(500);
                            }
                            catch (IOException e)
                            {
                                Console.WriteLine($"ERROR: {e.Message}");
                            }
                        }
                    }
                }
                catch (IOException)
                {
                    Console.WriteLine($"Dispose threw IOException");
                }
            }
        }

        public static void StartMonitoring(CancellationToken cancelToken)
        {
            if (!s_enabled || s_running)
            {
                return;
            }

            s_running = true;

            Task.Factory.StartNew(
                () =>
                MonitorStats(cancelToken),
                cancelToken,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default).Ignore();

            Task.Factory.StartNew(
                () =>
                PipeServer(cancelToken),
                cancelToken,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default).Ignore();
        }

        static int GetRunningThreadPoolThreads()
        {
            ThreadPool.GetMaxThreads(out var max, out _);
            ThreadPool.GetAvailableThreads(out var available, out _);
            var running = max - available;
            return running;
        }
    }
}
