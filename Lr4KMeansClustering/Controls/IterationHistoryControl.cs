using System.Text;
using Lr4KMeansClustering.Models;

namespace Lr4KMeansClustering.Controls;

// Просмотр исходных центров и подробностей выбранной итерации
public sealed class IterationHistoryControl : UserControl
{
    private readonly NumericUpDown iterationInput;
    private readonly DataGridView pointGrid;
    private readonly TextBox detailsBox;
    private ClusteringResult? result;
    private bool updating;

    public event Action<IterationSnapshot?>? SnapshotChanged;

    public IterationHistoryControl()
    {
        var header = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 42,
            Padding = new Padding(8, 8, 8, 4),
            WrapContents = false
        };
        header.Controls.Add(new Label
        {
            Text = "Итерация:",
            AutoSize = true,
            Margin = new Padding(0, 6, 8, 0)
        });

        iterationInput = new NumericUpDown
        {
            Minimum = 0,
            Maximum = 0,
            Width = 90,
            Enabled = false
        };
        iterationInput.ValueChanged += (_, _) => ShowSelectedIteration();
        header.Controls.Add(iterationInput);

        pointGrid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.Fixed3D,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false
        };
        pointGrid.DefaultCellStyle.Font = new Font("Segoe UI", 9f);
        pointGrid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f);
        pointGrid.ColumnHeadersHeight = 28;
        pointGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        pointGrid.RowTemplate.Height = 25;

        detailsBox = new TextBox
        {
            Dock = DockStyle.Bottom,
            Height = 155,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            BackColor = Color.White,
            Font = new Font("Consolas", 9.5f)
        };

        Controls.Add(pointGrid);
        Controls.Add(detailsBox);
        Controls.Add(header);
    }

    public void SetResult(ClusteringResult? clusteringResult)
    {
        result = clusteringResult;
        updating = true;

        pointGrid.Columns.Clear();
        pointGrid.Rows.Clear();
        detailsBox.Clear();
        iterationInput.Enabled = result is not null;
        iterationInput.Maximum = result is null ? 0 : result.Iterations.Count - 1;
        iterationInput.Value = result is null ? 0 : result.Iterations.Count - 1;

        updating = false;
        ShowSelectedIteration();
    }

    public void ShowFinalIteration()
    {
        if (result is null)
        {
            return;
        }

        iterationInput.Value = result.Iterations.Count - 1;
    }

    private void ShowSelectedIteration()
    {
        if (updating)
        {
            return;
        }

        if (result is null)
        {
            SnapshotChanged?.Invoke(null);
            return;
        }

        IterationSnapshot snapshot = result.Iterations[(int)iterationInput.Value];
        BuildColumns(result.Options.ClusterCount);
        FillRows(snapshot);
        detailsBox.Text = FormatDetails(snapshot);
        SnapshotChanged?.Invoke(snapshot);
    }

    private void BuildColumns(int clusterCount)
    {
        pointGrid.Columns.Clear();
        AddColumn("Point", "Точка");
        AddColumn("X", "X1");
        AddColumn("Y", "X2");

        for (int cluster = 0; cluster < clusterCount; cluster++)
        {
            AddColumn($"D{cluster + 1}", $"d до z{cluster + 1}");
        }

        AddColumn("Cluster", "Кластер");
        AddColumn("Note", "Примечание");
    }

    private void AddColumn(string name, string header)
    {
        pointGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = name,
            HeaderText = header,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });
    }

    private void FillRows(IterationSnapshot snapshot)
    {
        pointGrid.Rows.Clear();
        if (snapshot.IsInitial)
        {
            return;
        }

        foreach (PointIteration step in snapshot.Points)
        {
            var values = new List<object>
            {
                $"x{step.PointIndex + 1}",
                step.Point.X.ToString("F4"),
                step.Point.Y.ToString("F4")
            };

            values.AddRange(step.SquaredDistances.Select(distance => Math.Sqrt(distance).ToString("F4")));
            values.Add($"C{step.ClusterIndex + 1}");
            values.Add(step.MovedToEmptyCluster ? "Перенос в пустой кластер" : string.Empty);

            pointGrid.Rows.Add(values.ToArray());
        }

        pointGrid.ClearSelection();
    }

    private static string FormatDetails(IterationSnapshot snapshot)
    {
        var text = new StringBuilder();

        if (snapshot.IsInitial)
        {
            text.AppendLine("Итерация 0. Автоматически выбранные начальные центры:");
            AppendCenters(text, snapshot.CentersAfter);
            text.AppendLine();
            text.Append("Распределение точек ещё не выполнялось.");
            return text.ToString();
        }

        text.AppendLine($"Итерация {snapshot.Number}");
        text.AppendLine("Центры до распределения:");
        AppendCenters(text, snapshot.CentersBefore);
        text.AppendLine("Центры после пересчёта:");
        AppendCenters(text, snapshot.CentersAfter);
        text.AppendLine($"Перешло точек: {snapshot.ChangedAssignments}");
        text.AppendLine($"Максимальное смещение центра: {snapshot.MaximumCenterShift:G8}");
        text.AppendLine($"J = {snapshot.Objective:G10}");

        if (snapshot.ReinitializedClusters.Count > 0)
        {
            string clusters = string.Join(", ", snapshot.ReinitializedClusters.Select(index => $"C{index + 1}"));
            text.Append($"Переинициализированы пустые кластеры: {clusters}.");
        }

        return text.ToString();
    }

    private static void AppendCenters(StringBuilder text, IReadOnlyList<Point2D> centers)
    {
        for (int index = 0; index < centers.Count; index++)
        {
            text.AppendLine($"  z{index + 1} = ({centers[index].X:G8}; {centers[index].Y:G8})");
        }
    }
}
