namespace Lr4KMeansClustering.Controls;

internal static class ColorPalette
{
    private static readonly Color[] Colors =
    {
        Color.FromArgb(38, 108, 181),
        Color.FromArgb(220, 72, 64),
        Color.FromArgb(43, 145, 92),
        Color.FromArgb(151, 88, 191),
        Color.FromArgb(227, 139, 42),
        Color.FromArgb(20, 148, 160),
        Color.FromArgb(196, 74, 135),
        Color.FromArgb(105, 121, 44),
        Color.FromArgb(92, 99, 112),
        Color.FromArgb(125, 82, 56)
    };

    public static Color ForCluster(int clusterIndex)
    {
        return Colors[Math.Abs(clusterIndex) % Colors.Length];
    }

}
