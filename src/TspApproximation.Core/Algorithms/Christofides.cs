namespace TspApproximation.Core.Algorithms;

public sealed class Christofides : IAlgorithm
{
    /// <summary></summary>
    /// <note>
    /// O(n^4) complexity (dominated by the MWPM step (Edmonds' blossom on the odd-degree subgraph)).
    /// </note>
    public TspOutput Solve(TspInput input)
    {
        var graph = input.Graph;
        var n = graph.VertexCount;

        var mst = graph.Mst();
        List<int> oddVertices = FindOddDegreeVertices(n, mst);
        Graph subGraph = CreateCompleteSubGraph(oddVertices, graph);
        List<(int, int)> matchingOriginal = GenerateMatchingFromSubGraph(subGraph, graph, oddVertices);
        List<List<int>> adjMulti = CreateAdjacencyListFromMstAndMatching(n, mst, matchingOriginal);
        var eulerCircuit = FindEulerCircuit(adjMulti, n);
        (List<int> route, int totalDistance) = BuildRouteFromEulerCircuit(eulerCircuit, graph);
        return new TspOutput(route, totalDistance);
    }

    /// <summary>
    /// Generates a minimum-cost perfect matching from the given subgraph and translates it back to the original graph indices.
    /// </summary>
    /// <param name="subGraph">The subgraph.</param>
    /// <param name="graph">The original graph used to determine edge costs in the subgraph.</param>
    /// <param name="origVertices">Mapping of subGraph vertices to original graph vertices.</param>
    /// <returns>A list of edges representing the matching in terms of the original graph's vertex indices.</returns>
    /// <note>O(k^4) complexity, where k = |origVertices| &le; n (dominated by the MWPM step).</note>
    private static List<(int, int)> GenerateMatchingFromSubGraph(Graph subGraph, Graph graph, List<int> origVertices)
    {
        var matching = new Matching(subGraph);

        // Build cost list for the subgraph edges
        int edgeCount = subGraph.EdgeCount;
        var costs = new List<double>(edgeCount);
        for (int e = 0; e < edgeCount; e++)
        {
            var (lu, lv) = subGraph.GetEdge(e);
            costs.Add(graph[origVertices[lu], origVertices[lv]]);
        }

        var mcpm = matching.SolveMinimumCostPerfectMatching(costs);

        // Translate matched edges back to original vertex indices
        var matchingOriginal = new List<(int, int)>();
        foreach (int e in mcpm.EdgeIndices)
        {
            var (lu, lv) = subGraph.GetEdge(e);
            matchingOriginal.Add((origVertices[lu], origVertices[lv]));
        }

        return matchingOriginal;
    }

    /// <summary>
    /// Creates an adjacency list representing the combined structure of a minimum spanning tree (MST) and a matching.
    /// </summary>
    /// <param name="n">The number of vertices in the graph.</param>
    /// <param name="mst">The minimum spanning tree of the graph, containing edges and adjacency information.</param>
    /// <param name="matching">A list of edges representing the matching.</param>
    /// <returns>An adjacency list where each vertex is associated with its connected vertices in the combined structure.</returns>
    /// <note>O(n) complexity (iterates over O(n) MST edges and O(n) matching edges).</note>
    private static List<List<int>> CreateAdjacencyListFromMstAndMatching(
        int n,
        MstResult mst,
        List<(int, int)> matching)
    {
        var adjMulti = new List<List<int>>(n);
        for (int i = 0; i < n; i++)
        {
            adjMulti.Add([]);
        }

        foreach (var edge in mst.Edges.Where(edge => edge.U < edge.V))
        {
            adjMulti[edge.U].Add(edge.V);
            adjMulti[edge.V].Add(edge.U);
        }

        foreach (var (u, v) in matching)
        {
            adjMulti[u].Add(v);
            adjMulti[v].Add(u);
        }

        return adjMulti;
    }

