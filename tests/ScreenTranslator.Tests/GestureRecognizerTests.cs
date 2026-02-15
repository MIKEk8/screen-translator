using ScreenTranslator.Core.Models;

namespace ScreenTranslator.Tests;

public class GestureRecognizerTests
{
    [Fact]
    public void TooFewPoints_IsNotClosedShape()
    {
        var recognizer = new GestureRecognizer();
        for (int i = 0; i < 10; i++)
            recognizer.AddPoint(i, i);

        Assert.False(recognizer.IsClosedShape());
    }

    [Fact]
    public void ClosedCircle_IsClosedShape()
    {
        var recognizer = new GestureRecognizer();

        // Draw a circle with radius 100 centered at (200, 200)
        const int numPoints = 60;
        const double cx = 200, cy = 200, r = 100;
        for (int i = 0; i <= numPoints; i++)
        {
            var angle = 2 * Math.PI * i / numPoints;
            recognizer.AddPoint(cx + r * Math.Cos(angle), cy + r * Math.Sin(angle));
        }

        Assert.True(recognizer.IsClosedShape());
    }

    [Fact]
    public void ClosedCircle_BoundingBox_IsCorrect()
    {
        var recognizer = new GestureRecognizer();

        const int numPoints = 60;
        const double cx = 200, cy = 200, r = 100;
        for (int i = 0; i <= numPoints; i++)
        {
            var angle = 2 * Math.PI * i / numPoints;
            recognizer.AddPoint(cx + r * Math.Cos(angle), cy + r * Math.Sin(angle));
        }

        var box = recognizer.GetBoundingBox();
        Assert.True(box.X >= 99 && box.X <= 101);
        Assert.True(box.Y >= 99 && box.Y <= 101);
        Assert.True(box.Width >= 198 && box.Width <= 201);
        Assert.True(box.Height >= 198 && box.Height <= 201);
    }

    [Fact]
    public void OpenLine_IsNotClosedShape()
    {
        var recognizer = new GestureRecognizer();

        // Draw a straight line — start and end far apart
        for (int i = 0; i < 50; i++)
            recognizer.AddPoint(100 + i * 5, 100);

        Assert.False(recognizer.IsClosedShape());
    }

    [Fact]
    public void SmallShape_IsNotClosedShape()
    {
        var recognizer = new GestureRecognizer();

        // Draw a tiny circle (radius 10, area ~314 < 2500 threshold)
        const int numPoints = 40;
        const double cx = 100, cy = 100, r = 10;
        for (int i = 0; i <= numPoints; i++)
        {
            var angle = 2 * Math.PI * i / numPoints;
            recognizer.AddPoint(cx + r * Math.Cos(angle), cy + r * Math.Sin(angle));
        }

        Assert.False(recognizer.IsClosedShape());
    }

    [Fact]
    public void Reset_ClearsState()
    {
        var recognizer = new GestureRecognizer();
        recognizer.AddPoint(10, 20);
        recognizer.AddPoint(30, 40);

        Assert.Equal(2, recognizer.PointCount);

        recognizer.Reset();

        Assert.Equal(0, recognizer.PointCount);
        Assert.False(recognizer.IsClosedShape());
    }
}
