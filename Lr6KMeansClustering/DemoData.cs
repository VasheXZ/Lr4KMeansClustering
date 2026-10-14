using Lr6KMeansClustering.Models;

namespace Lr6KMeansClustering;

// Демонстрационная неразмеченная выборка для трёх кластеров
internal static class DemoData
{
    public const int ClusterCount = 3;
    public const int ReproducibleSeed = 2026;

    public static List<Point2D> Create()
    {
        return new List<Point2D>
        {
            new(3, 8),
            new(5, 8),
            new(4, 7),
            new(6, 7),
            new(8, 6),
            new(9, 5),
            new(8, 4),
            new(7, 5),
            new(9, 3),
            new(2, 2),
            new(3, 1),
            new(4, 2),
            new(3, 3)
        };
    }
}
