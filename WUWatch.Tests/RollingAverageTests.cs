using WUWatch;

namespace WUWatch.Tests;

public class RollingAverageTests
{
    [Fact]
    public void EmptyAverageIsZero()
    {
        var avg = new RollingAverage(5);
        Assert.Equal(0, avg.CurrentAverage);
    }

    [Fact]
    public void AverageDividesByFullWindowSize()
    {
        var avg = new RollingAverage(10);
        avg.AddSample(100);
        Assert.Equal(10, avg.CurrentAverage);
    }

    [Fact]
    public void AverageOfMultipleSamplesWithinWindow()
    {
        var avg = new RollingAverage(10);
        avg.AddSample(50);
        avg.AddSample(50);
        Assert.Equal(10, avg.CurrentAverage);
    }

    [Fact]
    public void WindowSlidesAndDropsOldSamples()
    {
        var avg = new RollingAverage(3);
        avg.AddSample(100);
        avg.AddSample(100);
        avg.AddSample(100);
        avg.AddSample(0);
        Assert.Equal(200.0 / 3.0, avg.CurrentAverage, precision: 6);
    }

    [Fact]
    public void ClearResetsSamples()
    {
        var avg = new RollingAverage(3);
        avg.AddSample(100);
        avg.Clear();
        Assert.Equal(0, avg.CurrentAverage);
        Assert.Equal(0, avg.SampleCount);
    }

    [Fact]
    public void ZeroWindowSizeIsClampedToMinimum()
    {
        var avg = new RollingAverage(0);
        avg.AddSample(100);
        Assert.Equal(100, avg.CurrentAverage, precision: 6);
    }

    [Fact]
    public void NegativeWindowSizeIsClampedToMinimum()
    {
        var avg = new RollingAverage(-5);
        avg.AddSample(100);
        Assert.Equal(100, avg.CurrentAverage, precision: 6);
    }
}