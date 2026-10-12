using Lr4KMeansClustering.Algorithms;
using Lr4KMeansClustering.Controls;
using Lr4KMeansClustering.Models;

namespace Lr4KMeansClustering;

// Главное окно интерактивной кластеризации
public sealed partial class MainForm : Form
{
    private const double CenterTolerance = 1e-6;
    private readonly List<Point2D> points = new();
    private ClusteringResult? clusteringResult;
    private CancellationTokenSource? clusteringCancellation;

    public MainForm()
    {
        InitializeComponent();
        ConnectEvents();
        UpdateClusterLegend();
        RefreshPoints();
    }

    private async Task ClusterAsync(int? fixedSeed = null)
    {
        int clusterCount = (int)clusterCountInput.Value;
        if (points.Count < clusterCount)
        {
            statusLabel.Text =
                $"Недостаточно точек: для K = {clusterCount} требуется не менее {clusterCount}.";
            statusLabel.ForeColor = Color.FromArgb(160, 73, 32);
            UpdateBuildAvailability();
            return;
        }

        InvalidateClusteringResult();

        using var cancellation = new CancellationTokenSource();
        clusteringCancellation = cancellation;
        SetCalculationState(true);

        Point2D[] source = points.ToArray();
        var options = new ClusteringOptions(
            clusterCount,
            GetInitializationMethod(),
            (int)maxIterationsInput.Value,
            CenterTolerance,
            fixedSeed ?? Random.Shared.Next());

        try
        {
            ClusteringResult result = await Task.Run(
                () => KMeans.Cluster(source, options, cancellation.Token),
                cancellation.Token);

            bool outdated = IsDisposed
                || cancellation.IsCancellationRequested
                || clusteringCancellation != cancellation;

            if (!outdated)
            {
                ShowClusteringResult(result);
            }
        }
        catch (ArgumentException exception)
        {
            if (!IsDisposed && clusteringCancellation == cancellation)
            {
                ShowClusteringError(exception.Message);
            }
        }
        catch (OperationCanceledException)
        {
            // Штатная отмена пользователем
        }
        finally
        {
            if (clusteringCancellation == cancellation)
            {
                clusteringCancellation = null;
                if (!IsDisposed)
                {
                    SetCalculationState(false);
                }
            }
        }
    }

    private void ShowClusteringResult(ClusteringResult result)
    {
        clusteringResult = result;
        clusterGrid.Rows.Clear();

        foreach (ClusterStatistics cluster in result.Clusters)
        {
            clusterGrid.Rows.Add(
                $"C{cluster.ClusterIndex + 1}",
                cluster.PointCount,
                $"({cluster.Center.X:F4}; {cluster.Center.Y:F4})",
                cluster.Objective.ToString("F4"));
        }

        clusterGrid.ClearSelection();
        summaryLabel.Text =
            $"J = {result.Objective:G10}\n" +
            $"Итераций: {result.IterationCount}; точек: {result.SourcePoints.Count}; seed: {result.Options.RandomSeed}\n" +
            FormatStopReason(result.StopReason);
        statusLabel.Text = "Кластеризация завершена. Выберите итерацию на вкладке «Ход работы».";
        statusLabel.ForeColor = Color.FromArgb(35, 115, 74);
        repeatButton.Enabled = true;

        iterationHistoryControl.SetResult(result);
        detailsTabControl.SelectedIndex = 0;
    }

    private void ShowClusteringError(string message)
    {
        statusLabel.Text = message;
        statusLabel.ForeColor = Color.FromArgb(160, 73, 32);
    }

    private void InvalidateClusteringResult()
    {
        clusteringCancellation?.Cancel();
        clusteringResult = null;
        coordinatePlane.Snapshot = null;
        iterationHistoryControl.SetResult(null);
        clusterGrid.Rows.Clear();
        summaryLabel.Text = "Результат ещё не построен.";
        repeatButton.Enabled = false;
        statusLabel.Text = "Добавьте точки или запустите построение.";
        statusLabel.ForeColor = Color.FromArgb(61, 77, 96);
        UpdateIdleStatus();
        UpdateBuildAvailability();
        coordinatePlane.Invalidate();
    }

    private void SetCalculationState(bool calculating)
    {
        buildButton.Text = calculating ? "Остановить" : "Построить";
        buildButton.Enabled = calculating || points.Count >= (int)clusterCountInput.Value;
        repeatButton.Enabled = !calculating && clusteringResult is not null;
        demoButton.Enabled = !calculating;
        resetButton.Enabled = !calculating;
        clusterCountInput.Enabled = !calculating;
        initializationSelector.Enabled = !calculating;
        maxIterationsInput.Enabled = !calculating;
    }

    private void RefreshPoints()
    {
        coordinatePlane.Points = points;
        pointCountLabel.Text = $"Точек: {points.Count}";
        UpdateBuildAvailability();
        coordinatePlane.Invalidate();
    }

    private void UpdateBuildAvailability()
    {
        if (clusteringCancellation is null)
        {
            buildButton.Enabled = points.Count >= (int)clusterCountInput.Value;
        }
    }

