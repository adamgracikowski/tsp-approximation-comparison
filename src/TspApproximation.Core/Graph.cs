using System.Diagnostics;

namespace TspApproximation.Core;

public sealed class Graph
{
    public Graph(int[,] adjacencyMatrix)
    {
        AdjacencyMatrix = adjacencyMatrix;
    }

    public int EdgeCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < VertexCount; i++)
            {
                for (int j = i + 1; j < VertexCount; j++)
                {
                    if (AdjacencyMatrix[i, j] != 0)
                    {
                        count++;
                    }
                }
            }
            return count;
        }
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

    public List<int> AdjList(int u)
    {
        var list = new List<int>();
        for (int i = 0; i < VertexCount; i++)
        {
            if (AdjacencyMatrix[u, i] != 0)
            {
                list.Add(i);
            }
        }
        return list;
    }

    public int GetEdgeIndex(int u, int v)
    {
        int index = 0;
        for (int i = 0; i < VertexCount; i++)
        {
            for (int j = i + 1; j < VertexCount; j++)
            {
                if (AdjacencyMatrix[i, j] != 0)
                {
                    if ((i == u && j == v) || (i == v && j == u))
                    {
                        return index;
                    }
                    index++;
                }
            }
        }
        return -1;
    }

    public (int, int) GetEdge(int edgeIndex)
    {
        int index = 0;
        for (int i = 0; i < VertexCount; i++)
        {
            for (int j = i + 1; j < VertexCount; j++)
            {
                if (AdjacencyMatrix[i, j] != 0)
                {
                    if (index == edgeIndex)
                    {
                        return (i, j);
                    }
                    index++;
                }
            }
        }
        return (-1, -1);
    }
}