namespace TspApproximation.Core;

public sealed class Graph
{
    private readonly int[,] _adjacencyMatrix;

    /// <summary>
    /// edgeIndex[u, v] gives the index of edge (u,v), or -1 if not present.
    /// </summary>
    private readonly int[,] _edgeIndex;

    /// <summary>
    /// edges[i] gives the (u, v) endpoints of edge i (u &lt; v).
    /// </summary>
    private readonly (int U, int V)[] _edges;

    /// <summary>
    /// Precomputed adjacency lists; O(1) access.
    /// </summary>
    private readonly IReadOnlyList<int>[] _adjList;

    public Graph(int[,] adjacencyMatrix, bool precomputeAdjLists = true)
    {
        _adjacencyMatrix = adjacencyMatrix;
        var n = adjacencyMatrix.GetLength(0);

        if (!precomputeAdjLists)
        {
            _edgeIndex = new int[0, 0];
            _edges = [];
            _adjList = [];
            return;
        }

        _edgeIndex = new int[n, n];
        ResetEdgeIndex();
        _edges = InitializeEdgeIndex().ToArray();
        _adjList = new IReadOnlyList<int>[n];
        InitializeAdjList();

        return;

        List<(int, int)> InitializeEdgeIndex()
        {
            var valueTuples = new List<(int, int)>();
            for (int i = 0; i < n; i++)
            {
                for (int j = i + 1; j < n; j++)
                {
                    if (adjacencyMatrix[i, j] != 0)
                    {
                        int idx = valueTuples.Count;
                        _edgeIndex[i, j] = idx;
                        _edgeIndex[j, i] = idx;
                        valueTuples.Add((i, j));
                    }
                }
            }

            return valueTuples;
        }

        void ResetEdgeIndex()
        {
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    _edgeIndex[i, j] = -1;
                }
            }
        }

        void InitializeAdjList()
        {
            for (int u = 0; u < n; u++)
            {
                var neighbors = new List<int>();
                for (int v = 0; v < n; v++)
                {
                    if (adjacencyMatrix[u, v] != 0)
                    {
                        neighbors.Add(v);
                    }
                }

                _adjList[u] = neighbors;
            }
        }
    }

    /// <summary>
    /// Number of vertices.
    /// </summary>
    public int VertexCount => _adjacencyMatrix.GetLength(0);

    /// <summary>
    /// Number of edges.
    /// </summary>
    public int EdgeCount => _edges.Length;

    /// <summary>
    /// Edge weight between vertices i and j.
    /// </summary>
    public int this[int i, int j] => _adjacencyMatrix[i, j];

    /// <summary>
    /// Returns the adjacency list of vertex u. O(1).
    /// </summary>
    public IReadOnlyList<int> AdjList(int u) => _adjList[u];

    /// <summary>
    /// Returns the index of edge (u, v), or -1 if absent. O(1).
    /// </summary>
    public int GetEdgeIndex(int u, int v) => _edgeIndex[u, v];

    /// <summary>
    /// Returns the endpoints of edge i as (u, v) with u &lt; v. O(1).
    /// </summary>
    public (int, int) GetEdge(int edgeIndex) => _edges[edgeIndex];
}