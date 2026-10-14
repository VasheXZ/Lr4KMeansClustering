using System.Drawing.Drawing2D;
using System.ComponentModel;
using Lr6KMeansClustering.Models;

namespace Lr6KMeansClustering.Controls;

// Интерактивная координатная плоскость и состояние выбранной итерации
public sealed class CoordinatePlane : Control
{
    private const double MinimumCoordinate = -10;
    private const double MaximumCoordinate = 10;
    private const int PlotPadding = 44;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public IReadOnlyList<Point2D> Points { get; set; } = Array.Empty<Point2D>();

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public IterationSnapshot? Snapshot { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool ShowLabels { get; set; } = true;

    public event Action<Point2D, MouseButtons>? PointClicked;
    public event Action<Point2D?>? CoordinatesChanged;

    public CoordinatePlane()
    {
        DoubleBuffered = true;
        BackColor = Color.White;
        Cursor = Cursors.Cross;
        SetStyle(ControlStyles.ResizeRedraw, true);
    }

    public PointF ToScreen(Point2D point)
    {
        RectangleF plot = GetPlotRectangle();
        float x = plot.Left + (float)((point.X - MinimumCoordinate)
            / (MaximumCoordinate - MinimumCoordinate) * plot.Width);
        float y = plot.Bottom - (float)((point.Y - MinimumCoordinate)
            / (MaximumCoordinate - MinimumCoordinate) * plot.Height);
        return new PointF(x, y);
    }

    public Point2D ToWorld(PointF point)
    {
        RectangleF plot = GetPlotRectangle();
        double x = MinimumCoordinate
            + (point.X - plot.Left) / plot.Width * (MaximumCoordinate - MinimumCoordinate);
        double y = MinimumCoordinate
            + (plot.Bottom - point.Y) / plot.Height * (MaximumCoordinate - MinimumCoordinate);
        return new Point2D(
            Math.Clamp(x, MinimumCoordinate, MaximumCoordinate),
            Math.Clamp(y, MinimumCoordinate, MaximumCoordinate));
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        DrawBackground(e.Graphics);
        DrawPoints(e.Graphics);
        DrawCenters(e.Graphics);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        CoordinatesChanged?.Invoke(GetPlotRectangle().Contains(e.Location) ? ToWorld(e.Location) : null);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        CoordinatesChanged?.Invoke(null);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (GetPlotRectangle().Contains(e.Location))
        {
            PointClicked?.Invoke(ToWorld(e.Location), e.Button);
        }
    }

    private RectangleF GetPlotRectangle()
    {
        float width = Math.Max(1, ClientSize.Width - 2 * PlotPadding);
        float height = Math.Max(1, ClientSize.Height - 2 * PlotPadding);
        float side = Math.Min(width, height);
        float left = (ClientSize.Width - side) / 2f;
        float top = (ClientSize.Height - side) / 2f;
        return new RectangleF(left, top, side, side);
    }

    private void DrawBackground(Graphics graphics)
    {
        RectangleF plot = GetPlotRectangle();
        using var background = new SolidBrush(Color.FromArgb(250, 252, 255));
        using var gridPen = new Pen(Color.FromArgb(226, 232, 240), 1);
        using var majorGridPen = new Pen(Color.FromArgb(205, 214, 226), 1);
        using var axisPen = new Pen(Color.FromArgb(70, 82, 98), 1.7f);
        using var borderPen = new Pen(Color.FromArgb(173, 184, 198), 1);
        using var labelBrush = new SolidBrush(Color.FromArgb(75, 87, 104));

        graphics.FillRectangle(background, plot);

        for (int value = -10; value <= 10; value++)
        {
            PointF verticalStart = ToScreen(new Point2D(value, MinimumCoordinate));
            PointF verticalEnd = ToScreen(new Point2D(value, MaximumCoordinate));
            PointF horizontalStart = ToScreen(new Point2D(MinimumCoordinate, value));
            PointF horizontalEnd = ToScreen(new Point2D(MaximumCoordinate, value));
            Pen pen = value % 5 == 0 ? majorGridPen : gridPen;
            graphics.DrawLine(pen, verticalStart, verticalEnd);
            graphics.DrawLine(pen, horizontalStart, horizontalEnd);
        }

        graphics.DrawLine(
            axisPen,
            ToScreen(new Point2D(MinimumCoordinate, 0)),
            ToScreen(new Point2D(MaximumCoordinate, 0)));
        graphics.DrawLine(
            axisPen,
            ToScreen(new Point2D(0, MinimumCoordinate)),
            ToScreen(new Point2D(0, MaximumCoordinate)));
        graphics.DrawRectangle(borderPen, Rectangle.Round(plot));

        for (int value = -10; value <= 10; value += 5)
        {
            if (value == 0)
            {
                continue;
            }

            PointF xPoint = ToScreen(new Point2D(value, 0));
            PointF yPoint = ToScreen(new Point2D(0, value));
            string text = value.ToString();
            SizeF textSize = graphics.MeasureString(text, Font);

            float xLabelX = Math.Clamp(
                xPoint.X - textSize.Width / 2,
                plot.Left + 2,
                plot.Right - textSize.Width - 2);
            float xLabelY = xPoint.Y + 4;
            graphics.DrawString(text, Font, labelBrush, xLabelX, xLabelY);

            float yLabelX = yPoint.X + 5;
            float yLabelY = Math.Clamp(
                yPoint.Y - textSize.Height / 2,
                plot.Top + 2,
                plot.Bottom - textSize.Height - 2);
            graphics.DrawString(text, Font, labelBrush, yLabelX, yLabelY);
        }

        PointF origin = ToScreen(new Point2D(0, 0));
        SizeF x1Size = graphics.MeasureString("X1", Font);
        SizeF x2Size = graphics.MeasureString("X2", Font);
        graphics.DrawString(
            "X1",
            Font,
            labelBrush,
            plot.Right + 7,
            origin.Y - x1Size.Height - 3);
        graphics.DrawString(
            "X2",
            Font,
            labelBrush,
            origin.X + 7,
            plot.Top - x2Size.Height - 3);
    }

    private void DrawPoints(Graphics graphics)
    {
        IReadOnlyList<int>? assignments = Snapshot is { IsInitial: false }
            ? Snapshot.Assignments
            : null;

        for (int index = 0; index < Points.Count; index++)
        {
            PointF screen = ToScreen(Points[index]);
            Color color = assignments is not null && index < assignments.Count
                ? ColorPalette.ForCluster(assignments[index])
                : Color.FromArgb(86, 101, 120);

            using var brush = new SolidBrush(color);
            using var outline = new Pen(Color.White, 2);
            graphics.FillEllipse(brush, screen.X - 6, screen.Y - 6, 12, 12);
            graphics.DrawEllipse(outline, screen.X - 6, screen.Y - 6, 12, 12);

            if (ShowLabels)
            {
                using var labelBrush = new SolidBrush(Color.FromArgb(33, 42, 55));
                graphics.DrawString($"x{index + 1}", Font, labelBrush, screen.X + 7, screen.Y - 17);
            }
        }
    }

    private void DrawCenters(Graphics graphics)
    {
        if (Snapshot is null)
        {
            return;
        }

        IReadOnlyList<Point2D> centers = Snapshot.CentersAfter;
        for (int index = 0; index < centers.Count; index++)
        {
            PointF screen = ToScreen(centers[index]);
            Color color = ColorPalette.ForCluster(index);
            using var fill = new SolidBrush(Color.White);
            using var pen = new Pen(color, 3);
            graphics.FillEllipse(fill, screen.X - 9, screen.Y - 9, 18, 18);
            graphics.DrawLine(pen, screen.X - 7, screen.Y, screen.X + 7, screen.Y);
            graphics.DrawLine(pen, screen.X, screen.Y - 7, screen.X, screen.Y + 7);
            graphics.DrawEllipse(pen, screen.X - 9, screen.Y - 9, 18, 18);

            if (ShowLabels)
            {
                using var labelBrush = new SolidBrush(color);
                using var labelFont = new Font(Font, FontStyle.Bold);
                graphics.DrawString($"z{index + 1}", labelFont, labelBrush, screen.X + 10, screen.Y + 3);
            }
        }
    }
}
