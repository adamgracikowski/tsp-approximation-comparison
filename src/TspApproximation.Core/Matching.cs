namespace TspApproximation.Core;

internal enum VertexLabel
{
    Unlabeled = 0,
    Odd = 1,
    Even = 2,
}

public sealed class Matching
{
    //
    // Graph & Matching Basics
    //

    private readonly Graph _g;

    /// <summary>
    /// Number of edges of the graph.
    /// </summary>
    private readonly int _m;

    /// <summary>
    /// Number of vertices of the graph.
    /// </summary>
    private readonly int _n;

    /// <summary>
    /// mate[u] stores the index of the vertex matched to u, or -1 if unmatched.
    /// </summary>
    private readonly List<int> _mate;

    //
    // Blossom Structure Variables
    //

    /// <summary>
    /// Maps any vertex or sub-blossom to the highest-level blossom currently containing it.
    /// outer[i] == i when the vertex is not inside any blossom.
    /// </summary>
    private readonly List<int> _outer;

    /// <summary>
    /// For each blossom B, the immediate nodes (vertices or smaller blossoms) that form its odd cycle.
    /// </summary>
    private readonly List<List<int>> _shallow;

    /// <summary>
    /// For each blossom B, every original graph vertex residing anywhere inside it.
    /// </summary>
    private readonly List<List<int>> _deep;

    /// <summary>
    /// The base vertex of the blossom i.e., the one connected to the rest of the alternating tree.
    /// </summary>
    private readonly List<int> _tip;

    /// <summary>
    /// True when a blossom index is currently in use (shrunk and not yet expanded).
    /// </summary>
    private readonly List<bool> _active;

    /// <summary>
    /// Available indices (n..2n-1) that can be assigned to new blossoms.
    /// </summary>
    private List<int> _free = [];

    //
    // Alternating Forest Variables
    //

    /// <summary>
    /// Label assigned during BFS.
    /// </summary>
    private readonly List<VertexLabel> _type;

    /// <summary>
    /// forest[v] = u means vertex v was reached from u in the alternating tree.
    /// </summary>
    private readonly List<int> _forest;

    /// <summary>
    /// Stores the unmatched root vertex at the base of the current alternating tree.
    /// </summary>
    private readonly List<int> _root;

    /// <summary>
    /// BFS queue of EVEN vertices whose neighbours have not yet been explored.
    /// </summary>
    private readonly LinkedList<int> _forestList = new();

    /// <summary>
    /// Ensures each vertex is processed at most once per Grow() step.
    /// </summary>
    private bool[] _visited;

    //
    // Weighted Matching (Dual) Variables
    //

    /// <summary>
    /// Dual variable (price) for each vertex and blossom.
    /// </summary>
    private readonly List<double> _dual;

    /// <summary>
    /// Reduced cost of each edge. An edge can only enter the matching when its slack == 0.
    /// </summary>
    private readonly List<double> _slack;

    /// <summary>
    /// A blocked blossom cannot be expanded because its dual value is still positive.
    /// </summary>
    private readonly List<bool> _blocked;

    /// <summary>
    /// Whether the current matching is perfect.
    /// </summary>
    private bool _perfect;

    public Matching(Graph g)
    {
        _g = g;
        _n = _g.VertexCount;
        _m = _g.EdgeCount;

        _outer = new List<int>(new int[2 * _n]);
        _deep = new List<List<int>>(2 * _n);
        _shallow = new List<List<int>>(2 * _n);
        for (int i = 0; i < 2 * _n; i++)
        {
            _deep.Add([]);
            _shallow.Add([]);
        }

        _tip = new List<int>(new int[2 * _n]);
        _active = new List<bool>(new bool[2 * _n]);
        _type = new List<VertexLabel>(new VertexLabel[2 * _n]);
        _forest = new List<int>(new int[2 * _n]);
        _root = new List<int>(new int[2 * _n]);
        _blocked = new List<bool>(new bool[2 * _n]);
        _dual = new List<double>(new double[2 * _n]);
        _slack = new List<double>(new double[_m]);
        _mate = new List<int>(new int[2 * _n]);
        _visited = new bool[2 * _n];
    }

