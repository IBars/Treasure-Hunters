using UnityEngine;
using System.Collections.Generic;

public class ChunkWorldGenerator : MonoBehaviour
{
    public GameObject dirtBlockPrefab;   // Alt katman artık DirtBlock
    public GameObject grassBlockPrefab;  // Üst katman
    public Transform player;

    public int chunkSize = 16;
    public int maxHeight = 12;
    public float noiseScale = 0.08f;

    public int viewDistance = 2; // kaç chunk etraf yüklü

    Dictionary<Vector2Int, GameObject> chunks = new Dictionary<Vector2Int, GameObject>();

    void Update()
    {
        UpdateChunks();
    }

    void UpdateChunks()
    {
        int playerChunkX = Mathf.FloorToInt(player.position.x / chunkSize);
        int playerChunkZ = Mathf.FloorToInt(player.position.z / chunkSize);

        for (int x = -viewDistance; x <= viewDistance; x++)
        {
            for (int z = -viewDistance; z <= viewDistance; z++)
            {
                Vector2Int chunkPos = new Vector2Int(playerChunkX + x, playerChunkZ + z);

                if (!chunks.ContainsKey(chunkPos))
                {
                    CreateChunk(chunkPos.x, chunkPos.y);
                }
                else
                {
                    chunks[chunkPos].SetActive(true);
                }
            }
        }

        // UZAK CHUNK'LARI KAPAT
        foreach (var chunk in chunks)
        {
            int distX = Mathf.Abs(chunk.Key.x - playerChunkX);
            int distZ = Mathf.Abs(chunk.Key.y - playerChunkZ);

            if (distX > viewDistance || distZ > viewDistance)
            {
                chunk.Value.SetActive(false);
            }
        }
    }

    void CreateChunk(int chunkX, int chunkZ)
    {
        GameObject chunkObj = new GameObject($"Chunk_{chunkX}_{chunkZ}");
        chunkObj.transform.parent = transform;

        chunks.Add(new Vector2Int(chunkX, chunkZ), chunkObj);

        for (int x = 0; x < chunkSize; x++)
        {
            for (int z = 0; z < chunkSize; z++)
            {
                int worldX = chunkX * chunkSize + x;
                int worldZ = chunkZ * chunkSize + z;

                float noise = Mathf.PerlinNoise(worldX * noiseScale, worldZ * noiseScale);
                int height = Mathf.RoundToInt(noise * maxHeight);

                for (int y = 0; y <= height; y++)
                {
                    Vector3 pos = new Vector3(worldX, y, worldZ);

                    // Alt katman dirt, üst katman grass
                    GameObject prefabToSpawn = (y == height) ? grassBlockPrefab : dirtBlockPrefab;
                    GameObject block = Instantiate(prefabToSpawn, pos, Quaternion.identity, chunkObj.transform);

                    // Üst katmana GrassBlock tag ekle
                    if (y == height)
                    {
                        block.tag = "GrassBlock";
                    }
                }
            }
        }
    }
}
