using System.Diagnostics;
using System.Management;

namespace WUWatch;

public sealed record LoadSample(double DiskActiveTimePercent, double CpuPercent);

public sealed class SystemLoadMonitor : IDisposable
{
    private readonly AppSettings _settings;
    private readonly AppLogger _logger;
    private PerformanceCounter? _cpuCounter;
    private PerformanceCounter? _diskActiveTimeCounter;
    private System.Threading.Timer? _sampleTimer;
    private bool _monitoring;

    public event Action<LoadSample>? SampleTaken;

    public SystemLoadMonitor(AppSettings settings, AppLogger logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public void Start()
    {
        if (_monitoring) return;

        try
        {
            InitializeCounters();
            _monitoring = true;
            _sampleTimer = new System.Threading.Timer(
                OnSample,
                null,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(_settings.SampleIntervalSeconds));

            _logger.Info("System load monitoring started");
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to start system load monitoring", ex);
            _monitoring = false;
        }
    }

    public void Stop()
    {
        _monitoring = false;
        _sampleTimer?.Dispose();
        _sampleTimer = null;
        _cpuCounter?.Dispose();
        _cpuCounter = null;
        _diskActiveTimeCounter?.Dispose();
        _diskActiveTimeCounter = null;
        _logger.Info("System load monitoring stopped");
    }

    private void InitializeCounters()
    {
        _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");

        string diskInstance = FindSystemDiskInstance();
        _diskActiveTimeCounter = new PerformanceCounter("PhysicalDisk", "% Disk Time", diskInstance);
        _logger.Info($"Using disk counter instance: {diskInstance}");
    }

    private string FindSystemDiskInstance()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT DeviceID FROM Win32_LogicalDisk WHERE DeviceID='C:'");

            foreach (ManagementObject obj in searcher.Get())
            {
                string? deviceId = obj["DeviceID"]?.ToString();
                if (deviceId != null)
                {
                    return FindDiskPhysicalDrive(deviceId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Warn($"Failed to query disk instance via WMI: {ex.Message}");
        }

        return "_Total";
    }

    private string FindDiskPhysicalDrive(string logicalDisk)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                $"ASSOCIATORS OF {{Win32_LogicalDisk.DeviceID='{logicalDisk}'}} " +
                "WHERE AssocClass=Win32_LogicalDiskToDiskDrive");

            foreach (ManagementObject obj in searcher.Get())
            {
                string? driveId = obj["DeviceID"]?.ToString();
                if (driveId != null)
                {
                    return driveId.Replace("\\\\.\\PHYSICALDRIVE", "");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Warn($"Failed to find physical drive for {logicalDisk}: {ex.Message}");
        }

        return "_Total";
    }

    private void OnSample(object? state)
    {
        if (!_monitoring) return;

        try
        {
            double cpu = _cpuCounter?.NextValue() ?? 0;
            double disk = _diskActiveTimeCounter?.NextValue() ?? 0;
            SampleTaken?.Invoke(new LoadSample(disk, cpu));
        }
        catch (Exception ex)
        {
            _logger.Warn($"Error sampling system load: {ex.Message}");
        }
    }

    public void Dispose()
    {
        Stop();
    }
}