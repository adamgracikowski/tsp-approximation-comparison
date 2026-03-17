namespace TspApproximation.Core.Algorithms;

public sealed record Edge(
    int U, 
    int V, 
    int Weight
);

public sealed record MstResult(
    IReadOnlyList<IReadOnlyList<int>> AdjacencyList,
    IReadOnlyList<Edge> Edges,
    int TotalWeight
);

public static class MstExtensions
{
    public static MstResult Mst(this Graph graph)
    {
        throw new NotFiniteNumberException("MST algorithm is not implemented yet.");
    }
}