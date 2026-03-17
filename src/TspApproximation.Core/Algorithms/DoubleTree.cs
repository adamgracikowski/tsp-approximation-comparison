namespace TspApproximation.Core.Algorithms;

public sealed class DoubleTree : IAlgorithm
{
    public TspOutput Solve(TspInput input)
    {
        var mstResult = input.Graph.Mst();
        var root = mstResult.Root;
        var adjacencyList = mstResult.AdjacencyList;

        List<int> route = new(input.Graph.VertexCount);
        Stack<int> visitStack = new();
        visitStack.Push(root);

        var visited = new bool[input.Graph.VertexCount];
        var lastVertex = -1;
        var distance = 0;

        while (visitStack.Count > 0)
        {
            var v = visitStack.Pop();
            if (!visited[v])
            {
                visited[v] = true;
                route.Add(v);

                if (lastVertex != -1)
                {
                    distance += input.Graph[lastVertex, v];
                }

                lastVertex = v;
                for (int i = adjacencyList[v].Count - 1; i >= 0; i--)
                {
                    int neighbor = adjacencyList[v][i];
                    if (!visited[neighbor])
                    {
                        visitStack.Push(neighbor);
                    }
                }
            }
        }

        route.Add(root);
        distance += input.Graph[lastVertex, root];

        return new TspOutput(route, distance);
    }
}