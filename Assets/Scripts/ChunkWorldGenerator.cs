using UnityEngine;
using System.Collections.Generic;

public class ChunkWorldGenerator : MonoBehaviour
{
    public GameObject grassPrefab;
    public GameObject dirtPrefab;
    public Transform player;
    
    [Header("Dünya Ayarları")]
    public int chunkSize = 16;
    public float noiseScale = 0.1f;
    public int heightMultiplier = 10; // Dağların yüksekliği

    private Dictionary<Vector2Int, GameObject> chunks = new Dictionary<Vector2Int, GameObject>();
    private List<GameObject> allBlocks = new List<GameObject>();

    void Update()
    {
        int currentChunkX = Mathf.FloorToInt(player.position.x / chunkSize);
        int currentChunkZ = Mathf.FloorToInt(player.position.z / chunkSize);

        for (int x = -1; x <= 1; x++)
        {
            for (int z = -1; z <= 1; z++)
            {
                Vector2Int chunkPos = new Vector2Int(currentChunkX + x, currentChunkZ + z);
                if (!chunks.ContainsKey(chunkPos))
                {
                    CreateChunk(chunkPos.x, chunkPos.y);
                }
            }
        }
    }

    void CreateChunk(int chunkX, int chunkZ)
    {
        GameObject chunkParent = new GameObject("Chunk_" + chunkX + "_" + chunkZ);
        chunks.Add(new Vector2Int(chunkX, chunkZ), chunkParent);

        for (int x = 0; x < chunkSize; x++)
        {
            for (int z = 0; z < chunkSize; z++)
            {
                float worldX = chunkX * chunkSize + x;
                float worldZ = chunkZ * chunkSize + z;

                // Perlin Noise ile tepe yüksekliğini hesapla (Örn: 5, 8, 2 vb.)
                int surfaceY = Mathf.FloorToInt(Mathf.PerlinNoise(worldX * noiseScale, worldZ * noiseScale) * heightMultiplier);

                // Döngü: En üstteki surfaceY'den başla, -1'e kadar in
                for (int y = surfaceY; y >= -1; y--)
                {
                    Vector3 pos = new Vector3(worldX, y, worldZ);
                    
                    if (y == surfaceY)
                    {
                        // En üst katman her zaman Çim
                        SpawnBlock(grassPrefab, pos, chunkParent, 0);
                    }
                    else
                    {
                        // Aradaki katmanlar -1'e kadar Toprak
                        SpawnBlock(dirtPrefab, pos, chunkParent, 1);
                    }
                }
                // y = -2 ve altı döngüye girmediği için boşluk kalacak
            }
        }
    }

    void SpawnBlock(GameObject prefab, Vector3 pos, GameObject parent, int id)
    {
        GameObject blockObj = Instantiate(prefab, pos, Quaternion.identity, parent.transform);
        Block b = blockObj.GetComponent<Block>() ?? blockObj.AddComponent<Block>();
        b.blockID = id;
        b.blockName = prefab.name;
        allBlocks.Add(blockObj);
    }

    public void RemoveBlockManually(GameObject block)
    {
        if (allBlocks.Contains(block)) allBlocks.Remove(block);
        Destroy(block);
    }

    public void RegisterNewBlock(GameObject block, Vector3Int pos)
    {
        allBlocks.Add(block);
    }
}