using System.Runtime.InteropServices;

namespace TspApproximation.Core;

public sealed class Matching
{
    /// <summary>
    /// True when a blossom index is currently in use (shrunk and not yet expanded).
    /// </summary>
    private readonly List<bool> _active;

    /// <summary>
    /// For each blossom B, whether it is blocked from expansion. 
    /// A blocked blossom cannot be expanded because its dual value is still positive.
    /// </summary>
    /// <note>Indexed by blossom id, i.e., indexes n...2*n-1.
    /// The first n ones (corresponding to normal vertices) are not used. </note>
    private readonly List<bool> _blocked;

    /// <summary>
    /// For each blossom B, every original graph vertex residing anywhere inside it.
    /// </summary>
    private readonly List<List<int>> _deep;

    //
    // Weighted Matching (Dual) Variables
    //

    /// <summary>
    /// Dual variable (price) for each vertex and blossom.
    /// </summary>
    private readonly List<double> _dual;

    /// <summary>
    /// Stores the parent of each vertex in the alternating forest during BFS exploration.
    /// forest[v] = u indicates that vertex v was reached from vertex u (u is the parent of v).
    /// </summary>
    private readonly List<int> _forest;

    /// <summary>
    /// BFS queue of EVEN vertices whose neighbors have not yet been explored.
    /// </summary>
    private readonly LinkedList<int> _forestQueue = [];

    /// <summary>
    /// Available indices (n...2n-1) that can be assigned to new blossoms.
    /// </summary>
    private readonly List<int> _free = [];

    private readonly Graph _g;

    /// <summary>
    /// mate[u] stores the index of the vertex matched to u, or -1 if unmatched.
    /// </summary>
    private readonly List<int> _matchedWith;

    /// <summary>
    /// Maps any vertex or sub-blossom to the highest-level blossom currently containing it.
    /// outer[i] == i when the vertex is not inside any blossom.
    /// </summary>
    private readonly List<int> _outer;

    /// <summary>
    /// Stores the unmatched root vertex at the base of the current alternating tree.
    /// </summary>
    private readonly List<int> _root;

    /// <summary>
    /// For each blossom B, the immediate nodes (vertices or smaller blossoms) that form its odd cycle.
    /// </summary>
    private readonly List<List<int>> _shallow;

    /// <summary>
    /// Reduced cost of each edge. An edge can only enter the matching when its slack == 0.
    /// </summary>
    private readonly List<double> _slack;

    /// <summary>
    /// The base vertex of the blossom i.e., the one connected to the rest of the alternating tree.
    /// </summary>
    private readonly List<int> _tip;

    /// <summary>
    /// Label assigned during BFS.
    /// </summary>
    private readonly List<VertexLabel> _vertexLabel;

    /// <summary>
    /// Ensures each vertex is processed at most once per Grow() step.
    /// </summary>
    private readonly List<bool> _visited;

    /// <summary>
    /// Whether the current matching is perfect.
    /// </summary>
    private bool _perfect;

    public Matching(Graph g)
    {
        _g = g;

        _outer = new List<int>(new int[2 * N]);
        _deep = new List<List<int>>(2 * N);
        _shallow = new List<List<int>>(2 * N);
        for (int i = 0; i < 2 * N; i++)
        {
            _deep.Add([]);
            _shallow.Add([]);
        }

        _tip = new List<int>(new int[2 * N]);
        _active = new List<bool>(new bool[2 * N]);
        _vertexLabel = new List<VertexLabel>(new VertexLabel[2 * N]);
        _forest = new List<int>(new int[2 * N]);
        _root = new List<int>(new int[2 * N]);
        _blocked = new List<bool>(new bool[2 * N]);
        _dual = new List<double>(new double[2 * N]);
        _slack = new List<double>(new double[M]);
        _matchedWith = new List<int>(new int[2 * N]);
        _visited = new List<bool>(new bool[2 * N]);
    }

