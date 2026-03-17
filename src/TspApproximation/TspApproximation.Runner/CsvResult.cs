namespace TspApproximation.Runner;

public sealed class CsvResult
{
    public string FileName { get; set; } = string.Empty;

    public string Algorithm { get; set; } = string.Empty;

    public int VertexCount { get; set; }

    public int TotalDistance { get; set; }

    public int RouteLength { get; set; }

    public string Route { get; set; } = string.Empty;

    public double TotalElapsedMiliseconds { get; set; }
}