    public void Grow()
    {
        Reset();

        while (_forestList.Count > 0)
        {
            int w = _outer[_forestList.First!.Value];
            _forestList.RemoveFirst();

            foreach (int u in _deep[w])
            {
                bool cont = false;
                foreach (int v in _g.AdjList(u))
                {
                    if (IsEdgeBlocked(u, v))
                    {
                        continue;
                    }

                    if (_type[_outer[v]] == VertexLabel.Odd)
                    {
                        continue;
                    }

                    if (_type[_outer[v]] != VertexLabel.Even)
                    {
                        // CASE 1: v is unlabeled; grow the alternating forest
                        int vm = _mate[_outer[v]];

                        _forest[_outer[v]] = u;
                        _type[_outer[v]] = VertexLabel.Odd;
                        _root[_outer[v]] = _root[_outer[u]];
                        _forest[_outer[vm]] = v;
                        _type[_outer[vm]] = VertexLabel.Even;
                        _root[_outer[vm]] = _root[_outer[u]];

                        if (!_visited[_outer[vm]])
                        {
                            _forestList.AddLast(vm);
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
                        _forestList.AddFirst(b);
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

        _perfect = true;
        for (int i = 0; i < _n; i++)
        {
            if (_mate[_outer[i]] == -1)
            {
                _perfect = false;
            }
        }
    }

    private bool IsAdjacent(int u, int v) => _g[u, v] != 0 && !IsEdgeBlocked(u, v);

    private bool IsEdgeBlocked(int u, int v) => _slack[_g.GetEdgeIndex(u, v)] > 0;

    private bool IsEdgeBlocked(int e) => _slack[e] > 0;

    public void Heuristic()
    {
        var degree = new int[_n];
        var b = new BinaryHeap();

        for (int i = 0; i < _m; i++)
        {
            if (IsEdgeBlocked(i))
            {
                continue;
            }

            var (u, v) = _g.GetEdge(i);
            degree[u]++;
            degree[v]++;
        }

        for (int i = 0; i < _n; i++)
        {
            b.Insert(degree[i], i);
        }

        while (b.Size() > 0)
        {
            int u = b.DeleteMin();
            if (_mate[_outer[u]] == -1)
            {
                int min = -1;
                foreach (int v in _g.AdjList(u))
                {
                    if (IsEdgeBlocked(u, v) || _outer[u] == _outer[v] || _mate[_outer[v]] != -1)
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
                    _mate[_outer[u]] = min;
                    _mate[_outer[min]] = u;
                }
            }
        }
    }

    private void DestroyBlossom(int t)
    {
        if ((t < _n) || (_blocked[t] && _dual[t] > 0))
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
        _mate[t] = -1;
    }

    private void Expand(int u, bool expandBlocked = false)
    {
        int v = _outer[_mate[u]];
        int index = _m;
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

        _mate[u] = q;
        _mate[v] = p;

        if (u < _n || (_blocked[u] && !expandBlocked))
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
        _mate[shallow[0]] = _mate[u];
        for (int i = 1; i + 1 < shallow.Count; i += 2)
        {
            _mate[shallow[i]] = shallow[i + 1];
            _mate[shallow[i + 1]] = shallow[i];
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

    private void Augment(int u, int v)
    {
        int p = _outer[u];
        int q = _outer[v];
        int outv = q;

        int fp = _forest[p];
        _mate[p] = q;
        _mate[q] = p;
        Expand(p);
        Expand(q);

        while (fp != -1)
        {
            q = _outer[_forest[p]];
            p = _outer[_forest[q]];
            fp = _forest[p];

            _mate[p] = q;
            _mate[q] = p;
            Expand(p);
            Expand(q);
        }

        p = outv;
        fp = _forest[p];
        while (fp != -1)
        {
            q = _outer[_forest[p]];
            p = _outer[_forest[q]];
            fp = _forest[p];
            _mate[p] = q;
            _mate[q] = p;
            Expand(p);
            Expand(q);
        }
    }

    private void Reset()
    {
        for (int i = 0; i < 2 * _n; i++)
        {
            _forest[i] = -1;
            _root[i] = i;
            if (i >= _n && _active[i] && _outer[i] == i)
            {
                DestroyBlossom(i);
            }
        }

        Array.Clear(_visited, 0, 2 * _n);
        _forestList.Clear();

        for (int i = 0; i < _n; i++)
        {
            if (_mate[_outer[i]] == -1)
            {
                _type[_outer[i]] = VertexLabel.Even;
                if (!_visited[_outer[i]])
                {
                    _forestList.AddLast(i);
                }

                _visited[_outer[i]] = true;
            }
            else
            {
                _type[_outer[i]] = VertexLabel.Unlabeled;
            }
        }
    }

    private int GetFreeBlossomIndex()
    {
        int i = _free.Last();
        _free.RemoveAt(_free.Count - 1);
        return i;
    }

    private void AddFreeBlossomIndex(int i) => _free.Add(i);

    private void ClearBlossomIndices()
    {
        _free.Clear();
        for (int i = _n; i < 2 * _n; i++)
        {
            AddFreeBlossomIndex(i);
        }
    }

    /// <summary>
    /// Contracts the blossom w, ..., u, v, ..., w, where w is the LCA of u and v in the alternating forest.
    /// Both passed vertices are EVEN.
    /// </summary>
    private int Blossom(int u, int v)
    {
        int t = GetFreeBlossomIndex();
        var isInPath = new bool[2 * _n];

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
        _type[t] = VertexLabel.Even;
        _root[t] = _root[_tip[t]];
        _active[t] = true;
        _outer[t] = t;
        _mate[t] = _mate[_tip[t]];

        return t;
    }

    private void UpdateDualCosts()
    {
        double e1 = 0, e2 = 0, e3 = 0;
        bool inite1 = false, inite2 = false, inite3 = false;

        for (int i = 0; i < _m; i++)
        {
            var (u, v) = _g.GetEdge(i);

            if ((_type[_outer[u]] == VertexLabel.Even && _type[_outer[v]] == VertexLabel.Unlabeled) ||
                (_type[_outer[v]] == VertexLabel.Even && _type[_outer[u]] == VertexLabel.Unlabeled))
            {
                if (!inite1 || e1 > _slack[i])
                {
                    e1 = _slack[i];
                    inite1 = true;
                }
            }
            else if (_outer[u] != _outer[v] &&
                _type[_outer[u]] == VertexLabel.Even &&
                _type[_outer[v]] == VertexLabel.Even)
            {
                if (!inite2 || e2 > _slack[i])
                {
                    e2 = _slack[i];
                    inite2 = true;
                }
            }
        }

        for (int i = _n; i < 2 * _n; i++)
        {
            if (_active[i] && i == _outer[i] && _type[_outer[i]] == VertexLabel.Odd &&
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

        for (int i = 0; i < 2 * _n; i++)
        {
            if (i != _outer[i])
            {
                continue;
            }

            if (_active[i] && _type[_outer[i]] == VertexLabel.Even)
            {
                _dual[i] += e;
            }
            else if (_active[i] && _type[_outer[i]] == VertexLabel.Odd)
            {
                _dual[i] -= e;
            }
        }

        for (int i = 0; i < _m; i++)
        {
            var (u, v) = _g.GetEdge(i);
            if (_outer[u] == _outer[v])
            {
                continue;
            }

            if (_type[_outer[u]] == VertexLabel.Even && _type[_outer[v]] == VertexLabel.Even)
            {
                _slack[i] -= 2.0 * e;
            }
            else if (_type[_outer[u]] == VertexLabel.Odd && _type[_outer[v]] == VertexLabel.Odd)
            {
                _slack[i] += 2.0 * e;
            }
            else if ((_type[_outer[v]] == VertexLabel.Unlabeled && _type[_outer[u]] == VertexLabel.Even) ||
                (_type[_outer[u]] == VertexLabel.Unlabeled && _type[_outer[v]] == VertexLabel.Even))
            {
                _slack[i] -= e;
            }
            else if ((_type[_outer[v]] == VertexLabel.Unlabeled && _type[_outer[u]] == VertexLabel.Odd) ||
                (_type[_outer[u]] == VertexLabel.Unlabeled && _type[_outer[v]] == VertexLabel.Odd))
            {
                _slack[i] += e;
            }
        }

        for (int i = _n; i < 2 * _n; i++)
        {
            if (_dual[i] > 0)
            {
                _blocked[i] = true;
            }
            else if (_active[i] && _blocked[i])
            {
                if (_mate[i] == -1)
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

    public (List<int>, double) SolveMinimumCostPerfectMatching(List<double> cost)
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
        double obj = 0;
        foreach (int it in matching)
        {
            obj += cost[it];
        }

        return (matching, obj);
    }

    private void PositiveCosts()
    {
        double minEdge = 0;
        for (int i = 0; i < _m; i++)
        {
            if (minEdge - _slack[i] > 0)
            {
                minEdge = _slack[i];
            }
        }

        for (int i = 0; i < _m; i++)
        {
            _slack[i] -= minEdge;
        }
    }

    public List<int> SolveMaximumMatching()
    {
        Clear();
        Grow();
        return RetrieveMatching();
    }

    private void Clear()
    {
        ClearBlossomIndices();
        for (int i = 0; i < 2 * _n; i++)
        {
            _outer[i] = i;
            _deep[i].Clear();
            if (i < _n)
            {
                _deep[i].Add(i);
            }

            _shallow[i].Clear();
            _active[i] = i < _n;
            _type[i] = VertexLabel.Unlabeled;
            _forest[i] = -1;
            _root[i] = i;
            _blocked[i] = false;
            _dual[i] = 0;
            _mate[i] = -1;
            _tip[i] = i;
        }

        for (int i = 0; i < _m; i++)
        {
            _slack[i] = 0;
        }
    }

    private List<int> RetrieveMatching()
    {
        var matching = new List<int>();
        for (int i = 0; i < 2 * _n; i++)
        {
            if (_active[i] && _mate[i] != -1 && _outer[i] == i)
            {
                Expand(i, true);
            }
        }

        for (int i = 0; i < _m; i++)
        {
            var (u, v) = _g.GetEdge(i);
            if (_mate[u] == v)
            {
                matching.Add(i);
            }
        }

        return matching;
    }
}