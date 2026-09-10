namespace WUWatch.Tests;

public sealed class FakeClock
{
    public DateTime Now { get; set; } = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Local);

    public void Advance(TimeSpan duration) => Now = Now.Add(duration);

    public Func<DateTime> GetTimeProvider() => () => Now;
}

public static class TestHelpers
{
    public static AppSettings CreateSettings() => new()
    {
        SampleIntervalSeconds = 1,
        WarningAverageSeconds = 30,
        DiskShowPercent = 80,
        CpuShowPercent = 85,
        DiskHidePercent = 50,
        CpuHidePercent = 60,
        WarningHideQuietMinutes = 15,
        ArmedEpisodeHours = 8,
        RelevantWindowsUpdateEventIds = [41]
    };

    public static void FeedHighLoad(WUWatchEngine engine, int count, double disk = 95, double cpu = 90)
    {
        for (int i = 0; i < count; i++)
        {
            engine.RecordLoadSample(disk, cpu);
        }
    }

    public static void FeedLowLoad(WUWatchEngine engine, int count, double disk = 10, double cpu = 10)
    {
        for (int i = 0; i < count; i++)
        {
            engine.RecordLoadSample(disk, cpu);
        }
    }
}