    private void UpdateIdleStatus()
    {
        int required = (int)clusterCountInput.Value;
        if (points.Count < required)
        {
            int missing = required - points.Count;
            statusLabel.Text =
                $"Для K = {required} добавьте ещё {missing} {GetPointWord(missing)}.";
            statusLabel.ForeColor = Color.FromArgb(160, 73, 32);
            return;
        }

        statusLabel.Text = "Можно запускать кластеризацию.";
        statusLabel.ForeColor = Color.FromArgb(61, 77, 96);
    }

    private static string GetPointWord(int count)
    {
        int lastTwoDigits = count % 100;
        if (lastTwoDigits is >= 11 and <= 14)
        {
            return "точек";
        }

        return (count % 10) switch
        {
            1 => "точку",
            2 or 3 or 4 => "точки",
            _ => "точек"
        };
    }

    private void UpdateClusterLegend()
    {
        clusterLegend.SuspendLayout();
        clusterLegend.Controls.Clear();

        for (int cluster = 0; cluster < (int)clusterCountInput.Value; cluster++)
        {
            clusterLegend.Controls.Add(new Label
            {
                AutoSize = true,
                Text = $"● C{cluster + 1}",
                ForeColor = ColorPalette.ForCluster(cluster),
                Margin = new Padding(2, 3, 12, 0)
            });
        }

        clusterLegend.ResumeLayout();
    }

    private void ConnectEvents()
    {
        buildButton.Click += async (_, _) =>
        {
            if (clusteringCancellation is not null)
            {
                clusteringCancellation.Cancel();
                return;
            }

            await ClusterAsync();
        };

        repeatButton.Click += async (_, _) => await ClusterAsync();
        demoButton.Click += async (_, _) =>
        {
            points.Clear();
            points.AddRange(DemoData.Create());
            clusterCountInput.Value = DemoData.ClusterCount;
            initializationSelector.SelectedIndex = 0;
            maxIterationsInput.Value = 100;
            RefreshPoints();
            await ClusterAsync(DemoData.ReproducibleSeed);
        };

        resetButton.Click += (_, _) =>
        {
            points.Clear();
            RefreshPoints();
            InvalidateClusteringResult();
        };

        clusterCountInput.ValueChanged += (_, _) =>
        {
            UpdateClusterLegend();
            InvalidateClusteringResult();
        };
        initializationSelector.SelectedIndexChanged += (_, _) => InvalidateIfResultExists();
        maxIterationsInput.ValueChanged += (_, _) => InvalidateIfResultExists();
        showLabelsCheckBox.CheckedChanged += (_, _) =>
        {
            coordinatePlane.ShowLabels = showLabelsCheckBox.Checked;
            coordinatePlane.Invalidate();
        };

        coordinatePlane.CoordinatesChanged += point =>
        {
            coordinateLabel.Text = point is null
                ? "X1: —    X2: —"
                : $"X1: {point.Value.X:F2}    X2: {point.Value.Y:F2}";
        };
        coordinatePlane.PointClicked += CoordinatePlanePointClicked;
        iterationHistoryControl.SnapshotChanged += snapshot =>
        {
            coordinatePlane.Snapshot = snapshot;
            coordinatePlane.Invalidate();
        };
    }

    private void InvalidateIfResultExists()
    {
        if (clusteringResult is not null)
        {
            InvalidateClusteringResult();
        }
    }

    private void CoordinatePlanePointClicked(Point2D point, MouseButtons button)
    {
        if (button == MouseButtons.Left)
        {
            AddPoint(point);
            return;
        }

        if (button == MouseButtons.Right)
        {
            RemoveNearestPoint(point);
        }
    }

    private void AddPoint(Point2D point)
    {
        if (points.Contains(point))
        {
            statusLabel.Text = "Точка с такими координатами уже существует.";
            statusLabel.ForeColor = Color.FromArgb(160, 73, 32);
            return;
        }

        points.Add(point);
        RefreshPoints();
        InvalidateClusteringResult();
    }

    private void RemoveNearestPoint(Point2D point)
    {
        PointF click = coordinatePlane.ToScreen(point);
        var nearest = points
            .Select((value, index) => new
            {
                Index = index,
                Distance = GetSquaredScreenDistance(click, value)
            })
            .Where(item => item.Distance <= 100)
            .OrderBy(item => item.Distance)
            .FirstOrDefault();

        if (nearest is null)
        {
            return;
        }

        points.RemoveAt(nearest.Index);
        RefreshPoints();
        InvalidateClusteringResult();
    }

    private double GetSquaredScreenDistance(PointF screenPoint, Point2D worldPoint)
    {
        PointF other = coordinatePlane.ToScreen(worldPoint);
        double deltaX = screenPoint.X - other.X;
        double deltaY = screenPoint.Y - other.Y;
        return deltaX * deltaX + deltaY * deltaY;
    }

    private InitializationMethod GetInitializationMethod()
    {
        return initializationSelector.SelectedIndex == 1
            ? InitializationMethod.KMeansPlusPlus
            : InitializationMethod.RandomPoints;
    }

    private static string FormatStopReason(ClusteringStopReason reason)
    {
        return reason switch
        {
            ClusteringStopReason.AssignmentsUnchanged =>
                "Причина остановки: распределение точек не изменилось.",
            ClusteringStopReason.CentersStable =>
                $"Причина остановки: смещение центров не превышает {CenterTolerance:G1}.",
            _ => "Причина остановки: достигнут предел итераций."
        };
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        clusteringCancellation?.Cancel();
        base.OnFormClosing(e);
    }
}