    /// <summary>
    /// Number of edges of the graph.
    /// </summary>
    private int M
    {
        get => _g.EdgeCount;
    }

    /// <summary>
    /// Number of vertices of the graph.
    /// </summary>
    private int N
    {
        get => _g.VertexCount;
    }

    /// <summary>
    /// Computes the maximum matching for the graph associated with this instance. 
    /// </summary>
    /// <returns>
    /// A list of edge indices of the maximum matching in the graph.
    /// </returns>
    public List<int> SolveMaximumMatching()
    {
        Clear();
        Grow();
        return RetrieveMatching();
    }

    /// <summary>
    /// Calculates the minimum cost perfect matching for a graph given a list of edge costs.
    /// </summary>
    /// <param name="cost">
    /// A list of edge costs associated with the graph. 
    /// </param>
    /// <returns>
    /// A struct containing the indices of the edges in the resulting minimum cost perfect matching
    /// and the associated total cost.
    /// </returns>
    public MinimumCostPerfectMatchingResult SolveMinimumCostPerfectMatching(List<double> cost)
    {
        SolveMaximumMatching();
        if (!_perfect)
        {
            throw new Exception("The graph does not have a perfect matching");
        }

        Clear();
        for (int i = 0; i < cost.Count; i++)
        {
            _slack[i] = cost[i];
        }

        PositiveCosts();

        _perfect = false;
        while (!_perfect)
        {
            Heuristic();
            Grow();
            UpdateDualCosts();
            Reset();
        }

        var matching = RetrieveMatching();
        double obj = matching.Sum(it => cost[it]);
        return new MinimumCostPerfectMatchingResult { EdgeIndices = matching, Cost = obj };
    }

    private void AddFreeBlossomIndex(int i) => _free.Add(i);

    /// <summary>
    /// Augments the current matching along the path connecting the roots of two alternating trees,
    /// using the edge (u, v) as the bridge between them.
    /// </summary>
    /// <param name="u">A vertex in one alternating tree, endpoint of the bridging edge.</param>
    /// <param name="v">A vertex in the other alternating tree, endpoint of the bridging edge.</param>
    private void Augment(int u, int v)
    {
        int p = _outer[u];
        int q = _outer[v];
        _matchedWith[p] = q;
        _matchedWith[q] = p;
        Expand(p);
        Expand(q);
        AugmentPath(p);
        AugmentPath(q);
    }

    /// <summary>
    /// Walks up one arm of the augmenting path from <paramref name="start"/> to its tree root,
    /// reversing the matching along the way.
    /// </summary>
    private void AugmentPath(int start)
    {
        int p = start;
        int fp = _forest[p];
        while (fp != -1)
        {
            int q = _outer[_forest[p]];
            p = _outer[_forest[q]];
            fp = _forest[p];
            _matchedWith[p] = q;
            _matchedWith[q] = p;
            Expand(p);
            Expand(q);
        }
    }

    /// <summary>
    /// Contracts the blossom w, ..., u, v, ..., w, where w is the LCA of u and v in the alternating forest.
    /// Both passed vertices are EVEN.
    /// </summary>
    private int Blossom(int u, int v)
    {
        int t = GetFreeBlossomIndex();
        var isInPath = new bool[2 * N];

        int pathWalker = u;
        while (pathWalker != -1)
        {
            isInPath[_outer[pathWalker]] = true;
            pathWalker = _forest[_outer[pathWalker]];
        }

        int tip = _outer[v];
        while (!isInPath[tip])
        {
            tip = _outer[_forest[tip]];
        }

        _tip[t] = tip;

        var circuit = new List<int>();
        pathWalker = _outer[u];
        circuit.Insert(0, pathWalker);
        while (pathWalker != _tip[t])
        {
            pathWalker = _outer[_forest[pathWalker]];
            circuit.Insert(0, pathWalker);
        }

        _shallow[t].Clear();
        _deep[t].Clear();
        _shallow[t].AddRange(circuit);

        pathWalker = _outer[v];
        while (pathWalker != _tip[t])
        {
            _shallow[t].Add(pathWalker);
            pathWalker = _outer[_forest[pathWalker]];
        }

        foreach (int shallowVertex in _shallow[t])
        {
            _outer[shallowVertex] = t;
            foreach (int deepVertex in _deep[shallowVertex])
            {
                _deep[t].Add(deepVertex);
                _outer[deepVertex] = t;
            }
        }

        _forest[t] = _forest[_tip[t]];
        _vertexLabel[t] = VertexLabel.Even;
        _root[t] = _root[_tip[t]];
        _active[t] = true;
        _outer[t] = t;
        _matchedWith[t] = _matchedWith[_tip[t]];

        return t;
    }

