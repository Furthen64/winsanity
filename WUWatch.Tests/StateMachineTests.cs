using WUWatch;

namespace WUWatch.Tests;

public class StateMachineTests
{
    private static (WUWatchEngine engine, FakeClock clock) CreateArmedEngine()
    {
        var clock = new FakeClock();
        var engine = new WUWatchEngine(TestHelpers.CreateSettings(), clock.GetTimeProvider());
        engine.RecordUpdateEvent(41, "test");
        Assert.Equal(AppState.Armed, engine.State);
        return (engine, clock);
    }

    [Fact]
    public void StartsIdle()
    {
        var engine = new WUWatchEngine(TestHelpers.CreateSettings());
        Assert.Equal(AppState.Idle, engine.State);
    }

    [Fact]
    public void UpdateEventTransitionsIdleToArmed()
    {
        var engine = new WUWatchEngine(TestHelpers.CreateSettings());
        engine.RecordUpdateEvent(41, "downloaded");
        Assert.Equal(AppState.Armed, engine.State);
        Assert.NotNull(engine.FirstEventTime);
        Assert.Equal(2026, engine.FirstEventTime?.Year);
    }

    [Fact]
    public void SustainedHighDiskLoadTriggersWarning()
    {
        var (engine, _) = CreateArmedEngine();
        TestHelpers.FeedHighLoad(engine, 30, disk: 95, cpu: 10);
        Assert.Equal(AppState.Warning, engine.State);
    }

    [Fact]
    public void SustainedHighCpuLoadTriggersWarning()
    {
        var (engine, _) = CreateArmedEngine();
        TestHelpers.FeedHighLoad(engine, 30, disk: 10, cpu: 90);
        Assert.Equal(AppState.Warning, engine.State);
    }

    [Fact]
    public void SingleSpikeDoesNotTriggerWarning()
    {
        var (engine, _) = CreateArmedEngine();
        engine.RecordLoadSample(100, 100);
        Assert.Equal(AppState.Armed, engine.State);
    }

    [Fact]
    public void AveragesReportWarmupUntilWindowFilled()
    {
        var (engine, _) = CreateArmedEngine();

        engine.RecordLoadSample(10, 10);
        Assert.True(engine.AveragesWarmingUp);
        Assert.Equal(1, engine.AverageSampleCount);

        for (int i = 1; i < engine.AverageWindowSeconds; i++)
        {
            engine.RecordLoadSample(10, 10);
        }

        Assert.False(engine.AveragesWarmingUp);
        Assert.Equal(engine.AverageWindowSeconds, engine.AverageSampleCount);
    }

    [Fact]
    public void OrSemantics_WarningTriggersWhenOnlyDiskAboveThreshold()
    {
        var (engine, _) = CreateArmedEngine();
        TestHelpers.FeedHighLoad(engine, 30, disk: 95, cpu: 10);
        Assert.Equal(AppState.Warning, engine.State);
    }

    [Fact]
    public void WarningRequiresSustainedLoad_NeitherMetricAboveDoesNotWarn()
    {
        var (engine, _) = CreateArmedEngine();
        TestHelpers.FeedLowLoad(engine, 60);
        Assert.Equal(AppState.Armed, engine.State);
    }

    [Fact]
    public void ManualDismissalReturnsToArmed()
    {
        var (engine, _) = CreateArmedEngine();
        TestHelpers.FeedHighLoad(engine, 30);
        Assert.Equal(AppState.Warning, engine.State);

        engine.DismissWarning();
        Assert.Equal(AppState.Armed, engine.State);
    }

    [Fact]
    public void WarningDoesNotReopenWhileHighLoadNeverCleared()
    {
        var (engine, _) = CreateArmedEngine();
        TestHelpers.FeedHighLoad(engine, 30);
        Assert.Equal(AppState.Warning, engine.State);

        engine.DismissWarning();
        Assert.Equal(AppState.Armed, engine.State);

        TestHelpers.FeedHighLoad(engine, 60);
        Assert.Equal(AppState.Armed, engine.State);
    }

