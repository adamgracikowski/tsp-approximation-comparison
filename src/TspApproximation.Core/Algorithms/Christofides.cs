namespace TspApproximation.Core.Algorithms;

public sealed class Christofides : IAlgorithm
{
    public TspOutput Solve(TspInput input)
    {
        var graph = input.Graph;
        var n = graph.VertexCount;

        // Step 1: compute MST
        var mst = graph.Mst();

        // Step 2: find odd-degree vertices in the MST
        var degree = new int[n];
        for (int v = 0; v < n; v++)
        {
            degree[v] = mst.AdjacencyList[v].Count;
        }

        var oddVertices = new List<int>();
        for (int v = 0; v < n; v++)
        {
            if (degree[v] % 2 == 1)
            {
                oddVertices.Add(v);
            }
        }

        // Step 3: build complete subgraph on odd-degree vertices and find MWPM
        int k = oddVertices.Count;
        var subMatrix = new int[k, k];
        for (int i = 0; i < k; i++)
        {
            for (int j = 0; j < k; j++)
            {
                subMatrix[i, j] = (i != j) ? graph[oddVertices[i], oddVertices[j]] : 0;
            }
        }

        var subGraph = new Graph(subMatrix);
        var matching = new Matching(subGraph);

        // Build cost list for the subgraph edges
        int edgeCount = subGraph.EdgeCount;
        var costs = new List<double>(edgeCount);
        for (int e = 0; e < edgeCount; e++)
        {
            var (lu, lv) = subGraph.GetEdge(e);
            costs.Add(graph[oddVertices[lu], oddVertices[lv]]);
        }

        var (matchedEdges, _) = matching.SolveMinimumCostPerfectMatching(costs);

        // Translate matched edges back to original vertex indices
        var matchingOriginal = new List<(int, int)>();
        foreach (int e in matchedEdges)
        {
            var (lu, lv) = subGraph.GetEdge(e);
            matchingOriginal.Add((oddVertices[lu], oddVertices[lv]));
        }

        // Step 4: build multigraph adjacency list (MST + matching edges)
        var adjMulti = new List<List<int>>(n);
        for (int i = 0; i < n; i++)
        {
            adjMulti.Add(new List<int>());
        }

        foreach (var edge in mst.Edges)
        {
            if (edge.U < edge.V)
            {
                adjMulti[edge.U].Add(edge.V);
                adjMulti[edge.V].Add(edge.U);
            }
        }

        foreach (var (u, v) in matchingOriginal)
        {
            adjMulti[u].Add(v);
            adjMulti[v].Add(u);
        }

        // Step 5: find Euler circuit using Hierholzer's algorithm
        var eulerCircuit = FindEulerCircuit(adjMulti, n);

        // Step 6: shortcut to Hamiltonian cycle
        var visited = new bool[n];
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

        return new TspOutput(route, totalDistance);
    }

    private static List<int> FindEulerCircuit(List<List<int>> adj, int n)
    {
        // Work on mutable copies (linked lists for O(1) removal)
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
                // Remove the reverse edge
                adjLinks[v].Remove(u);
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