using Lr6KMeansClustering.Models;

namespace Lr6KMeansClustering.Algorithms;

// Классический K-means с квадратом евклидова расстояния
public static class KMeans
{
    private const double DistanceTieTolerance = 1e-12;
    private const double MaximumValue = 1e12;

    public static ClusteringResult Cluster(
        IEnumerable<Point2D> source,
        ClusteringOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(options);

        Point2D[] points = source.ToArray();
        Validate(points, options);
        cancellationToken.ThrowIfCancellationRequested();

        var random = new Random(options.RandomSeed);
        Point2D[] centers = InitializeCenters(points, options, random, cancellationToken);
        var iterations = new List<IterationSnapshot>
        {
            CreateInitialSnapshot(centers)
        };

        int[] previousAssignments = Enumerable.Repeat(-1, points.Length).ToArray();
        int[] assignments = new int[points.Length];
        ClusteringStopReason stopReason = ClusteringStopReason.IterationLimit;

        for (int iteration = 1; iteration <= options.MaxIterations; iteration++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Point2D[] centersBefore = centers.ToArray();
            double[][] distances = AssignToNearestCenters(
                points,
                centersBefore,
                assignments,
                cancellationToken);

            bool[] movedForEmptyCluster = new bool[points.Length];
            int[] reinitializedClusters = RepairEmptyClusters(
                assignments,
                distances,
                movedForEmptyCluster,
                options.ClusterCount);

            Point2D[] centersAfter = CalculateCenters(points, assignments, options.ClusterCount);
            EnsureFiniteCenters(centersAfter, iteration);

            int changedAssignments = CountAssignmentChanges(assignments, previousAssignments);
            double maximumShift = CalculateMaximumShift(centersBefore, centersAfter);
            double objective = CalculateObjective(points, assignments, centersAfter);
            EnsureFiniteObjective(objective, iteration);

            PointIteration[] pointSteps = CreatePointSteps(
                points,
                distances,
                assignments,
                movedForEmptyCluster);

            iterations.Add(new IterationSnapshot(
                iteration,
                centersBefore,
                pointSteps,
                centersAfter,
                assignments.ToArray(),
                reinitializedClusters,
                changedAssignments,
                maximumShift,
                objective));

            centers = centersAfter;

            if (changedAssignments == 0)
            {
                stopReason = ClusteringStopReason.AssignmentsUnchanged;
                break;
            }

            if (maximumShift <= options.CenterTolerance)
            {
                stopReason = ClusteringStopReason.CentersStable;
                break;
            }

            Array.Copy(assignments, previousAssignments, assignments.Length);
        }

        double finalObjective = CalculateObjective(points, assignments, centers);
        ClusterStatistics[] statistics = CreateStatistics(points, assignments, centers);

        return new ClusteringResult(
            points,
            options,
            iterations,
            centers,
            assignments.ToArray(),
            statistics,
            finalObjective,
            stopReason);
    }

    private static void Validate(IReadOnlyList<Point2D> points, ClusteringOptions options)
    {
        if (points.Count < 2)
        {
            throw new ArgumentException("Добавьте не менее двух точек.");
        }

        if (options.ClusterCount < 2 || options.ClusterCount > points.Count)
        {
            throw new ArgumentException("Число кластеров K должно быть от 2 до числа точек.");
        }

        if (options.MaxIterations is < 1 or > 10000)
        {
            throw new ArgumentException("Максимальное число итераций должно быть от 1 до 10000.");
        }

        if (!double.IsFinite(options.CenterTolerance) || options.CenterTolerance <= 0)
        {
            throw new ArgumentException("Допуск смещения центров должен быть положительным числом.");
        }

        if (points.Any(point =>
            !point.IsFinite() || Math.Abs(point.X) > 1e6 || Math.Abs(point.Y) > 1e6))
        {
            throw new ArgumentException(
                "Координаты должны быть конечными числами по модулю не больше 1000000.");
        }

        if (points.Distinct().Count() != points.Count)
        {
            throw new ArgumentException("Точки с одинаковыми координатами не допускаются.");
        }
    }

    private static Point2D[] InitializeCenters(
        IReadOnlyList<Point2D> points,
        ClusteringOptions options,
        Random random,
        CancellationToken cancellationToken)
    {
        return options.Initialization == InitializationMethod.RandomPoints
            ? InitializeRandom(points, options.ClusterCount, random)
            : InitializeKMeansPlusPlus(points, options.ClusterCount, random, cancellationToken);
    }