    /// <summary>
    /// Creates a complete subgraph from the specified subset of vertices in the original graph.
    /// </summary>
    /// <param name="subVertices">The subset of vertices from the original graph to include in the subgraph.</param>
    /// <param name="graph">The original graph used to determine the edge weights in the subgraph.</param>
    /// <returns>A new graph instance representing the complete subgraph, where edge weights are derived from the original graph.</returns>
    /// <note>O(k^2) complexity, where k = |subVertices| &le; n (fills the k&#10799;k adjacency matrix and precomputes edge indices).</note>
    private static Graph CreateCompleteSubGraph(List<int> subVertices, Graph graph)
    {
        int k = subVertices.Count;
        var subMatrix = new int[k, k];
        for (int i = 0; i < k; i++)
        {
            for (int j = 0; j < k; j++)
            {
                subMatrix[i, j] = (i != j) ? graph[subVertices[i], subVertices[j]] : 0;
            }
        }

        var subGraph = new Graph(subMatrix);
        return subGraph;
    }

    /// <summary>
    /// Finds all vertices with an odd degree in the minimum spanning tree (MST) of a graph.
    /// </summary>
    /// <param name="n">The number of vertices in the graph.</param>
    /// <param name="mst">The minimum spanning tree of the graph.</param>
    /// <returns>A list of vertex indices that have an odd degree in the MST.</returns>
    /// <note>O(n) complexity (single pass over n vertices checking adjacency list length).</note>
    private static List<int> FindOddDegreeVertices(int n, MstResult mst)
    {
        var oddVertices = new List<int>();
        for (int v = 0; v < n; v++)
        {
            if (mst.AdjacencyList[v].Count % 2 == 1)
            {
                oddVertices.Add(v);
            }
        }

        return oddVertices;
    }

    /// <summary>
    /// Constructs a Hamiltonian cycle and calculates its total distance using an Euler circuit.
    /// </summary>
    /// <param name="eulerCircuit">A list of vertices representing the Euler circuit.</param>
    /// <param name="graph">The graph used to determine distances between vertices.</param>
    /// <returns>A tuple containing the resulting Hamiltonian route and the total distance of the route.</returns>
    /// <note>O(n) complexity (single pass over the O(n) vertices in the Euler circuit).</note>
    private static (List<int> route, int totalDistance) BuildRouteFromEulerCircuit(List<int> eulerCircuit, Graph graph)
    {
        var visited = new bool[graph.VertexCount];
        var route = new List<int>();
        int totalDistance = 0;
        int prev = -1;

        foreach (var v in eulerCircuit.Where(v => !visited[v]))
        {
            visited[v] = true;
            route.Add(v);
            if (prev != -1)
            {
                totalDistance += graph[prev, v];
            }

            prev = v;
        }

        route.Add(route[0]);
        totalDistance += graph[prev, route[0]];
        return (route, totalDistance);
    }

    /// <summary>
    /// Computes the Eulerian circuit from the given adjacency list of a multigraph.
    /// Assumes the graph is Eulerian and all vertices have even degrees. 
    /// </summary>
    /// <param name="adj">The adjacency list representing the multigraph. Each index corresponds to a vertex, and the list at each index contains adjacent vertices.</param>
    /// <param name="n">The number of vertices in the graph.</param>
    /// <returns>A list of integers representing the sequence of vertex indices in the Eulerian circuit.</returns>
    /// <note>
    /// O(n^2) complexity (Hierholzer's algorithm is O(m*) = O(n),
    /// but each reverse-edge removal via LinkedList.Remove is O(degree) = O(n) in the worst case.
    /// </note>
    private static List<int> FindEulerCircuit(List<List<int>> adj, int n)
    {
        var adjLinks = new LinkedList<int>[n];
        for (int i = 0; i < n; i++)
        {
            adjLinks[i] = new LinkedList<int>(adj[i]);
        }

        var stack = new Stack<int>();
        var circuit = new List<int>();
        stack.Push(0);

        while (stack.Count > 0)
        {
            int u = stack.Peek();
            if (adjLinks[u].Count > 0)
            {
                int v = adjLinks[u].First!.Value;
                adjLinks[u].RemoveFirst();
                adjLinks[v].Remove(u); // Remove the reverse edge
                stack.Push(v);
            }
            else
            {
                circuit.Add(stack.Pop());
            }
        }

        circuit.Reverse();
        return circuit;
    }
}