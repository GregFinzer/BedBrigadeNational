namespace BedBrigade.Common.Logic;

public static class PerfLog
{
    public static DateTime? _lastLogTime = null;

    private static DateTime LastLogTime
    {
        get
        {
            if (_lastLogTime == null)
            {
                _lastLogTime = DateTime.UtcNow;
            }

            return _lastLogTime.Value;
        }
    }

    public static void Log(string message)
    {
        var now = DateTime.UtcNow;
        var elapsed = now - LastLogTime;
        _lastLogTime = now;
        Serilog.Log.Debug($"PERF: {message} (Elapsed: {elapsed.TotalMilliseconds} milliseconds)");
    }
}