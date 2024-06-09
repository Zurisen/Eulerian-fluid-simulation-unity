using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class SpatialHash
{
    private float _cellsize;
    private Dictionary<Vector2Int, List<int>> _hashTable;

    public SpatialHash(float cellsize)
    {
        this._cellsize = cellsize;
        this._hashTable = new Dictionary<Vector2Int, List<int>>();
    }

    public void Clear()
    {
        _hashTable.Clear();
    }

    public Vector2Int Hash(Vector2 position)
    {
        return new Vector2Int(
            Mathf.FloorToInt(position.x / _cellsize),
            Mathf.FloorToInt(position.y / _cellsize)
        );
    }

    public void Insert(int index, Vector2 position)
    {
        Vector2Int socket = Hash(position);
        if (!_hashTable.ContainsKey(socket))
        {
            _hashTable[socket] = new List<int>();
        }

        _hashTable[socket].Add(index);
    }

    public List<int> GetNeighbors(Vector2 position, float searchRadius)
    {
        List<int> neighbors = new List<int>();
        int searchRange = Mathf.CeilToInt(searchRadius / _cellsize);

        Vector2Int originSocket = Hash(position);
        for (int x = -searchRange; x <= searchRange; x++)
        {
            for (int y = -searchRange; y <= searchRange; y++)
            {
                Vector2Int neighborHash = new Vector2Int(originSocket.x + x, originSocket.y + y);
                if (_hashTable.ContainsKey(neighborHash))
                {
                    neighbors.AddRange(_hashTable[neighborHash]);
                }
            }
        }

        return neighbors;
    }
}


