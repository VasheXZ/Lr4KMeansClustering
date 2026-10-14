namespace Lr6KMeansClustering.Models;

// Точка двумерного пространства признаков
public readonly struct Point2D : IEquatable<Point2D>
{
    public double X { get; }
    public double Y { get; }

    public Point2D(double x, double y)
    {
        X = x;
        Y = y;
    }

    public double DistanceSquaredTo(Point2D other)
    {
        double deltaX = X - other.X;
        double deltaY = Y - other.Y;
        return deltaX * deltaX + deltaY * deltaY;
    }

    public bool IsFinite()
    {
        return double.IsFinite(X) && double.IsFinite(Y);
    }

    public bool Equals(Point2D other)
    {
        return X.Equals(other.X) && Y.Equals(other.Y);
    }

    public override bool Equals(object? obj)
    {
        return obj is Point2D other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }

    public static bool operator ==(Point2D left, Point2D right) => left.Equals(right);
    public static bool operator !=(Point2D left, Point2D right) => !left.Equals(right);
}
