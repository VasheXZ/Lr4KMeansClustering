namespace Lr4KMeansClustering.Models;

public enum ClusteringStopReason
{
    AssignmentsUnchanged,
    CentersStable,
    IterationLimit
}

// Расстояния и назначение одной точки на выбранной итерации
public sealed class PointIteration
{
    public int PointIndex { get; }
    public Point2D Point { get; }
    public IReadOnlyList<double> SquaredDistances { get; }
    public int ClusterIndex { get; }
    public bool MovedToEmptyCluster { get; }

    public PointIteration(
        int pointIndex,
        Point2D point,
        IReadOnlyList<double> squaredDistances,
        int clusterIndex,
        bool movedToEmptyCluster)
    {
        PointIndex = pointIndex;
        Point = point;
        SquaredDistances = squaredDistances;
        ClusterIndex = clusterIndex;
        MovedToEmptyCluster = movedToEmptyCluster;
    }
}

// Полное состояние одной итерации; номер 0 хранит только начальные центры
public sealed class IterationSnapshot
{
    public int Number { get; }
    public IReadOnlyList<Point2D> CentersBefore { get; }
    public IReadOnlyList<PointIteration> Points { get; }
    public IReadOnlyList<Point2D> CentersAfter { get; }
    public IReadOnlyList<int> Assignments { get; }
    public IReadOnlyList<int> ReinitializedClusters { get; }
    public int ChangedAssignments { get; }
    public double MaximumCenterShift { get; }
    public double Objective { get; }
    public bool IsInitial => Number == 0;

    public IterationSnapshot(
        int number,
        IReadOnlyList<Point2D> centersBefore,
        IReadOnlyList<PointIteration> points,
        IReadOnlyList<Point2D> centersAfter,
        IReadOnlyList<int> assignments,
        IReadOnlyList<int> reinitializedClusters,
        int changedAssignments,
        double maximumCenterShift,
        double objective)
    {
        Number = number;
        CentersBefore = centersBefore;
        Points = points;
        CentersAfter = centersAfter;
        Assignments = assignments;
        ReinitializedClusters = reinitializedClusters;
        ChangedAssignments = changedAssignments;
        MaximumCenterShift = maximumCenterShift;
        Objective = objective;
    }
}
