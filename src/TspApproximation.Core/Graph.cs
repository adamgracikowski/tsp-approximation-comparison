namespace TspApproximation.Core;

public sealed class Graph
{
    public Graph(int[,] adjacencyMatrix)
    {
        _adjacencyMatrix = adjacencyMatrix;
    }

    public int VertexCount => _adjacencyMatrix.GetLength(0);

    public int this[int i, int j]
    {
        get => _adjacencyMatrix[i, j];
    }

    private int[,] _adjacencyMatrix { get; }
}