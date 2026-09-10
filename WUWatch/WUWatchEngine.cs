namespace WUWatch;

public enum AppState
{
    Idle,
    Armed,
    Warning
}

public record EpisodeEntry(DateTime Timestamp, string Description, string? Detail = null);

public sealed class WUWatchEngine
{
    private readonly AppSettings _settings;
    private readonly Func<DateTime> _clock;
    private readonly RollingAverage _cpuAverage;
    private readonly RollingAverage _diskAverage;

    public AppState State { get; private set; } = AppState.Idle;
    public DateTime LastUpdateEventTime { get; private set; } = DateTime.MinValue;
    public DateTime? QuietStartTime { get; private set; }
    public DateTime? FirstEventTime { get; private set; }
    public DateTime? WarningStartTime { get; private set; }

    public double CurrentCpuPercent { get; private set; }
    public double CurrentDiskActiveTimePercent { get; private set; }
    public double CpuRollingAverage => _cpuAverage.CurrentAverage;
    public double DiskRollingAverage => _diskAverage.CurrentAverage;

    public IReadOnlyList<EpisodeEntry> Entries { get; } = new List<EpisodeEntry>();

    public event Action<AppState, AppState>? StateChanged;

    public WUWatchEngine(AppSettings settings, Func<DateTime>? clock = null)
    {
        _settings = settings;
        _clock = clock ?? (() => DateTime.Now);
        _cpuAverage = new RollingAverage(settings.WarningAverageSeconds);
        _diskAverage = new RollingAverage(settings.WarningAverageSeconds);
    }

    public void RecordUpdateEvent(int eventId, string? message)
    {
        DateTime now = _clock();
        LastUpdateEventTime = now;
        FirstEventTime ??= now;

        RecordEntry($"Windows Update event {eventId}", message);

        if (State == AppState.Idle)
        {
            SetState(AppState.Armed);
        }
    }

    public void RecordLoadSample(double diskPercent, double cpuPercent)
    {
        CurrentDiskActiveTimePercent = diskPercent;
        CurrentCpuPercent = cpuPercent;
        _diskAverage.AddSample(diskPercent);
        _cpuAverage.AddSample(cpuPercent);

        switch (State)
        {
            case AppState.Armed:
                EvaluateArmedState();
                break;

            case AppState.Warning:
                EvaluateWarningState();
                break;
        }
    }

    public void Tick()
    {
        if (State == AppState.Armed)
        {
            bool episodeExpired = _clock().Subtract(LastUpdateEventTime).TotalHours >= _settings.ArmedEpisodeHours;
            bool noActiveWarningCondition = !SignalsWarning();

            if (episodeExpired && noActiveWarningCondition)
            {
                ReturnToIdle("episode timeout");
            }
        }
    }

    public void DismissWarning()
    {
        if (State == AppState.Warning)
        {
            RecordEntry("Warning dismissed by user");
            _mustClearBeforeRewarn = true;
            SetState(AppState.Armed);
        }
    }

    public void ForceWarning()
    {
        if (State != AppState.Warning)
        {
            RecordEntry("Warning forced (debug)");
            SetState(AppState.Warning);
        }
    }

    public void ForceIdle()
    {
        ReturnToIdle("forced by user");
    }

    private bool _mustClearBeforeRewarn;

    private void EvaluateArmedState()
    {
        if (_mustClearBeforeRewarn)
        {
            if (BelowShowThresholds())
            {
                _mustClearBeforeRewarn = false;
            }
        }
        else if (SignalsWarning())
        {
            RecordEntry("Warning threshold crossed");
            SetState(AppState.Warning);
        }
    }

    private void EvaluateWarningState()
    {
        if (IsQuiet())
        {
            if (QuietStartTime == null)
            {
                QuietStartTime = _clock();
                RecordEntry("Load entered quiet range");
            }
            else
            {
                double quietMinutes = _clock().Subtract(QuietStartTime.Value).TotalMinutes;
                if (quietMinutes >= _settings.WarningHideQuietMinutes)
                {
                    RecordEntry("Quiet period complete, warning hidden");
                    _mustClearBeforeRewarn = false;
                    SetState(AppState.Armed);
                }
            }
        }
        else
        {
            if (QuietStartTime != null)
            {
                RecordEntry("Quiet timer reset");
                QuietStartTime = null;
            }
        }
    }

    private bool BelowShowThresholds()
    {
        return DiskRollingAverage < _settings.DiskShowPercent &&
               CpuRollingAverage < _settings.CpuShowPercent;
    }

    private bool IsQuiet()
    {
        return CurrentDiskActiveTimePercent < _settings.DiskHidePercent &&
               CurrentCpuPercent < _settings.CpuHidePercent;
    }

    private bool SignalsWarning()
    {
        return DiskRollingAverage >= _settings.DiskShowPercent ||
               CpuRollingAverage >= _settings.CpuShowPercent;
    }

    private void ReturnToIdle(string reason)
    {
        QuietStartTime = null;
        _mustClearBeforeRewarn = false;
        LastUpdateEventTime = DateTime.MinValue;
        SetState(AppState.Idle);
        ResetEpisodeData();
    }

    private void SetState(AppState newState)
    {
        AppState old = State;
        State = newState;

        if (newState == AppState.Warning)
        {
            WarningStartTime = _clock();
        }

        RecordEntry($"State: {old} -> {newState}");
        StateChanged?.Invoke(old, newState);
    }

    private void RecordEntry(string description, string? detail = null)
    {
        ((List<EpisodeEntry>)Entries).Add(new EpisodeEntry(_clock(), description, detail));
    }

    private void ResetEpisodeData()
    {
        FirstEventTime = null;
        WarningStartTime = null;
        LastUpdateEventTime = DateTime.MinValue;
        ((List<EpisodeEntry>)Entries).Clear();
        _cpuAverage.Clear();
        _diskAverage.Clear();
        CurrentCpuPercent = 0;
        CurrentDiskActiveTimePercent = 0;
    }
}