    private bool CheckPerfectMatching()
    {
        for (int i = 0; i < N; i++)
        {
            if (_matchedWith[_outer[i]] == -1)
            {
                return false;
            }
        }
        return true;
    }

    private void Clear()
    {
        ClearBlossomIndices();
        for (int i = 0; i < 2 * N; i++)
        {
            _outer[i] = i;
            _deep[i].Clear();
            if (i < N)
            {
                _deep[i].Add(i);
            }

            _shallow[i].Clear();
            _active[i] = i < N;
            _vertexLabel[i] = VertexLabel.Unlabeled;
            _forest[i] = -1;
            _root[i] = i;
            _blocked[i] = false;
            _dual[i] = 0;
            _matchedWith[i] = -1;
            _tip[i] = i;
        }

        for (int i = 0; i < M; i++)
        {
            _slack[i] = 0;
        }
    }

    private void ClearBlossomIndices()
    {
        _free.Clear();
        for (int i = N; i < 2 * N; i++)
        {
            AddFreeBlossomIndex(i);
        }
    }

    /// <summary>
    /// Destroys the specified blossom, releasing all vertices it contains and restoring their
    /// original outer representation. Updates the state of the blossom to inactive, unblocks it if needed,
    /// and marks it as free for reuse.
    /// </summary>
    /// <param name="t">The index of the blossom to be destroyed. If the index corresponds to a standard vertex
    /// or the blossom remains blocked due to its positive dual value, no action is performed.</param>
    private void DestroyBlossom(int t)
    {
        if ((t < N) || (_blocked[t] && _dual[t] > 0))
        {
            return;
        }

        foreach (int s in _shallow[t])
        {
            _outer[s] = s;
            foreach (int jt in _deep[s])
            {
                _outer[jt] = s;
            }

            DestroyBlossom(s);
        }

        _active[t] = false;
        _blocked[t] = false;
        AddFreeBlossomIndex(t);
        _matchedWith[t] = -1;
    }

