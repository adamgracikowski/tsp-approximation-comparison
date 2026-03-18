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
        var n = graph.VertexCount;
        var edgesLimit = n * 2 - 2;
        PriorityQueue<Edge, int> priorityQueue = new();
        List<List<int>> adjacencyList = new(n);
        for (var i = 0; i < n; i++)
        {
            adjacencyList.Add(new List<int>());
        }

        List<Edge> edges = new();
        var visited = new bool[n];
        var weight = 0;

        ProcessVertex(0, graph, priorityQueue, visited);

        while (priorityQueue.Count > 0 && edges.Count < edgesLimit)
        {
            var edge = priorityQueue.Dequeue();
            if (visited[edge.V])
            {
                continue;
            }

            adjacencyList[edge.U].Add(edge.V);
            adjacencyList[edge.V].Add(edge.U);
            edges.Add(edge);
            edges.Add(new Edge(edge.V, edge.U, edge.Weight));
            weight += edge.Weight;
            ProcessVertex(edge.V, graph, priorityQueue, visited);
        }

        return new MstResult(adjacencyList, edges, weight);
    }

    private static void ProcessVertex(int v, Graph graph, PriorityQueue<Edge, int> priorityQueue, bool[] visited)
    {
        visited[v] = true;
        for (var i = 0; i < graph.VertexCount; i++)
        {
            if (visited[i])
            {
                continue;
            }

            priorityQueue.Enqueue(new Edge(v, i, graph[v, i]), graph[v, i]);
        }
    }
}