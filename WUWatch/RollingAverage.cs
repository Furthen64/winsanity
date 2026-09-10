namespace WUWatch;

public sealed class RollingAverage
{
    private readonly int _windowSize;
    private readonly Queue<double> _samples = new();

    public RollingAverage(int windowSize)
    {
        _windowSize = Math.Max(1, windowSize);
    }

    public void AddSample(double value)
    {
        _samples.Enqueue(value);
        while (_samples.Count > _windowSize)
        {
            _samples.Dequeue();
        }
    }

    public void Clear() => _samples.Clear();

    public double CurrentAverage => _samples.Count == 0
        ? 0
        : _samples.Sum() / _windowSize;

    public int SampleCount => _samples.Count;
}