    /// <summary>
    /// Expands an inactive blossom, restoring its original components as individual vertices
    /// and pairing the vertices of the odd cycle. This operation updates the matching structure
    /// and prepares the blossom for reuse. Optionally, blocked blossoms can also be expanded
    /// based on the provided parameter.
    /// </summary>
    /// <param name="u">The index of the blossom to be expanded. If the blossom is a standard vertex
    /// or remains blocked and <paramref name="expandBlocked"/> is false, no action is performed.</param>
    /// <param name="expandBlocked">Indicates whether blocked blossoms should be expanded.
    /// Defaults to false, meaning blocked blossoms retain their state unless explicitly expanded.</param>
    private void Expand(int u, bool expandBlocked = false)
    {
        int v = _outer[_matchedWith[u]];
        int index = M;
        int p = -1, q = -1;

        foreach (int di in _deep[u])
        {
            foreach (int dj in _deep[v])
            {
                if (IsAdjacent(di, dj) && _g.GetEdgeIndex(di, dj) < index)
                {
                    index = _g.GetEdgeIndex(di, dj);
                    p = di;
                    q = dj;
                }
            }
        }

        _matchedWith[u] = q;
        _matchedWith[v] = p;

        if (u < N || (_blocked[u] && !expandBlocked))
        {
            return;
        }

        // Rotate shallow[u] so that the element containing p comes first
        bool found = false;
        var shallowUList = new LinkedList<int>(_shallow[u]);
        var currentNode = shallowUList.First;
        while (currentNode != null && !found)
        {
            foreach (var jt in _deep[currentNode.Value])
            {
                if (jt == p)
                {
                    found = true;
                    break;
                }
            }

            var nextNode = currentNode.Next;
            if (!found)
            {
                shallowUList.Remove(currentNode);
                shallowUList.AddLast(currentNode);
            }

            currentNode = nextNode;
        }

        _shallow[u] = shallowUList.ToList();

        // Pair the vertices of the odd cycle:
        // shallow[0] is the tip — its mate was already set above.
        // The remaining elements form pairs: (1,2), (3,4), ...
        var shallow = _shallow[u];
        _matchedWith[shallow[0]] = _matchedWith[u];
        for (int i = 1; i + 1 < shallow.Count; i += 2)
        {
            _matchedWith[shallow[i]] = shallow[i + 1];
            _matchedWith[shallow[i + 1]] = shallow[i];
        }

        foreach (int s in _shallow[u])
        {
            _outer[s] = s;
            foreach (int jt in _deep[s])
            {
                _outer[jt] = s;
            }
        }

        _active[u] = false;
        AddFreeBlossomIndex(u);

        foreach (int s in _shallow[u])
        {
            Expand(s, expandBlocked);
        }
    }

    private int GetFreeBlossomIndex()
    {
        int i = _free.Last();
        _free.RemoveAt(_free.Count - 1);
        return i;
    }

    private void Grow()
    {
        Reset();
        
        while (_forestQueue.Count > 0)
        {
            int w = _outer[_forestQueue.First!.Value];
            _forestQueue.RemoveFirst();

            foreach (int u in _deep[w])
            {
                bool cont = false;
                foreach (int v in _g.AdjList(u))
                {
                    if (IsEdgeBlocked(u, v))
                    {
                        continue;
                    }

                    if (_vertexLabel[_outer[v]] == VertexLabel.Odd)
                    {
                        continue;
                    }

                    if (_vertexLabel[_outer[v]] != VertexLabel.Even)
                    {
                        // CASE 1: v is unlabeled; grow the alternating forest
                        int vm = _matchedWith[_outer[v]];

                        // update the outer v vertex
                        _forest[_outer[v]] = u;
                        _vertexLabel[_outer[v]] = VertexLabel.Odd;
                        _root[_outer[v]] = _root[_outer[u]];
                        
                        // update the outer vm vertex i.e., the v's matched partner
                        _forest[_outer[vm]] = v;
                        _vertexLabel[_outer[vm]] = VertexLabel.Even;
                        _root[_outer[vm]] = _root[_outer[u]];

                        if (!_visited[_outer[vm]]) // next consider outer if not already considered
                        {
                            _forestQueue.AddLast(vm);
                            _visited[_outer[vm]] = true;
                        }
                    }
                    else if (_root[_outer[v]] != _root[_outer[u]])
                    {
                        // CASE 2: augmenting path found (u and v from different trees)
                        Augment(u, v);
                        Reset();
                        cont = true;
                        break;
                    }
                    else if (_outer[u] != _outer[v])
                    {
                        // CASE 3: odd cycle; contract into a blossom, explore it immediately
                        int b = Blossom(u, v);
                        _forestQueue.AddFirst(b);
                        _visited[b] = true;
                        cont = true;
                        break;
                    }
                }

                if (cont)
                {
                    break;
                }
            }
        }

        _perfect = CheckPerfectMatching();
    }