    private static Point2D[] InitializeRandom(
        IReadOnlyList<Point2D> points,
        int clusterCount,
        Random random)
    {
        int[] indices = Enumerable.Range(0, points.Count).ToArray();

        for (int index = 0; index < clusterCount; index++)
        {
            int selectedIndex = random.Next(index, indices.Length);
            (indices[index], indices[selectedIndex]) = (indices[selectedIndex], indices[index]);
        }

        return indices.Take(clusterCount).Select(index => points[index]).ToArray();
    }

    private static Point2D[] InitializeKMeansPlusPlus(
        IReadOnlyList<Point2D> points,
        int clusterCount,
        Random random,
        CancellationToken cancellationToken)
    {
        var selectedIndices = new List<int>(clusterCount) { random.Next(points.Count) };
        var selectedSet = new HashSet<int>(selectedIndices);

        while (selectedIndices.Count < clusterCount)
        {
            cancellationToken.ThrowIfCancellationRequested();
            double[] minimumDistances = new double[points.Count];
            double total = 0;

            for (int pointIndex = 0; pointIndex < points.Count; pointIndex++)
            {
                if (selectedSet.Contains(pointIndex))
                {
                    continue;
                }

                double minimum = selectedIndices.Min(centerIndex =>
                    points[pointIndex].DistanceSquaredTo(points[centerIndex]));
                minimumDistances[pointIndex] = minimum;
                total += minimum;
            }

            int nextIndex = total > 0
                ? SelectWeightedIndex(minimumDistances, total, random, selectedSet)
                : Enumerable.Range(0, points.Count).First(index => !selectedSet.Contains(index));

            selectedIndices.Add(nextIndex);
            selectedSet.Add(nextIndex);
        }

        return selectedIndices.Select(index => points[index]).ToArray();
    }

    private static int SelectWeightedIndex(
        IReadOnlyList<double> weights,
        double total,
        Random random,
        IReadOnlySet<int> excluded)
    {
        double threshold = random.NextDouble() * total;
        double accumulated = 0;

        for (int index = 0; index < weights.Count; index++)
        {
            if (excluded.Contains(index))
            {
                continue;
            }

            accumulated += weights[index];
            if (accumulated >= threshold)
            {
                return index;
            }
        }

        return Enumerable.Range(0, weights.Count).Last(index => !excluded.Contains(index));
    }

    private static IterationSnapshot CreateInitialSnapshot(IReadOnlyList<Point2D> centers)
    {
        Point2D[] copy = centers.ToArray();
        return new IterationSnapshot(
            0,
            copy,
            Array.Empty<PointIteration>(),
            copy,
            Array.Empty<int>(),
            Array.Empty<int>(),
            0,
            0,
            double.NaN);
    }

    private static double[][] AssignToNearestCenters(
        IReadOnlyList<Point2D> points,
        IReadOnlyList<Point2D> centers,
        int[] assignments,
        CancellationToken cancellationToken)
    {
        var distances = new double[points.Count][];

        for (int pointIndex = 0; pointIndex < points.Count; pointIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            distances[pointIndex] = new double[centers.Count];
            int bestCluster = 0;
            double bestDistance = double.PositiveInfinity;

            for (int clusterIndex = 0; clusterIndex < centers.Count; clusterIndex++)
            {
                double distance = points[pointIndex].DistanceSquaredTo(centers[clusterIndex]);
                distances[pointIndex][clusterIndex] = distance;

                double tolerance = DistanceTieTolerance * Math.Max(1, Math.Abs(bestDistance));
                if (clusterIndex == 0 || distance < bestDistance - tolerance)
                {
                    bestDistance = distance;
                    bestCluster = clusterIndex;
                }
            }

            assignments[pointIndex] = bestCluster;
        }

        return distances;
    }

    private static int[] RepairEmptyClusters(
        int[] assignments,
        IReadOnlyList<double[]> distances,
        bool[] movedForEmptyCluster,
        int clusterCount)
    {
        int[] counts = CountClusters(assignments, clusterCount);
        var repaired = new List<int>();

        for (int emptyCluster = 0; emptyCluster < clusterCount; emptyCluster++)
        {
            if (counts[emptyCluster] != 0)
            {
                continue;
            }

            int selectedPoint = Enumerable.Range(0, assignments.Length)
                .Where(index => counts[assignments[index]] > 1 && !movedForEmptyCluster[index])
                .OrderByDescending(index => distances[index][assignments[index]])
                .ThenBy(index => index)
                .First();

            int sourceCluster = assignments[selectedPoint];
            counts[sourceCluster]--;
            assignments[selectedPoint] = emptyCluster;
            counts[emptyCluster]++;
            movedForEmptyCluster[selectedPoint] = true;
            repaired.Add(emptyCluster);
        }

        return repaired.ToArray();
    }

