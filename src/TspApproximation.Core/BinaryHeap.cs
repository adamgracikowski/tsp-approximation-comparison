namespace TspApproximation.Core;

/// <summary>
/// Min-heap for (double key, int satellite) pairs.
/// Satellites are assumed to be unique non-negative integers (vertex or edge indices).
/// Supports O(log n) insert, delete-min, and key change via direct satellite lookup.
/// </summary>
public class BinaryHeap
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
        _satellite = [0]; // position 0 unused; heap is 1-based
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

        int i;
        for (i = _size; i / 2 > 0 && _key[_satellite[i / 2]] > k; i /= 2)
        {
            _satellite[i] = _satellite[i / 2];
            _pos[_satellite[i]] = i;
        }

        _satellite[i] = s;
        _pos[s] = i;
        _key[s] = k;
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
        int slast = _satellite[_size--];

        int child;
        int i;
        for (i = 1, child = 2; child <= _size; i = child, child *= 2)
        {
            if (child < _size && _key[_satellite[child]] > _key[_satellite[child + 1]])
            {
                child++;
            }

            if (_key[slast] > _key[_satellite[child]])
            {
                _satellite[i] = _satellite[child];
                _pos[_satellite[child]] = i;
            }
            else
            {
                break;
            }
        }

        _satellite[i] = slast;
        _pos[slast] = i;
        _pos[min] = -1;

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
        int i;
        for (i = _pos[s]; i / 2 > 0; i /= 2)
        {
            _satellite[i] = _satellite[i / 2];
            _pos[_satellite[i]] = i;
        }

        _satellite[1] = s;
        _pos[s] = 1;

        DeleteMin();
    }
}