    private void Heuristic()
    {
        var degree = new int[N];
        var b = new BinaryHeap();

        for (int i = 0; i < M; i++)
        {
            if (IsEdgeBlocked(i))
            {
                continue;
            }

            var (u, v) = _g.GetEdge(i);
            degree[u]++;
            degree[v]++;
        }

        for (int i = 0; i < N; i++)
        {
            b.Insert(degree[i], i);
        }

        while (b.Size() > 0)
        {
            int u = b.DeleteMin();
            if (_matchedWith[_outer[u]] == -1)
            {
                int min = -1;
                foreach (int v in _g.AdjList(u))
                {
                    if (IsEdgeBlocked(u, v) || _outer[u] == _outer[v] || _matchedWith[_outer[v]] != -1)
                    {
                        continue;
                    }

                    if (min == -1 || degree[v] < degree[min])
                    {
                        min = v;
                    }
                }

                if (min != -1)
                {
                    _matchedWith[_outer[u]] = min;
                    _matchedWith[_outer[min]] = u;
                }
            }
        }
    }

    private bool IsAdjacent(int u, int v) => _g[u, v] != 0 && !IsEdgeBlocked(u, v);

    private bool IsEdgeBlocked(int u, int v) => _slack[_g.GetEdgeIndex(u, v)] > 0;

    private bool IsEdgeBlocked(int e) => _slack[e] > 0;

    private void PositiveCosts()
    {
        double minEdge = 0;
        for (int i = 0; i < M; i++)
        {
            if (minEdge - _slack[i] > 0)
            {
                minEdge = _slack[i];
            }
        }

        for (int i = 0; i < M; i++)
        {
            _slack[i] -= minEdge;
        }
    }

    /// <summary>
    /// Resets the internal state of the matching algorithm to prepare for a new search iteration.
    /// Clears the forest structure, marks all vertices as unvisited, resets tree roots,
    /// and destroys any active blossoms that are no longer part of the current search.
    /// Initializes the forest with unmatched vertices for further exploration.
    /// </summary>
    private void Reset()
    {
        for (int i = 0; i < 2 * N; i++)
        {
            _forest[i] = -1;
            _root[i] = i;
            if (i >= N && _active[i] && _outer[i] == i)
            {
                DestroyBlossom(i);
            }
        }
        
        CollectionsMarshal.AsSpan(_visited)[..(2 * N)].Clear();
        _forestQueue.Clear();
        for (int i = 0; i < N; i++)
        {
            if (_matchedWith[_outer[i]] == -1)
            {
                _vertexLabel[_outer[i]] = VertexLabel.Even;
                if (!_visited[_outer[i]])
                {
                    _forestQueue.AddLast(i);
                }

                _visited[_outer[i]] = true;
            }
            else
            {
                _vertexLabel[_outer[i]] = VertexLabel.Unlabeled;
            }
        }
    }

    /// <summary>
    /// Retrieves the current matching as a list of edge indices. Each edge in the resulting list corresponds
    /// to a pair of matched vertices in the graph. Only active blossoms and their contained vertices are considered
    /// during the matching retrieval process.
    /// </summary>
    /// <returns>
    /// A list of integers representing the edge indices of the matching in the graph.
    /// </returns>
    private List<int> RetrieveMatching()
    {
        var matching = new List<int>();
        for (int i = 0; i < 2 * N; i++)
        {
            if (_active[i] && _matchedWith[i] != -1 && _outer[i] == i)
            {
                Expand(i, true);
            }
        }

        for (int i = 0; i < M; i++)
        {
            var (u, v) = _g.GetEdge(i);
            if (_matchedWith[u] == v)
            {
                matching.Add(i);
            }
        }

        return matching;
    }

