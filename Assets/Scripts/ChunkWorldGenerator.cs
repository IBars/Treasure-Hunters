using UnityEngine;
using System.Collections.Generic;

public class ChunkWorldGenerator : MonoBehaviour
{
    public GameObject dirtBlockPrefab;
    public GameObject grassBlockPrefab;
    public Transform player;

    public int chunkSize = 16;
    public int maxHeight = 12;
    public float noiseScale = 0.08f;
    public int viewDistance = 2;
    
    // İstediğin 5 blokluk mesafe
    public float visibilityRadius = 5f; 

    Dictionary<Vector2Int, GameObject> chunks = new Dictionary<Vector2Int, GameObject>();
    // Blokları ve rendererlarını eşleştiren liste
    Dictionary<Vector2Int, List<MeshRenderer>> dirtRenderers = new Dictionary<Vector2Int, List<MeshRenderer>>();

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
                    CreateChunk(chunkPos.x, chunkPos.y);
                
                // Her aktif chunk içindeki blokların mesafesini kontrol et
                UpdateBlockVisibility(chunkPos);
                
                chunks[chunkPos].SetActive(true);
            }
        }

        // Uzak chunkları kapatma
        foreach (var chunk in chunks)
        {
            if (Mathf.Abs(chunk.Key.x - playerChunkX) > viewDistance || Mathf.Abs(chunk.Key.y - playerChunkZ) > viewDistance)
            {
                chunk.Value.SetActive(false);
            }
        }
    }

    void CreateChunk(int chunkX, int chunkZ)
    {
        Vector2Int chunkKey = new Vector2Int(chunkX, chunkZ);
        GameObject chunkObj = new GameObject($"Chunk_{chunkX}_{chunkZ}");
        chunkObj.transform.parent = transform;
        chunks.Add(chunkKey, chunkObj);
        dirtRenderers.Add(chunkKey, new List<MeshRenderer>());

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
                    GameObject prefabToSpawn = (y == height) ? grassBlockPrefab : dirtBlockPrefab;
                    GameObject block = Instantiate(prefabToSpawn, pos, Quaternion.identity, chunkObj.transform);

                    if (y != height) // Sadece dirt blokları için
                    {
                        MeshRenderer mr = block.GetComponent<MeshRenderer>();
                        if (mr != null)
                        {
                            mr.enabled = false;
                            dirtRenderers[chunkKey].Add(mr);
                        }
                    }
                }
            }
        }
    }

    // Blok bazlı mesafe kontrolü
    void UpdateBlockVisibility(Vector2Int chunkKey)
    {
        if (dirtRenderers.ContainsKey(chunkKey))
        {
            foreach (MeshRenderer mr in dirtRenderers[chunkKey])
            {
                if (mr == null) continue;

                // Bloğun oyuncuya olan uzaklığını hesapla
                float distance = Vector3.Distance(player.position, mr.transform.position);

                // 5 bloktan yakınsa aç, değilse kapat
                if (distance <= visibilityRadius)
                {
                    if (!mr.enabled) mr.enabled = true;
                }
                else
                {
                    if (mr.enabled) mr.enabled = false;
                }
            }
        }
    }
}