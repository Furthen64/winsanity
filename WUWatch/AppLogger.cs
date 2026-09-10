namespace WUWatch;

public sealed class AppLogger
{
    private static readonly object Lock = new();
    private readonly string _logPath;

    public AppLogger()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string dir = Path.Combine(appData, "WUWatch");
        Directory.CreateDirectory(dir);
        _logPath = Path.Combine(dir, "wuwatch.log");
    }

    public void Info(string message) => Write("INFO", message);
    public void Warn(string message) => Write("WARN", message);
    public void Error(string message, Exception? ex = null)
    {
        string msg = ex != null ? $"{message}: {ex.Message}" : message;
        Write("ERROR", msg);
    }

    private void Write(string level, string message)
    {
        string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";

        lock (Lock)
        {
            try
            {
                File.AppendAllText(_logPath, line + Environment.NewLine);
            }
            catch
            {
                // Logging should never crash the application
            }
        }
    }
}
