namespace WUWatch;

public sealed class WarningForm : Form
{
    private readonly TrayApplicationContext _context;
    private readonly Label _titleLabel;
    private readonly Label _diskLabel;
    private readonly Label _diskAvgLabel;
    private readonly Label _cpuLabel;
    private readonly Label _cpuAvgLabel;
    private readonly Label _episodeStartLabel;
    private readonly Label _lastEventLabel;
    private readonly Label _quietTimerLabel;
    private readonly Button _dismissButton;
    private DateTime _dismissButtonDownTime;
    private System.Windows.Forms.Timer? _dismissTimer;

    public WarningForm(TrayApplicationContext context)
    {
        _context = context;

        Text = "WUWatch - Windows Update Warning";
        Size = new Size(500, 400);
        StartPosition = FormStartPosition.CenterScreen;
        TopMost = true;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;

        _titleLabel = new Label
        {
            Text = "WINDOWS UPDATE ACTIVE\nWindows Update activity was detected and is currently associated with significant system load.",
            Font = new Font(FontFamily.GenericSansSerif, 12F, FontStyle.Bold),
            ForeColor = Color.Black,
            TextAlign = ContentAlignment.TopCenter,
            Dock = DockStyle.Top,
            Height = 60
        };

        var infoPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20)
        };

        _diskLabel = CreateInfoLabel();
        _diskAvgLabel = CreateInfoLabel();
        _cpuLabel = CreateInfoLabel();
        _cpuAvgLabel = CreateInfoLabel();
        _episodeStartLabel = CreateInfoLabel();
        _lastEventLabel = CreateInfoLabel();
        _quietTimerLabel = CreateInfoLabel();

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 7
        };

        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));

        layout.Controls.Add(new Label { Text = "Disk active time:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 0, 0);
        layout.Controls.Add(_diskLabel, 1, 0);
        layout.Controls.Add(new Label { Text = "Disk rolling average:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 0, 1);
        layout.Controls.Add(_diskAvgLabel, 1, 1);
        layout.Controls.Add(new Label { Text = "CPU:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 0, 2);
        layout.Controls.Add(_cpuLabel, 1, 2);
        layout.Controls.Add(new Label { Text = "CPU rolling average:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 0, 3);
        layout.Controls.Add(_cpuAvgLabel, 1, 3);
        layout.Controls.Add(new Label { Text = "Update activity began:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 0, 4);
        layout.Controls.Add(_episodeStartLabel, 1, 4);
        layout.Controls.Add(new Label { Text = "Last update event:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 0, 5);
        layout.Controls.Add(_lastEventLabel, 1, 5);
        layout.Controls.Add(new Label { Text = "System quiet time:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 0, 6);
        layout.Controls.Add(_quietTimerLabel, 1, 6);

        infoPanel.Controls.Add(layout);

        _dismissButton = new Button
        {
            Text = "Hold to Dismiss (3 sec)",
            Dock = DockStyle.Bottom,
            Height = 40,
            Visible = false
        };
        _dismissButton.MouseDown += OnDismissButtonDown;
        _dismissButton.MouseUp += OnDismissButtonUp;

        Controls.Add(infoPanel);
        Controls.Add(_dismissButton);
        Controls.Add(_titleLabel);

        FormClosing += OnFormClosing;
    }

    private static Label CreateInfoLabel()
    {
        return new Label
        {
            TextAlign = ContentAlignment.MiddleLeft,
            Dock = DockStyle.Fill,
            Font = new Font(FontFamily.GenericMonospace, 10F)
        };
    }

    public void UpdateDisplay(WUWatchEngine engine, AppSettings settings)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => UpdateDisplay(engine, settings));
            return;
        }

        _titleLabel.BackColor = Color.Red;
        _titleLabel.ForeColor = Color.White;
        _dismissButton.Visible = engine.State == AppState.Warning;

        _diskLabel.Text = $"{engine.CurrentDiskActiveTimePercent:F1}%";
        _cpuLabel.Text = $"{engine.CurrentCpuPercent:F1}%";

        bool warmingUp = engine.AveragesWarmingUp;
        string warmup = $"  (warming up {engine.AverageSampleCount}/{engine.AverageWindowSeconds})";

        _diskAvgLabel.Text = engine.DiskRollingAverage.ToString("F1") + "%" + (warmingUp ? warmup : "");
        _cpuAvgLabel.Text = engine.CpuRollingAverage.ToString("F1") + "%" + (warmingUp ? warmup : "");

        Color avgColor = warmingUp ? Color.OrangeRed : Color.Black;
        _diskAvgLabel.ForeColor = avgColor;
        _cpuAvgLabel.ForeColor = avgColor;

        _episodeStartLabel.Text = engine.FirstEventTime?.ToString("HH:mm:ss") ?? "N/A";
        _lastEventLabel.Text = engine.LastUpdateEventTime == DateTime.MinValue
            ? "N/A"
            : engine.LastUpdateEventTime.ToString("HH:mm:ss");

        if (engine.QuietStartTime != null)
        {
            TimeSpan elapsed = DateTime.Now - engine.QuietStartTime.Value;
            TimeSpan required = TimeSpan.FromMinutes(settings.WarningHideQuietMinutes);
            _quietTimerLabel.Text = $"{elapsed:mm\\:ss} / {required:mm\\:ss} required";
        }
        else
        {
            _quietTimerLabel.Text = "Not active";
        }
    }

    private void OnDismissButtonDown(object? sender, MouseEventArgs e)
    {
        if (_dismissTimer != null)
        {
            _dismissTimer.Stop();
            _dismissTimer.Dispose();
            _dismissTimer = null;
        }

        _dismissButtonDownTime = DateTime.Now;
        _dismissTimer = new System.Windows.Forms.Timer { Interval = 100 };
        _dismissTimer.Tick += OnDismissTimerTick;
        _dismissTimer.Start();
    }

    private void OnDismissTimerTick(object? sender, EventArgs e)
    {
        double elapsed = (DateTime.Now - _dismissButtonDownTime).TotalSeconds;
        int remaining = Math.Max(0, 3 - (int)elapsed);
        _dismissButton.Text = remaining > 0
            ? $"Hold to Dismiss ({remaining} sec)"
            : "Release to Dismiss";

        if (remaining <= 0 && _dismissTimer != null)
        {
            _dismissTimer.Stop();
            _dismissTimer.Dispose();
            _dismissTimer = null;
        }
    }

    private void OnDismissButtonUp(object? sender, MouseEventArgs e)
    {
        double elapsed = (DateTime.Now - _dismissButtonDownTime).TotalSeconds;

        if (_dismissTimer != null)
        {
            _dismissTimer.Stop();
            _dismissTimer.Dispose();
            _dismissTimer = null;
        }

        _dismissButton.Text = "Hold to Dismiss (3 sec)";

        if (elapsed >= 3.0)
        {
            _context.DismissWarning();
        }
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            if (_context.Engine.State != AppState.Warning)
            {
                e.Cancel = false;
                return;
            }

            var result = MessageBox.Show(
                "Windows Update activity is still being monitored.\n\n" +
                "Closing this warning may hide ongoing system load.",
                "Confirm Dismiss",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.No)
            {
                e.Cancel = true;
                return;
            }

            e.Cancel = true;
            _context.DismissWarning();
        }
    }
}