    private void UpdateDualCosts()
    {
        double e1 = 0, e2 = 0, e3 = 0;
        bool inite1 = false, inite2 = false, inite3 = false;

        for (int i = 0; i < M; i++)
        {
            var (u, v) = _g.GetEdge(i);

            if ((_vertexLabel[_outer[u]] == VertexLabel.Even && _vertexLabel[_outer[v]] == VertexLabel.Unlabeled) ||
                (_vertexLabel[_outer[v]] == VertexLabel.Even && _vertexLabel[_outer[u]] == VertexLabel.Unlabeled))
            {
                if (!inite1 || e1 > _slack[i])
                {
                    e1 = _slack[i];
                    inite1 = true;
                }
            }
            else if (_outer[u] != _outer[v] &&
                _vertexLabel[_outer[u]] == VertexLabel.Even &&
                _vertexLabel[_outer[v]] == VertexLabel.Even)
            {
                if (!inite2 || e2 > _slack[i])
                {
                    e2 = _slack[i];
                    inite2 = true;
                }
            }
        }

        for (int i = N; i < 2 * N; i++)
        {
            if (_active[i] && i == _outer[i] && _vertexLabel[_outer[i]] == VertexLabel.Odd &&
                (!inite3 || e3 > _dual[i]))
            {
                e3 = _dual[i];
                inite3 = true;
            }
        }

        double e = 0;
        if (inite1)
        {
            e = e1;
        }
        else if (inite2)
        {
            e = e2;
        }
        else if (inite3)
        {
            e = e3;
        }

        if (e > e2 / 2.0 && inite2)
        {
            e = e2 / 2.0;
        }

        if (e > e3 && inite3)
        {
            e = e3;
        }

        for (int i = 0; i < 2 * N; i++)
        {
            if (i != _outer[i])
            {
                continue;
            }

            if (_active[i] && _vertexLabel[_outer[i]] == VertexLabel.Even)
            {
                _dual[i] += e;
            }
            else if (_active[i] && _vertexLabel[_outer[i]] == VertexLabel.Odd)
            {
                _dual[i] -= e;
            }
        }

        for (int i = 0; i < M; i++)
        {
            var (u, v) = _g.GetEdge(i);
            if (_outer[u] == _outer[v])
            {
                continue;
            }

            if (_vertexLabel[_outer[u]] == VertexLabel.Even && _vertexLabel[_outer[v]] == VertexLabel.Even)
            {
                _slack[i] -= 2.0 * e;
            }
            else if (_vertexLabel[_outer[u]] == VertexLabel.Odd && _vertexLabel[_outer[v]] == VertexLabel.Odd)
            {
                _slack[i] += 2.0 * e;
            }
            else if ((_vertexLabel[_outer[v]] == VertexLabel.Unlabeled && _vertexLabel[_outer[u]] == VertexLabel.Even) ||
                (_vertexLabel[_outer[u]] == VertexLabel.Unlabeled && _vertexLabel[_outer[v]] == VertexLabel.Even))
            {
                _slack[i] -= e;
            }
            else if ((_vertexLabel[_outer[v]] == VertexLabel.Unlabeled && _vertexLabel[_outer[u]] == VertexLabel.Odd) ||
                (_vertexLabel[_outer[u]] == VertexLabel.Unlabeled && _vertexLabel[_outer[v]] == VertexLabel.Odd))
            {
                _slack[i] += e;
            }
        }

        for (int i = N; i < 2 * N; i++)
        {
            if (_dual[i] > 0)
            {
                _blocked[i] = true;
            }
            else if (_active[i] && _blocked[i])
            {
                if (_matchedWith[i] == -1)
                {
                    DestroyBlossom(i);
                }
                else
                {
                    _blocked[i] = false;
                    Expand(i);
                }
            }
        }
    }

    public struct MinimumCostPerfectMatchingResult
    {
        public double Cost;
        public List<int> EdgeIndices;
    }
}

/// <summary>
/// Represents the label assigned to a vertex during the breadth-first search (BFS)
/// in the context of an alternating forest used in graph matching algorithms.
/// </summary>
internal enum VertexLabel
{
    /// <summary>The vertex has not yet been visited or labeled during the current BFS phase. </summary>
    Unlabeled = 0,

    /// <summary>The vertex is at an odd distance from the root of its alternating tree in the forest. </summary>
    Odd = 1,

    /// <summary>The vertex is at an even distance from the root of its alternating tree in the forest. </summary>
    Even = 2,
}

