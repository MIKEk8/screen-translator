namespace ScreenTranslator.Core.Models;

/// <summary>
/// Recognizes a closed shape (circle/oval) drawn by the user's mouse path.
/// Works in physical pixel coordinates.
/// </summary>
public class GestureRecognizer
{
    private readonly List<(double X, double Y)> _points = [];

    private const double CloseThresholdPx = 50;
    private const int MinPointsForShape = 30;
    private const double MinBoundingBoxArea = 2500;

    public void AddPoint(double x, double y)
    {
        _points.Add((x, y));
    }

    public bool IsClosedShape()
    {
        if (_points.Count < MinPointsForShape) return false;

        var (startX, startY) = _points[0];
        var (endX, endY) = _points[^1];
        var distance = Math.Sqrt((endX - startX) * (endX - startX) + (endY - startY) * (endY - startY));

        if (distance >= CloseThresholdPx) return false;

        var (minX, minY, maxX, maxY) = GetBounds();
        var area = (maxX - minX) * (maxY - minY);
        return area >= MinBoundingBoxArea;
    }

    public ScreenRegion GetBoundingBox()
    {
        var (minX, minY, maxX, maxY) = GetBounds();
        return new ScreenRegion(
            (int)minX,
            (int)minY,
            (int)(maxX - minX),
            (int)(maxY - minY));
    }

    public IReadOnlyList<(double X, double Y)> GetPoints() => _points;

    public int PointCount => _points.Count;

    public void Reset()
    {
        _points.Clear();
    }

    private (double MinX, double MinY, double MaxX, double MaxY) GetBounds()
    {
        if (_points.Count == 0) return (0, 0, 0, 0);

        double minX = double.MaxValue, minY = double.MaxValue;
        double maxX = double.MinValue, maxY = double.MinValue;

        foreach (var (x, y) in _points)
        {
            if (x < minX) minX = x;
            if (y < minY) minY = y;
            if (x > maxX) maxX = x;
            if (y > maxY) maxY = y;
        }

        return (minX, minY, maxX, maxY);
    }
}
