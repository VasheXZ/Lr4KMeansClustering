namespace Lr6KMeansClustering.Models;

public enum InitializationMethod
{
    RandomPoints,
    KMeansPlusPlus
}

// Параметры одного запуска K-средних
public sealed class ClusteringOptions
{
    public int ClusterCount { get; }
    public InitializationMethod Initialization { get; }
    public int MaxIterations { get; }
    public double CenterTolerance { get; }
    public int RandomSeed { get; }

    public ClusteringOptions(
        int clusterCount,
        InitializationMethod initialization,
        int maxIterations = 100,
        double centerTolerance = 1e-6,
        int randomSeed = 0)
    {
        ClusterCount = clusterCount;
        Initialization = initialization;
        MaxIterations = maxIterations;
        CenterTolerance = centerTolerance;
        RandomSeed = randomSeed;
    }
}
