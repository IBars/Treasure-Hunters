using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ChunkWorldGenerator : MonoBehaviour
{
    public static ChunkWorldGenerator Instance;

    [Header("Block Prefabs")]
    public GameObject grassPrefab;
    public GameObject dirtPrefab;
    public GameObject stonePrefab;
    public GameObject cobblePrefab;

    public Transform player;

    [Header("World Settings")]
    public int chunkSize = 16;
    public int viewDistance = 2;
    public float noiseScale = 0.05f;
    public int heightMultiplier = 15;
    public int baseHeight = 20;

    private Dictionary<Vector3Int, Chunk> chunks = new Dictionary<Vector3Int, Chunk>();
    private Vector3Int lastPlayerChunk = new Vector3Int(999, 0, 999);
    private bool generating = false;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        if (!player || generating) return;

        Vector3Int currentChunk = new Vector3Int(
            Mathf.FloorToInt(player.position.x / chunkSize),
            0,
            Mathf.FloorToInt(player.position.z / chunkSize)
        );

        if (currentChunk != lastPlayerChunk)
        {
            StartCoroutine(ManageChunks(currentChunk));
            lastPlayerChunk = currentChunk;
        }
    }

    IEnumerator ManageChunks(Vector3Int center)
    {
        generating = true;
        HashSet<Vector3Int> needed = new HashSet<Vector3Int>();

        for (int x = -viewDistance; x <= viewDistance; x++)
        {
            for (int z = -viewDistance; z <= viewDistance; z++)
            {
                Vector3Int coord = new Vector3Int(center.x + x, 0, center.z + z);
                needed.Add(coord);

                if (!chunks.ContainsKey(coord))
                    yield return StartCoroutine(CreateChunk(coord));
                else
                    chunks[coord].root.SetActive(true);
            }
        }

        foreach (var pair in chunks)
        {
            if (!needed.Contains(pair.Key))
                pair.Value.root.SetActive(false);
        }

        generating = false;
    }

    IEnumerator CreateChunk(Vector3Int coord)
    {
        GameObject chunkObj = new GameObject($"Chunk_{coord.x}_{coord.z}");
        chunkObj.transform.parent = transform;

        Chunk chunk = new Chunk(coord, chunkObj);
        chunks.Add(coord, chunk);

        int counter = 0;
        int blocksPerFrame = 300;

        for (int x = 0; x < chunkSize; x++)
        {
            for (int z = 0; z < chunkSize; z++)
            {
                int worldX = coord.x * chunkSize + x;
                int worldZ = coord.z * chunkSize + z;

                float noise = SimplexNoise.Noise(worldX * noiseScale, worldZ * noiseScale);
                int surfaceY = Mathf.FloorToInt(noise * heightMultiplier) + baseHeight;

                for (int y = surfaceY; y >= surfaceY - 25; y--)
                {
                    GameObject prefab;
                    int id;

                    if (y == surfaceY) { prefab = grassPrefab; id = 0; }
                    else if (y >= surfaceY - 5) { prefab = dirtPrefab; id = 1; }
                    else { prefab = stonePrefab; id = 2; }

                    Vector3Int pos = new Vector3Int(worldX, y, worldZ);

                    GameObject blockObj = Instantiate(prefab, pos, Quaternion.identity, chunkObj.transform);
                    Block block = blockObj.GetComponent<Block>();
                    block.blockID = id;

                    chunk.blocks[pos] = block;

                    counter++;
                    if (counter >= blocksPerFrame)
                    {
                        counter = 0;
                        yield return null;
                    }
                }
            }
        }
    }

    // =====================
    // DATA & VISIBILITY
    // =====================

    public bool HasBlock(Vector3Int worldPos)
    {
        Vector3Int chunkCoord = new Vector3Int(
            Mathf.FloorToInt((float)worldPos.x / chunkSize),
            0,
            Mathf.FloorToInt((float)worldPos.z / chunkSize)
        );

        if (!chunks.ContainsKey(chunkCoord)) return false;
        return chunks[chunkCoord].blocks.ContainsKey(worldPos);
    }

    public void RemoveBlockManually(GameObject blockObj)
    {
        Vector3Int pos = Vector3Int.RoundToInt(blockObj.transform.position);

        Vector3Int chunkCoord = new Vector3Int(
            Mathf.FloorToInt((float)pos.x / chunkSize),
            0,
            Mathf.FloorToInt((float)pos.z / chunkSize)
        );

        if (chunks.ContainsKey(chunkCoord))
            chunks[chunkCoord].blocks.Remove(pos);

        Destroy(blockObj);
        CheckNeighbors(pos);
    }

    public void RegisterNewBlock(GameObject blockObj, Vector3Int worldPos)
    {
        Vector3Int chunkCoord = new Vector3Int(
            Mathf.FloorToInt((float)worldPos.x / chunkSize),
            0,
            Mathf.FloorToInt((float)worldPos.z / chunkSize)
        );

        if (chunks.ContainsKey(chunkCoord))
        {
            blockObj.transform.parent = chunks[chunkCoord].root.transform;
            Block b = blockObj.GetComponent<Block>();
            chunks[chunkCoord].blocks[worldPos] = b;

            b.CheckVisibility(this);
            CheckNeighbors(worldPos);
        }
    }

    void CheckNeighbors(Vector3Int pos)
    {
        Vector3Int[] dirs =
        {
            Vector3Int.up, Vector3Int.down,
            Vector3Int.left, Vector3Int.right,
            new Vector3Int(0,0,1), new Vector3Int(0,0,-1)
        };

        foreach (var d in dirs)
        {
            Vector3Int nPos = pos + d;

            Vector3Int chunkCoord = new Vector3Int(
                Mathf.FloorToInt((float)nPos.x / chunkSize),
                0,
                Mathf.FloorToInt((float)nPos.z / chunkSize)
            );

            if (chunks.ContainsKey(chunkCoord) &&
                chunks[chunkCoord].blocks.TryGetValue(nPos, out Block b))
            {
                b.CheckVisibility(this);
            }
        }
    }
}
