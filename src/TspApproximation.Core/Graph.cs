namespace TspApproximation.Core;

public sealed class Graph
{
    public Graph(int[,] adjacencyMatrix)
    {
        AdjacencyMatrix = adjacencyMatrix;
    }

    public int VertexCount => AdjacencyMatrix.GetLength(0);

    public int this[int i, int j]
    {
        get => AdjacencyMatrix[i, j];
    }

    private int[,] AdjacencyMatrix
    {
        get;
    }
}