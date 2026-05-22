using System.Diagnostics;

namespace RinhaNet.Api.Tools
{
    public readonly struct PerfStep(string name, long thresholdMs = 1) : IDisposable
    {
        private readonly long _start = Stopwatch.GetTimestamp();

        public void Log(string stepName)
        {
            var elapsedMs = Stopwatch.GetElapsedTime(_start).TotalMilliseconds;
            if (elapsedMs >= thresholdMs)
            {
                Console.WriteLine($"[PERF] {name} - {stepName}: {elapsedMs:F3}ms");
            }
        }

        public void Dispose()
        {
            var elapsedMs = Stopwatch.GetElapsedTime(_start).TotalMilliseconds;

            if (elapsedMs >= thresholdMs)
            {
                Console.WriteLine($"[PERF] {name}: {elapsedMs:F3}ms");
            }
        }
    }
}
