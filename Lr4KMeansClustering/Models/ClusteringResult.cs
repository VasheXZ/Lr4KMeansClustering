namespace Lr6KMeansClustering.Models;

public sealed class ClusterStatistics
{
    public int ClusterIndex { get; }
    public Point2D Center { get; }
    public int PointCount { get; }
    public double Objective { get; }

    public ClusterStatistics(int clusterIndex, Point2D center, int pointCount, double objective)
    {
        ClusterIndex = clusterIndex;
        Center = center;
        PointCount = pointCount;
        Objective = objective;
    }
}

// Итог одного запуска кластеризации
public sealed class ClusteringResult
{
    public IReadOnlyList<Point2D> SourcePoints { get; }
    public ClusteringOptions Options { get; }
    public IReadOnlyList<IterationSnapshot> Iterations { get; }
    public IReadOnlyList<Point2D> Centers { get; }
    public IReadOnlyList<int> Assignments { get; }
    public IReadOnlyList<ClusterStatistics> Clusters { get; }
    public double Objective { get; }
    public ClusteringStopReason StopReason { get; }
    public int IterationCount => Iterations.Count - 1;

    public ClusteringResult(
        IReadOnlyList<Point2D> sourcePoints,
        ClusteringOptions options,
        IReadOnlyList<IterationSnapshot> iterations,
        IReadOnlyList<Point2D> centers,
        IReadOnlyList<int> assignments,
        IReadOnlyList<ClusterStatistics> clusters,
        double objective,
        ClusteringStopReason stopReason)
    {
        SourcePoints = sourcePoints;
        Options = options;
        Iterations = iterations;
        Centers = centers;
        Assignments = assignments;
        Clusters = clusters;
        Objective = objective;
        StopReason = stopReason;
    }
}
