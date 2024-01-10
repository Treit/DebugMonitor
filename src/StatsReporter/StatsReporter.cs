using System.Diagnostics;
using System.IO.Pipes;
using System.Text.Json;

namespace DebugMonitor
{

    public record struct DebugStats(
        int RunningThreadPoolThreads,
        int ExceptionCount
    );

    public static class StatsReporter
    {
        private static readonly bool s_enabled;
        private static bool s_running;
        private static DebugStats s_debugStats = new();

        static StatsReporter()
        {
            //s_enabled = Environment.GetEnvironmentVariable("DEBUG_MONITOR") is not null;
            s_enabled = true;
        }

        private static void MonitorStats(CancellationToken ct)
        {
            var sleepTime = 250;

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    s_debugStats.RunningThreadPoolThreads = GetRunningThreadPoolThreads();
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
                                var str = JsonSerializer.Serialize(s_debugStats);
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
