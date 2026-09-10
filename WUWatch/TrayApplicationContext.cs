namespace WUWatch;

public sealed class TrayApplicationContext : ApplicationContext
{
    private readonly AppSettings _settings;
    private readonly AppLogger _logger;
    private readonly WUWatchEngine _engine;
    private readonly WindowsUpdateWatcher _updateWatcher;
    private readonly SystemLoadMonitor _loadMonitor;
    private readonly NotifyIcon _trayIcon;
    private readonly System.Windows.Forms.Timer _stateTimer;
    private readonly SynchronizationContext? _uiContext;

    private WarningForm? _warningForm;

    public TrayApplicationContext()
    {
        _uiContext = SynchronizationContext.Current;
        _settings = AppSettings.Load();
        _logger = new AppLogger();
        _engine = new WUWatchEngine(_settings);
        _updateWatcher = new WindowsUpdateWatcher(_settings, _logger);
        _loadMonitor = new SystemLoadMonitor(_settings, _logger);

        _trayIcon = new NotifyIcon
        {
            Icon = SystemIcons.Warning,
            Visible = true,
            Text = "WUWatch - Windows Update Monitor"
        };

        var menu = new ContextMenuStrip();
        menu.Items.Add("Status", null, OnStatus);
        menu.Items.Add("Show Window", null, OnShowWindow);
        menu.Items.Add("Details", null, OnDetails);
        menu.Items.Add("-");
        menu.Items.Add("Simulate Update Event", null, OnSimulateUpdateEvent);
        menu.Items.Add("Simulate High Load", null, OnSimulateHighLoad);
        menu.Items.Add("Simulate Normal Load", null, OnSimulateNormalLoad);
        menu.Items.Add("Force Warning", null, OnForceWarning);
        menu.Items.Add("Return to Idle", null, OnReturnToIdle);
        menu.Items.Add("-");
        menu.Items.Add("Exit", null, OnExit);

        _trayIcon.ContextMenuStrip = menu;

        _engine.StateChanged += OnEngineStateChanged;
        _updateWatcher.RelevantEventReceived += (id, msg) => PostToUi(() => OnRelevantUpdateEvent(id, msg));
        _loadMonitor.SampleTaken += sample => PostToUi(() => OnSampleTaken(sample));

        _stateTimer = new System.Windows.Forms.Timer { Interval = 1000 };
        _stateTimer.Tick += OnStateTimerElapsed;

        _logger.Info("WUWatch started");
        _updateWatcher.Start();
        _stateTimer.Start();
    }

    private void PostToUi(Action action)
    {
        if (_uiContext != null)
        {
            _uiContext.Post(_ => action(), null);
        }
        else
        {
            action();
        }
    }

    private void OnRelevantUpdateEvent(int eventId, string? message)
    {
        _logger.Info($"Relevant Windows Update event received: {eventId} - {message ?? ""}");
        _engine.RecordUpdateEvent(eventId, message);
        UpdateWarningForm();
    }

    private void OnSampleTaken(LoadSample sample)
    {
        _engine.RecordLoadSample(sample.DiskActiveTimePercent, sample.CpuPercent);
        UpdateWarningForm();
    }

    private void OnStateTimerElapsed(object? sender, EventArgs e)
    {
        _engine.Tick();
    }

    private void OnEngineStateChanged(AppState oldState, AppState newState)
    {
        _logger.Info($"State transition: {oldState} -> {newState}");

        switch (newState)
        {
            case AppState.Idle:
                _loadMonitor.Stop();
                HideWarningForm();
                break;

            case AppState.Armed:
                _loadMonitor.Start();
                HideWarningForm();
                break;

            case AppState.Warning:
                _loadMonitor.Start();
                ShowWarningForm();
                break;
        }

        UpdateTrayIcon();
    }

