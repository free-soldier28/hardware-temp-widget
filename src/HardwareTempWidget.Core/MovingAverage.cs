namespace HardwareTempWidget.Core;

/// <summary>
/// Computes a moving average over a fixed-size sliding window of samples.
/// </summary>
public sealed class MovingAverage
{
    private readonly int _windowSize;
    private readonly Queue<float> _samples = new();
    private float _sum;

    public MovingAverage(int windowSize)
    {
        if (windowSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(windowSize));
        }

        _windowSize = windowSize;
    }

    public int SampleCount => _samples.Count;

    /// <summary>Adds a sample and returns the average over the current window.</summary>
    public float Add(float value)
    {
        _samples.Enqueue(value);
        _sum += value;

        while (_samples.Count > _windowSize)
        {
            _sum -= _samples.Dequeue();
        }

        return _sum / _samples.Count;
    }
}
