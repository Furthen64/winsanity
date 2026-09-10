using System.Diagnostics.Eventing.Reader;

namespace WUWatch;

public sealed class WindowsUpdateWatcher : IDisposable
{
    private EventLogWatcher? _watcher;
    private readonly HashSet<int> _relevantEventIds;
    private readonly AppLogger _logger;

    public event Action<int, string?>? RelevantEventReceived;

    public WindowsUpdateWatcher(AppSettings settings, AppLogger logger)
    {
        _relevantEventIds = [.. settings.RelevantWindowsUpdateEventIds];
        _logger = logger;
    }

    public void Start()
    {
        try
        {
            string filter = string.Join(" or ", _relevantEventIds.Select(id => $"EventID={id}"));
            string query = $"*[System[({filter})]]";

            var eventQuery = new EventLogQuery(
                "Microsoft-Windows-WindowsUpdateClient/Operational",
                PathType.LogName,
                query);

            _watcher = new EventLogWatcher(eventQuery);
            _watcher.EventRecordWritten += OnEventRecordWritten;
            _watcher.Enabled = true;
            _logger.Info("Windows Update event watcher started");
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to start Windows Update event watcher", ex);
        }
    }

    public void Stop()
    {
        if (_watcher != null)
        {
            _watcher.Enabled = false;
            _watcher.EventRecordWritten -= OnEventRecordWritten;
            _watcher.Dispose();
            _watcher = null;
            _logger.Info("Windows Update event watcher stopped");
        }
    }

    private void OnEventRecordWritten(object? sender, EventRecordWrittenEventArgs e)
    {
        if (e.EventRecord == null) return;

        int eventId = e.EventRecord.Id;

        if (_relevantEventIds.Contains(eventId))
        {
            string? message = e.EventRecord.FormatDescription();
            _logger.Info($"Relevant Windows Update event: {eventId} - {message}");
            RelevantEventReceived?.Invoke(eventId, message);
        }
    }

    public void Dispose()
    {
        Stop();
    }
}
