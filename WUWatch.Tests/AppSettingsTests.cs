using WUWatch;

namespace WUWatch.Tests;

public class AppSettingsTests
{
    [Fact]
    public void ZeroAverageWindowIsClamped()
    {
        var settings = new AppSettings { WarningAverageSeconds = 0 };
        settings.Normalize();
        Assert.Equal(1, settings.WarningAverageSeconds);
    }

    [Fact]
    public void NegativeSampleIntervalIsClamped()
    {
        var settings = new AppSettings { SampleIntervalSeconds = -3 };
        settings.Normalize();
        Assert.Equal(1, settings.SampleIntervalSeconds);
    }

    [Fact]
    public void EmptyEventIdsFallBackToEvent41()
    {
        var settings = new AppSettings { RelevantWindowsUpdateEventIds = [] };
        settings.Normalize();
        Assert.Equal([41], settings.RelevantWindowsUpdateEventIds);
    }

    [Fact]
    public void DuplicateAndInvalidEventIdsAreSanitized()
    {
        var settings = new AppSettings
        {
            RelevantWindowsUpdateEventIds = [41, 41, 42, -7, 999999]
        };
        settings.Normalize();
        Assert.Equal([41, 42], settings.RelevantWindowsUpdateEventIds);
    }

    [Fact]
    public void OutOfRangePercentagesAreClamped()
    {
        var settings = new AppSettings
        {
            DiskShowPercent = 150,
            CpuShowPercent = -10,
            DiskHidePercent = 0,
            CpuHidePercent = 0
        };
        settings.Normalize();
        Assert.Equal(100, settings.DiskShowPercent);
        Assert.Equal(1, settings.CpuShowPercent);
    }
}