    [Fact]
    public void WarningReopensAfterLoadClearedThenRisesAgain()
    {
        var (engine, _) = CreateArmedEngine();
        TestHelpers.FeedHighLoad(engine, 30);
        Assert.Equal(AppState.Warning, engine.State);

        engine.DismissWarning();
        TestHelpers.FeedLowLoad(engine, 30);
        Assert.Equal(AppState.Armed, engine.State);

        TestHelpers.FeedHighLoad(engine, 30);
        Assert.Equal(AppState.Warning, engine.State);
    }

    [Fact]
    public void QuietTimerStartsWhenBothMetricsBelowHideThresholds()
    {
        var (engine, _) = CreateArmedEngine();
        engine.ForceWarning();
        Assert.Equal(AppState.Warning, engine.State);

        TestHelpers.FeedLowLoad(engine, 1);
        Assert.NotNull(engine.QuietStartTime);
    }

    [Fact]
    public void QuietTimerResetsWhenLoadRises()
    {
        var (engine, clock) = CreateArmedEngine();
        engine.ForceWarning();
        TestHelpers.FeedLowLoad(engine, 1);
        Assert.NotNull(engine.QuietStartTime);

        clock.Advance(TimeSpan.FromMinutes(5));
        TestHelpers.FeedHighLoad(engine, 1);
        Assert.Null(engine.QuietStartTime);
    }

    [Fact]
    public void WarningAutoHidesAfterQuietPeriod()
    {
        var (engine, clock) = CreateArmedEngine();
        engine.ForceWarning();
        Assert.Equal(AppState.Warning, engine.State);

        TestHelpers.FeedLowLoad(engine, 1);
        clock.Advance(TimeSpan.FromMinutes(14));
        engine.RecordLoadSample(10, 10);
        Assert.Equal(AppState.Warning, engine.State);

        clock.Advance(TimeSpan.FromMinutes(2));
        engine.RecordLoadSample(10, 10);
        Assert.Equal(AppState.Armed, engine.State);
    }

    [Fact]
    public void NewUpdateEventResetsEpisodeTimeout()
    {
        var (engine, clock) = CreateArmedEngine();

        clock.Advance(TimeSpan.FromHours(5));
        engine.RecordUpdateEvent(41, "again");
        clock.Advance(TimeSpan.FromHours(5));
        engine.Tick();
        Assert.Equal(AppState.Armed, engine.State);

        clock.Advance(TimeSpan.FromHours(4));
        engine.Tick();
        Assert.Equal(AppState.Idle, engine.State);
    }

    [Fact]
    public void ArmedReturnsToIdleAfterEpisodeTimeout()
    {
        var (engine, clock) = CreateArmedEngine();

        clock.Advance(TimeSpan.FromHours(9));
        engine.Tick();
        Assert.Equal(AppState.Idle, engine.State);
    }

    [Fact]
    public void ActiveWarningConditionPreventsReturnToIdle()
    {
        var (engine, clock) = CreateArmedEngine();
        TestHelpers.FeedHighLoad(engine, 30);

        clock.Advance(TimeSpan.FromHours(9));
        engine.Tick();
        Assert.Equal(AppState.Warning, engine.State);
    }

    [Fact]
    public void EpisodeDataResetsOnReturnToIdle()
    {
        var (engine, clock) = CreateArmedEngine();
        TestHelpers.FeedHighLoad(engine, 30);
        Assert.Equal(AppState.Warning, engine.State);

        TestHelpers.FeedLowLoad(engine, 30);
        clock.Advance(TimeSpan.FromMinutes(16));
        engine.RecordLoadSample(10, 10);
        Assert.Equal(AppState.Armed, engine.State);

        clock.Advance(TimeSpan.FromHours(9));
        engine.Tick();
        Assert.Equal(AppState.Idle, engine.State);
        Assert.Empty(engine.Entries);
        Assert.Null(engine.FirstEventTime);
    }

    [Fact]
    public void ForceIdleReturnsToIdle()
    {
        var (engine, _) = CreateArmedEngine();
        engine.ForceIdle();
        Assert.Equal(AppState.Idle, engine.State);
    }

    [Fact]
    public void UpdateEventAfterEpisodeReArmsFromIdle()
    {
        var (engine, clock) = CreateArmedEngine();
        clock.Advance(TimeSpan.FromHours(9));
        engine.Tick();
        Assert.Equal(AppState.Idle, engine.State);

        engine.RecordUpdateEvent(41, "new episode");
        Assert.Equal(AppState.Armed, engine.State);
    }
}