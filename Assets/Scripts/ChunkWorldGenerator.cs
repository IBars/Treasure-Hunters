using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ChunkWorldGenerator : MonoBehaviour
{
    public GameObject grassPrefab;
    public GameObject dirtPrefab;
    public GameObject stonePrefab; // Inspector'dan Stone ekle
    public GameObject cobblePrefab; // Inspector'dan Cobblestone ekle
    public Transform player;
    
    [Header("Dünya Ayarları")]
    public int viewDistance = 2;
    public int chunkSize = 16;
    public float noiseScale = 0.1f;
    public int heightMultiplier = 10; 

    private Dictionary<Vector3Int, GameObject> chunks = new Dictionary<Vector3Int, GameObject>();
    private Vector3Int lastPlayerChunkPos = new Vector3Int(-99, -99, -99);
    private bool isGenerating = false;

    void Update()
    {
        if (player == null) return;

        Vector3Int currentPlayerChunkPos = new Vector3Int(
            Mathf.FloorToInt(player.position.x / chunkSize),
            Mathf.FloorToInt(player.position.y / chunkSize),
            Mathf.FloorToInt(player.position.z / chunkSize)
        );

        if (currentPlayerChunkPos != lastPlayerChunkPos && !isGenerating)
        {
            StartCoroutine(ManageChunksCoroutine(currentPlayerChunkPos));
            lastPlayerChunkPos = currentPlayerChunkPos;
        }
    }

    IEnumerator ManageChunksCoroutine(Vector3Int centerChunk)
    {
        isGenerating = true;
        HashSet<Vector3Int> activeCoords = new HashSet<Vector3Int>();

        for (int x = -viewDistance; x <= viewDistance; x++)
        {
            for (int y = -viewDistance; y <= viewDistance; y++)
            {
                for (int z = -viewDistance; z <= viewDistance; z++)
                {
                    Vector3Int coords = new Vector3Int(centerChunk.x + x, centerChunk.y + y, centerChunk.z + z);
                    activeCoords.Add(coords);

                    if (!chunks.ContainsKey(coords))
                    {
                        yield return StartCoroutine(Create3DChunkCoroutine(coords));
                    }
                    else if (!chunks[coords].activeSelf)
                    {
                        chunks[coords].SetActive(true);
                    }
                }
            }
        }

        List<Vector3Int> keys = new List<Vector3Int>(chunks.Keys);
        foreach (var key in keys)
        {
            if (!activeCoords.Contains(key))
            {
                chunks[key].SetActive(false);
            }
        }

        isGenerating = false;
    }

    IEnumerator Create3DChunkCoroutine(Vector3Int coords)
    {
        GameObject chunkParent = new GameObject($"Chunk_{coords.x}_{coords.y}_{coords.z}");
        chunkParent.transform.parent = this.transform;
        chunks.Add(coords, chunkParent);

        int blocksCreatedThisFrame = 0;
        int blocksPerFrame = 500; 

        for (int x = 0; x < chunkSize; x++)
        {
            for (int z = 0; z < chunkSize; z++)
            {
                float worldX = coords.x * chunkSize + x;
                float worldZ = coords.z * chunkSize + z;
                int surfaceY = Mathf.FloorToInt(Mathf.PerlinNoise(worldX * noiseScale, worldZ * noiseScale) * heightMultiplier);

                for (int y = 0; y < chunkSize; y++)
                {
                    int worldY = coords.y * chunkSize + y;

                    if (worldY <= surfaceY && worldY >= -10)
                    {
                        Vector3 pos = new Vector3(worldX, worldY, worldZ);
                        GameObject prefab;
                        int id;

                        // DERİNLİK MANTIĞI
                        if (worldY == surfaceY) {
                            prefab = grassPrefab;
                            id = 0;
                        }
                        else if (worldY > surfaceY - 3) {
                            prefab = dirtPrefab;
                            id = 1;
                        }
                        else {
                            prefab = stonePrefab; 
                            id = 2;
                        }

                        SpawnBlock(prefab, pos, chunkParent, id);

                        blocksCreatedThisFrame++;
                        if (blocksCreatedThisFrame >= blocksPerFrame)
                        {
                            blocksCreatedThisFrame = 0;
                            yield return null; 
                        }
                    }
                }
            }
        }
    }

    void SpawnBlock(GameObject prefab, Vector3 pos, GameObject parent, int id)
    {
        if (prefab == null) return;
        GameObject blockObj = Instantiate(prefab, pos, Quaternion.identity, parent.transform);
        
        Renderer blockRenderer = blockObj.GetComponentInChildren<Renderer>();
        if (blockRenderer != null)
        {
            if (Vector3.Distance(player.position, pos) > chunkSize * 1.5f) 
            {
                blockRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }
        
        Block b = blockObj.GetComponent<Block>() ?? blockObj.AddComponent<Block>();
        b.blockID = id;
    }

    public void RemoveBlockManually(GameObject block)
    {
        if (block != null) Destroy(block);
    }

    public void RegisterNewBlock(GameObject block, Vector3Int pos)
    {
        Vector3Int chunkCoords = new Vector3Int(
            Mathf.FloorToInt((float)pos.x / chunkSize),
            Mathf.FloorToInt((float)pos.y / chunkSize),
            Mathf.FloorToInt((float)pos.z / chunkSize)
        );

        if (chunks.ContainsKey(chunkCoords))
        {
            block.transform.parent = chunks[chunkCoords].transform;
        }
    }
}