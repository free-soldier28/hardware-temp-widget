namespace HardwareTempWidget.Core.Tests;

public class MovingAverageTests
{
    [Fact]
    public void Add_ReturnsSameValue_ForSingleSample()
    {
        var avg = new MovingAverage(3);

        Assert.Equal(50f, avg.Add(50f));
    }

    [Fact]
    public void Add_AveragesSamples_WithinWindow()
    {
        var avg = new MovingAverage(3);

        avg.Add(10f);
        avg.Add(20f);
        var result = avg.Add(30f);

        Assert.Equal(20f, result);
    }

    [Fact]
    public void Add_WindowSlidesAndDropsOldSamples()
    {
        var avg = new MovingAverage(3);

        avg.Add(10f);
        avg.Add(20f);
        avg.Add(30f);
        var result = avg.Add(40f);

        Assert.Equal(30f, result);
    }

    [Fact]
    public void Add_SmoothesOutliers()
    {
        var avg = new MovingAverage(5);

        avg.Add(50f);
        avg.Add(50f);
        avg.Add(50f);
        avg.Add(50f);
        var result = avg.Add(82f);

        Assert.Equal(56.4f, result, precision: 1);
    }

    [Fact]
    public void SampleCount_DoesNotExceedWindowSize()
    {
        var avg = new MovingAverage(2);

        avg.Add(1f);
        avg.Add(2f);
        avg.Add(3f);

        Assert.Equal(2, avg.SampleCount);
    }

    [Fact]
    public void Constructor_RejectsNonPositiveWindow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new MovingAverage(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new MovingAverage(-1));
    }
}
