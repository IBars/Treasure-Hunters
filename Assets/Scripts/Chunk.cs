using UnityEngine;
using System.Collections.Generic;

public class Chunk
{
    public Vector3Int coord;
    public GameObject root;
    public Dictionary<Vector3Int, Block> blocks = new Dictionary<Vector3Int, Block>();

    public Chunk(Vector3Int coord, GameObject root)
    {
        this.coord = coord;
        this.root = root;
    }
}
