namespace TspApproximation.Core.Algorithms;

public sealed record Edge(
    int U,
    int V,
    int Weight
);

public sealed record MstResult(
    int Root,
    IReadOnlyList<IReadOnlyList<int>> AdjacencyList,
    IReadOnlyList<Edge> Edges,
    int TotalWeight
);

public static class MstExtensions
{
    public static MstResult Mst(this Graph graph)
    {
        PriorityQueue<Edge, int> priorityQueue = new();
        List<List<int>> adjacencyList = new(graph.VertexCount);
        for (var i = 0; i < graph.VertexCount; i++)
        {
            adjacencyList.Add(new List<int>());
        }

        List<Edge> edges = new();
        var visited = new bool[graph.VertexCount];
        var weight = 0;

        // Start with vertex 0
        ProcessVertex(0, graph, priorityQueue, visited);

        while (priorityQueue.Count > 0 && edges.Count < graph.VertexCount - 1)
        {
            var edge = priorityQueue.Dequeue();
            if (visited[edge.V])
            {
                continue;
            }

            adjacencyList[edge.U].Add(edge.V);
            adjacencyList[edge.V].Add(edge.U);
            edges.Add(edge);
            weight += edge.Weight;
            ProcessVertex(edge.V, graph, priorityQueue, visited);
        }

        return new MstResult(0, adjacencyList, edges, weight);
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