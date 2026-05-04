namespace TspApproximation.Core;

/// <summary>
/// Min-heap for (double key, int satellite) pairs.
/// Satellites are assumed to be unique non-negative integers (vertex or edge indices).
/// Supports O(log n) insert, delete-min, and key change via direct satellite lookup.
/// </summary>
public sealed class BinaryHeap
{
    /// <summary>
    /// key[s] is the priority of satellite s.
    /// </summary>
    private List<double> _key;

    /// <summary>
    /// pos[s] is the 1-based position of satellite s in the heap array, or -1 if absent.
    /// </summary>
    private List<int> _pos;

    /// <summary>
    /// The heap array (1-based). satellite[i] is the satellite stored at heap position i.
    /// </summary>
    private List<int> _satellite;

    /// <summary>
    /// Number of elements currently in the heap.
    /// </summary>
    private int _size;

    public BinaryHeap()
    {
        _satellite = [0]; // position 0 unused
        _key = new List<double>();
        _pos = new List<int>();
        _size = 0;
    }

    /// <summary>
    /// Returns the number of elements in the heap.
    /// </summary>
    public int Size() => _size;

    /// <summary>
    /// Removes all elements and resets the structure.
    /// </summary>
    public void Clear()
    {
        _key.Clear();
        _pos.Clear();
        _satellite.Clear();
        _satellite.Add(0);
        _size = 0;
    }

    /// <summary>
    /// Inserts satellite s with priority k.
    /// </summary>
    public void Insert(double k, int s)
    {
        while (s >= _pos.Count)
        {
            _pos.Add(-1);
        }

        while (s >= _key.Count)
        {
            _key.Add(0);
        }

        if (_pos[s] != -1)
        {
            throw new InvalidOperationException("Error: satellite already in heap");
        }

        _size++;
        while (_size >= _satellite.Count)
        {
            _satellite.Add(0);
        }

        _key[s] = k;
        Place(s, _size);
        BubbleUp(s);
    }

    /// <summary>
    /// Removes and returns the satellite with the minimum key.
    /// </summary>
    public int DeleteMin()
    {
        if (_size == 0)
        {
            throw new InvalidOperationException("Error: empty heap");
        }

        int min = _satellite[1];
        int slast = _satellite[_size];
        _size--;
        _pos[min] = -1;

        if (_size > 0)
        {
            Place(slast, 1);
            BubbleDown(slast);
        }

        return min;
    }

    /// <summary>
    /// Updates the key of satellite s to k.
    /// </summary>
    public void ChangeKey(double k, int s)
    {
        Remove(s);
        Insert(k, s);
    }

    /// <summary>
    /// Removes satellite s from the heap.
    /// </summary>
    public void Remove(int s)
    {
        int i = _pos[s];
        int slast = _satellite[_size--];
        _pos[s] = -1;

        if (slast == s)
        {
            return;
        }

        Place(slast, i);
        BubbleUp(slast);
        BubbleDown(slast);
    }

    /// <summary>
    /// Places satellite s at heap position i, updating _satellite and _pos.
    /// </summary>
    private void Place(int s, int i)
    {
        _satellite[i] = s;
        _pos[s] = i;
    }

    /// <summary>
    /// Restores the min-heap property by moving the satellite s upward in the heap if its key is smaller than the key of its parent.
    /// </summary>
    /// <param name="s">The satellite to be moved upward in the heap.</param>
    private void BubbleUp(int s)
    {
        int i = _pos[s];
        while (i / 2 > 0 && _key[_satellite[i / 2]] > _key[s])
        {
            Place(_satellite[i / 2], i);
            i /= 2;
        }

        Place(s, i);
    }

    /// <summary>
    /// Restores the min-heap invariant by moving the satellite down the heap if its key is greater than its children's keys.
    /// </summary>
    /// <param name="s">The satellite to be moved down the heap.</param>
    private void BubbleDown(int s)
    {
        int i = _pos[s];
        while (true)
        {
            int child = i * 2;
            if (child > _size)
            {
                break;
            }

            if (child < _size && _key[_satellite[child]] > _key[_satellite[child + 1]])
            {
                child++;
            }

            if (_key[s] <= _key[_satellite[child]])
            {
                break;
            }

            Place(_satellite[child], i);
            i = child;
        }

        Place(s, i);
    }
}