    private void UpdateTrayIcon()
    {
        _trayIcon.Text = _engine.State switch
        {
            AppState.Idle => "WUWatch - Idle",
            AppState.Armed => "WUWatch - Armed (monitoring)",
            AppState.Warning => "WUWatch - WARNING",
            _ => "WUWatch"
        };
    }

    private void ShowWarningForm()
    {
        if (_warningForm == null || _warningForm.IsDisposed)
        {
            _warningForm = new WarningForm(this);
        }

        _warningForm.Show();
        _warningForm.BringToFront();
        _warningForm.UpdateDisplay(_engine, _settings);
    }

    private void UpdateWarningForm()
    {
        if (_warningForm != null && !_warningForm.IsDisposed && _warningForm.Visible)
        {
            _warningForm.UpdateDisplay(_engine, _settings);
        }
    }

    private void HideWarningForm()
    {
        if (_warningForm != null && !_warningForm.IsDisposed)
        {
            _warningForm.Hide();
        }
    }

    internal WUWatchEngine Engine => _engine;

    internal void DismissWarning()
    {
        _engine.DismissWarning();
    }

    private void OnStatus(object? sender, EventArgs e)
    {
        MessageBox.Show(
            $"Current state: {_engine.State}\n\n" +
            $"Disk active time: {_engine.CurrentDiskActiveTimePercent:F1}%\n" +
            $"Disk average: {_engine.DiskRollingAverage:F1}%\n" +
            $"CPU: {_engine.CurrentCpuPercent:F1}%\n" +
            $"CPU average: {_engine.CpuRollingAverage:F1}%",
            "WUWatch Status",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void OnShowWindow(object? sender, EventArgs e)
    {
        ShowWarningForm();
    }

    private void OnDetails(object? sender, EventArgs e)
    {
        string content = string.Join(
            Environment.NewLine,
            _engine.Entries.Select(x => $"{x.Timestamp:HH:mm:ss}  {x.Description}"));

        MessageBox.Show(
            string.IsNullOrEmpty(content) ? "No episode history." : content,
            "WUWatch Episode Details",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void OnSimulateUpdateEvent(object? sender, EventArgs e)
    {
        _logger.Info("Simulated Windows Update event");
        OnRelevantUpdateEvent(41, "Simulated update download event");
    }

    private void OnSimulateHighLoad(object? sender, EventArgs e)
    {
        _logger.Info("Simulated high load");

        for (int i = 0; i < _settings.WarningAverageSeconds; i++)
        {
            _engine.RecordLoadSample(
                Math.Min(100, _settings.DiskShowPercent + 10),
                Math.Min(100, _settings.CpuShowPercent + 10));
        }

        UpdateWarningForm();
        _logger.Info("Simulated high load samples fed to engine");
    }

    private void OnSimulateNormalLoad(object? sender, EventArgs e)
    {
        _logger.Info("Simulated normal load");

        for (int i = 0; i < _settings.WarningAverageSeconds; i++)
        {
            _engine.RecordLoadSample(10, 10);
        }

        UpdateWarningForm();
        _logger.Info("Simulated normal load samples fed to engine");
    }

    private void OnForceWarning(object? sender, EventArgs e)
    {
        _logger.Info("Forced warning state");
        _engine.ForceWarning();
    }

    private void OnReturnToIdle(object? sender, EventArgs e)
    {
        _logger.Info("Forced return to idle");
        _engine.ForceIdle();
    }

    private void OnExit(object? sender, EventArgs e)
    {
        if (MessageBox.Show(
            "Are you sure you want to exit WUWatch?",
            "Confirm Exit",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question) == DialogResult.Yes)
        {
            _logger.Info("WUWatch shutting down");
            _stateTimer.Stop();
            _updateWatcher.Dispose();
            _loadMonitor.Dispose();
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            Application.Exit();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _stateTimer.Dispose();
            _updateWatcher.Dispose();
            _loadMonitor.Dispose();
            _trayIcon.Dispose();
        }
        base.Dispose(disposing);
    }
}