    private static int[] CountClusters(IReadOnlyList<int> assignments, int clusterCount)
    {
        var counts = new int[clusterCount];
        foreach (int assignment in assignments)
        {
            counts[assignment]++;
        }

        return counts;
    }

    private static Point2D[] CalculateCenters(
        IReadOnlyList<Point2D> points,
        IReadOnlyList<int> assignments,
        int clusterCount)
    {
        var sumX = new double[clusterCount];
        var sumY = new double[clusterCount];
        var counts = new int[clusterCount];

        for (int index = 0; index < points.Count; index++)
        {
            int cluster = assignments[index];
            sumX[cluster] += points[index].X;
            sumY[cluster] += points[index].Y;
            counts[cluster]++;
        }

        return Enumerable.Range(0, clusterCount)
            .Select(cluster => new Point2D(
                sumX[cluster] / counts[cluster],
                sumY[cluster] / counts[cluster]))
            .ToArray();
    }

    private static int CountAssignmentChanges(
        IReadOnlyList<int> assignments,
        IReadOnlyList<int> previousAssignments)
    {
        int changed = 0;
        for (int index = 0; index < assignments.Count; index++)
        {
            if (assignments[index] != previousAssignments[index])
            {
                changed++;
            }
        }

        return changed;
    }

    private static double CalculateMaximumShift(
        IReadOnlyList<Point2D> before,
        IReadOnlyList<Point2D> after)
    {
        double maximumSquared = 0;
        for (int index = 0; index < before.Count; index++)
        {
            maximumSquared = Math.Max(maximumSquared, before[index].DistanceSquaredTo(after[index]));
        }

        return Math.Sqrt(maximumSquared);
    }

    private static double CalculateObjective(
        IReadOnlyList<Point2D> points,
        IReadOnlyList<int> assignments,
        IReadOnlyList<Point2D> centers)
    {
        double objective = 0;
        for (int index = 0; index < points.Count; index++)
        {
            objective += points[index].DistanceSquaredTo(centers[assignments[index]]);
        }

        return objective;
    }

    private static PointIteration[] CreatePointSteps(
        IReadOnlyList<Point2D> points,
        IReadOnlyList<double[]> distances,
        IReadOnlyList<int> assignments,
        IReadOnlyList<bool> movedForEmptyCluster)
    {
        return Enumerable.Range(0, points.Count)
            .Select(index => new PointIteration(
                index,
                points[index],
                distances[index],
                assignments[index],
                movedForEmptyCluster[index]))
            .ToArray();
    }

    private static ClusterStatistics[] CreateStatistics(
        IReadOnlyList<Point2D> points,
        IReadOnlyList<int> assignments,
        IReadOnlyList<Point2D> centers)
    {
        return Enumerable.Range(0, centers.Count)
            .Select(clusterIndex =>
            {
                int[] indices = Enumerable.Range(0, points.Count)
                    .Where(index => assignments[index] == clusterIndex)
                    .ToArray();
                double objective = indices.Sum(index =>
                    points[index].DistanceSquaredTo(centers[clusterIndex]));

                return new ClusterStatistics(
                    clusterIndex,
                    centers[clusterIndex],
                    indices.Length,
                    objective);
            })
            .ToArray();
    }

    private static void EnsureFiniteCenters(IReadOnlyList<Point2D> centers, int iteration)
    {
        bool invalid = centers.Any(center =>
            !center.IsFinite() || Math.Abs(center.X) > MaximumValue || Math.Abs(center.Y) > MaximumValue);

        if (invalid)
        {
            throw new ArgumentException($"Итерация {iteration}: получены недопустимые координаты центра.");
        }
    }

    private static void EnsureFiniteObjective(double objective, int iteration)
    {
        if (!double.IsFinite(objective) || objective > MaximumValue)
        {
            throw new ArgumentException($"Итерация {iteration}: значение J слишком велико.");
        }
    }
}
