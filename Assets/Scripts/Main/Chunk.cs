using UnityEngine;
using System.Collections.Generic;

public class Chunk
{
    public Vector3Int coord;
    public GameObject root;
    
    // Eski sistem için (water, dimension block gibi özel bloklar)
    public Dictionary<Vector3Int, Block> blocks = new Dictionary<Vector3Int, Block>();
    
    // Yeni optimized sistem için
    public Dictionary<Vector3Int, OptimizedBlock> optimizedBlocks = new Dictionary<Vector3Int, OptimizedBlock>();
    
    public Chunk(Vector3Int coord, GameObject root)
    {
        this.coord = coord;
        this.root = root;
    }
}