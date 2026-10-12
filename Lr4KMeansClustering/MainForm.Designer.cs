using Lr4KMeansClustering.Controls;

namespace Lr4KMeansClustering;

public sealed partial class MainForm
{
    private CoordinatePlane coordinatePlane = null!;
    private NumericUpDown clusterCountInput = null!;
    private ComboBox initializationSelector = null!;
    private NumericUpDown maxIterationsInput = null!;
    private Button buildButton = null!;
    private Button repeatButton = null!;
    private Button demoButton = null!;
    private Button resetButton = null!;
    private CheckBox showLabelsCheckBox = null!;
    private Label coordinateLabel = null!;
    private Label pointCountLabel = null!;
    private Label statusLabel = null!;
    private Label summaryLabel = null!;
    private FlowLayoutPanel clusterLegend = null!;
    private DataGridView clusterGrid = null!;
    private TabControl detailsTabControl = null!;
    private IterationHistoryControl iterationHistoryControl = null!;

    private void InitializeComponent()
    {
        Text = "Кластеризация методом K-средних";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(1240, 820);
        MinimumSize = new Size(1120, 780);
        Font = new Font("Segoe UI", 10f);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Color.FromArgb(244, 247, 251);

        CreateWindowLayout();
    }

    private void CreateWindowLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 1,
            Padding = new Padding(16),
            BackColor = Color.FromArgb(244, 247, 251)
        };

        var body = new SplitContainer
        {
            Dock = DockStyle.Fill,
            FixedPanel = FixedPanel.Panel2,
            Size = new Size(1208, 788),
            SplitterDistance = 770,
            SplitterWidth = 12,
            Panel1MinSize = 330,
            Panel2MinSize = 405,
            Margin = Padding.Empty,
            BackColor = Color.FromArgb(214, 222, 233)
        };
        body.Panel1.Controls.Add(CreateGraphPanel());
        body.Panel2.Controls.Add(CreateSidePanel());

        root.Controls.Add(body, 0, 0);
        Controls.Add(root);
    }

    private Control CreateGraphPanel()
    {
        var graphPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = Padding.Empty,
            BackColor = Color.White
        };
        graphPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        graphPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var graphBar = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            Padding = new Padding(10, 0, 10, 0),
            Margin = Padding.Empty,
            BackColor = Color.White
        };
        graphBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));
        graphBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 105));
        graphBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52));

        coordinateLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = "X1: —    X2: —",
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.FromArgb(63, 75, 91)
        };
        pointCountLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Точек: 0",
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.FromArgb(63, 75, 91)
        };
        showLabelsCheckBox = new CheckBox
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            Checked = true,
            Text = "Подписи точек и центров",
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.FromArgb(52, 66, 84),
            Margin = new Padding(8, 0, 0, 0)
        };

        graphBar.Controls.Add(coordinateLabel, 0, 0);
        graphBar.Controls.Add(pointCountLabel, 1, 0);
        graphBar.Controls.Add(showLabelsCheckBox, 2, 0);

        coordinatePlane = new CoordinatePlane
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty
        };

        graphPanel.Controls.Add(graphBar, 0, 0);
        graphPanel.Controls.Add(coordinatePlane, 0, 1);
        return graphPanel;
    }

    private Control CreateSidePanel()
    {
        var sidePanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 6,
            Padding = new Padding(12, 10, 12, 12),
            Margin = Padding.Empty,
            BackColor = Color.White
        };
        sidePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        sidePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
        sidePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        sidePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        sidePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
        sidePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var header = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Параметры кластеризации",
            Font = new Font("Segoe UI Semibold", 11f),
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.FromArgb(31, 48, 70),
            Margin = Padding.Empty
        };
        sidePanel.Controls.Add(header, 0, 0);
        sidePanel.Controls.Add(CreateParameterPanel(), 0, 1);

        buildButton = CreateButton("Построить", true);
        buildButton.Dock = DockStyle.Fill;
        buildButton.Margin = new Padding(0, 5, 0, 5);
        sidePanel.Controls.Add(buildButton, 0, 2);

        repeatButton = CreateButton("Повторить", false);
        demoButton = CreateButton("Пример", false);
        resetButton = CreateButton("Очистить", false);
        sidePanel.Controls.Add(CreateThreeButtonRow(repeatButton, demoButton, resetButton), 0, 3);

        statusLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Добавьте точки на поле или загрузите демонстрационный пример.",
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.FromArgb(61, 77, 96),
            Padding = new Padding(2, 4, 2, 4),
            Margin = Padding.Empty
        };
        sidePanel.Controls.Add(statusLabel, 0, 4);

        detailsTabControl = new TabControl
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 4, 0, 0)
        };
        detailsTabControl.TabPages.Add(CreateResultTab());
        detailsTabControl.TabPages.Add(CreateHistoryTab());
        sidePanel.Controls.Add(detailsTabControl, 0, 5);
        return sidePanel;
    }

    private Control CreateParameterPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            Margin = Padding.Empty,
            Padding = new Padding(0, 4, 0, 4)
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33f));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 33.34f));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33f));

        panel.Controls.Add(CreateCaption("Число кластеров K"), 0, 0);
        clusterCountInput = new NumericUpDown
        {
            Minimum = 2,
            Maximum = 10,
            Value = 3,
            Dock = DockStyle.Fill,
            Margin = new Padding(8, 3, 0, 3)
        };
        panel.Controls.Add(clusterCountInput, 1, 0);

        panel.Controls.Add(CreateCaption("Начальные центры"), 0, 1);
        initializationSelector = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Dock = DockStyle.Fill,
            Margin = new Padding(8, 3, 0, 3)
        };
        initializationSelector.Items.AddRange(new object[] { "Случайные точки", "K-means++" });
        initializationSelector.SelectedIndex = 0;
        panel.Controls.Add(initializationSelector, 1, 1);

        panel.Controls.Add(CreateCaption("Максимум итераций"), 0, 2);
        maxIterationsInput = new NumericUpDown
        {
            Minimum = 1,
            Maximum = 10000,
            Value = 100,
            Dock = DockStyle.Fill,
            Margin = new Padding(8, 3, 0, 3)
        };
        panel.Controls.Add(maxIterationsInput, 1, 2);
        return panel;
    }

    private static Control CreateThreeButtonRow(Button first, Button second, Button third)
    {
        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            Margin = Padding.Empty
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34f));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));

        first.Dock = DockStyle.Fill;
        second.Dock = DockStyle.Fill;
        third.Dock = DockStyle.Fill;
        first.Margin = new Padding(0, 4, 4, 4);
        second.Margin = new Padding(4, 4, 4, 4);
        third.Margin = new Padding(4, 4, 0, 4);
        row.Controls.Add(first, 0, 0);
        row.Controls.Add(second, 1, 0);
        row.Controls.Add(third, 2, 0);
        return row;
    }

    private TabPage CreateResultTab()
    {
        var tab = new TabPage("Результат")
        {
            Padding = new Padding(8),
            BackColor = Color.White
        };
        summaryLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 88,
            Text = "Результат ещё не построен.",
            ForeColor = Color.FromArgb(38, 54, 73),
            Padding = new Padding(2, 4, 2, 4)
        };
        clusterLegend = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 32,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(2, 3, 2, 3),
            Margin = Padding.Empty,
            BackColor = Color.White
        };
        clusterGrid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.Fixed3D
        };
        clusterGrid.Columns.Add("Cluster", "Кластер");
        clusterGrid.Columns.Add("Count", "Точек");
        clusterGrid.Columns.Add("Center", "Центр");
        clusterGrid.Columns.Add("Objective", "Jk (сумма d²)");
        clusterGrid.Columns["Objective"]!.ToolTipText =
            "Jk — сумма квадратов расстояний от точек к центру кластера";
        clusterGrid.DefaultCellStyle.Font = new Font("Segoe UI", 9f);
        clusterGrid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f);
        clusterGrid.ColumnHeadersHeight = 28;
        clusterGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        clusterGrid.RowTemplate.Height = 25;
        tab.Controls.Add(clusterGrid);
        tab.Controls.Add(clusterLegend);
        tab.Controls.Add(summaryLabel);
        return tab;
    }

    private TabPage CreateHistoryTab()
    {
        var tab = new TabPage("Ход работы")
        {
            Padding = new Padding(4),
            BackColor = Color.White
        };
        iterationHistoryControl = new IterationHistoryControl { Dock = DockStyle.Fill };
        tab.Controls.Add(iterationHistoryControl);
        return tab;
    }

    private static Label CreateCaption(string text)
    {
        return new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = Padding.Empty,
            ForeColor = Color.FromArgb(52, 66, 84)
        };
    }

    private static Button CreateButton(string text, bool primary)
    {
        return new Button
        {
            Text = text,
            FlatStyle = FlatStyle.Flat,
            BackColor = primary ? Color.FromArgb(42, 104, 176) : Color.FromArgb(237, 241, 246),
            ForeColor = primary ? Color.White : Color.FromArgb(42, 55, 72),
            UseVisualStyleBackColor = false
        };
    }
}
