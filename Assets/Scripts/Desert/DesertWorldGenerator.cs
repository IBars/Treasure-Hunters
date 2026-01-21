using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DesertWorldGenerator : MonoBehaviour
{
    public static GameObject InstanceGameObject;
    public static DesertWorldGenerator Instance;

    void Awake()
    {
        Instance = this;
        InstanceGameObject = this.gameObject;
    }

    [Header("Desert Block Prefabs")]
    public GameObject sandPrefab;           // Kum blok
    public GameObject sandstonePrefab;      // Kumtaşı (Sand altında)
    public GameObject cactusPrefab;         // Kaktüs
    public GameObject stonePrefab;    // Çöl taşı
    public GameObject dimensionBlockPrefab;  
    public GameObject leafPrefab; 
    public GameObject grassPrefab;
    public GameObject dirtPrefab;
    public GameObject cobblePrefab;
    public GameObject logPrefab; 

    [Header("World Settings")]
    public Transform player;
    public int chunkSize = 16;
    public int viewDistance = 2;
    public float noiseScale = 0.04f;        // Daha düz arazi için küçük değer
    public int heightMultiplier = 8;        // Çölde düz arazi
    public int baseHeight = 20;

    [Header("Desert Features")]
    [Range(0, 100)]
    public float cactusChance = 1.5f;       // Kaktüs oluşma şansı
    public int minCactusHeight = 2;
    public int maxCactusHeight = 5;

    [Header("Performance")]
    public int columnsPerFrame = 4;

    private Dictionary<Vector3Int, Chunk> chunks = new Dictionary<Vector3Int, Chunk>();
    private Vector3Int lastPlayerChunk = new Vector3Int(999, 0, 999);
    private bool generating = false;

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
            if (!needed.Contains(pair.Key))
                pair.Value.root.SetActive(false);

        generating = false;
    }

    IEnumerator CreateChunk(Vector3Int coord)
    {
        GameObject chunkObj = new GameObject($"DesertChunk_{coord.x}_{coord.z}");
        chunkObj.transform.parent = transform;
        Chunk chunk = new Chunk(coord, chunkObj);
        chunks.Add(coord, chunk);

        for (int x = 0; x < chunkSize; x++)
        {
            for (int z = 0; z < chunkSize; z++)
            {
                int worldX = coord.x * chunkSize + x;
                int worldZ = coord.z * chunkSize + z;
                
                // Çöl için özel noise hesaplama
                float noise = SimplexNoise.Noise(worldX * noiseScale, worldZ * noiseScale);
                int surfaceY = Mathf.FloorToInt(noise * heightMultiplier) + baseHeight;

                // Çöl arazisi oluştur
                for (int y = surfaceY; y > surfaceY - 7; y--)
                {
                    GameObject prefab = null;
                    Quaternion rotation = Quaternion.identity;

                    if (y == surfaceY)
                    {
                        // Yüzey katmanı - Kum
                        prefab = sandPrefab;
                        
                        // Kaktüs oluşturma şansı
                        if (Random.Range(0f, 100f) < cactusChance)
                        {
                            GenerateCactus(new Vector3Int(worldX, y + 1, worldZ), chunkObj.transform, chunk);
                        }
                    }
                    else if (y > surfaceY - 4)
                    {
                        // Kum altı katmanları - Kumtaşı
                        prefab = sandstonePrefab;
                    }
                    else
                    {
                        // Derinlik - Çöl taşı veya normal taş
                        prefab = stonePrefab;
                    }

                    if (prefab != null)
                    {
                        Vector3Int pos = new Vector3Int(worldX, y, worldZ);
                        PlaceBlock(prefab, pos, rotation, chunkObj.transform, chunk);
                    }
                }
            }
            if (x % columnsPerFrame == 0 && x != 0) yield return null;
        }
    }

    void PlaceBlock(GameObject prefab, Vector3Int pos, Quaternion rot, Transform parent, Chunk chunk)
{
    if (chunk.blocks.ContainsKey(pos)) return;

    GameObject blockObj = Instantiate(prefab, (Vector3)pos, rot, parent);
    blockObj.hideFlags = HideFlags.HideInHierarchy;

    Block block = blockObj.GetComponent<Block>();

    chunk.blocks[pos] = block;
}


    void GenerateCactus(Vector3Int pos, Transform parent, Chunk chunk)
    {
        int height = Random.Range(minCactusHeight, maxCactusHeight + 1);
        
        for (int i = 0; i < height; i++)
        {
            PlaceBlock(cactusPrefab, pos + Vector3Int.up * i, Quaternion.identity, parent, chunk);
        }
    }

    // Blok kontrol fonksiyonları (PlayerInteraction için gerekli)
    public bool HasBlock(Vector3Int pos)
    {
        foreach (var chunk in chunks.Values)
        {
            if (chunk.blocks.ContainsKey(pos))
                return true;
        }
        return false;
    }

    public void RemoveBlockManually(GameObject blockObj)
    {
        Vector3Int pos = Vector3Int.RoundToInt(blockObj.transform.position);
        
        foreach (var chunk in chunks.Values)
        {
            if (chunk.blocks.ContainsKey(pos))
            {
                chunk.blocks.Remove(pos);
                Destroy(blockObj);
                return;
            }
        }
    }

    public void RegisterNewBlock(GameObject blockObj, Vector3Int pos)
    {
        Vector3Int chunkCoord = new Vector3Int(
            Mathf.FloorToInt(pos.x / (float)chunkSize),
            0,
            Mathf.FloorToInt(pos.z / (float)chunkSize)
        );

        if (chunks.ContainsKey(chunkCoord))
        {
            Block block = blockObj.GetComponent<Block>();
            if (block != null)
            {
                chunks[chunkCoord].blocks[pos] = block;
            }
        }
    }
}