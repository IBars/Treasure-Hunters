using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ChunkWorldGenerator : MonoBehaviour
{
    public static GameObject InstanceGameObject;
    public static ChunkWorldGenerator Instance; // Eğer eski kodunda 'Instance' kullanılıyorsa bu kalsın

void Awake()
{
    Instance = this;
    InstanceGameObject = this.gameObject;
    
    // Eğer Awake içinde başka kodların varsa onları da buraya ekleyebilirsin
}

    [Header("Block Prefabs")]
    public GameObject leafPrefab; 
    public GameObject grassPrefab;
    public GameObject dirtPrefab;
    public GameObject stonePrefab;
    public GameObject cobblePrefab;
    public GameObject logPrefab; 
    public GameObject dimensionBlockPrefab; 
    public GameObject cactusPrefab;


    [Header("Pond & Water Settings")]
    public GameObject waterPrefab; 
    public GameObject sandPrefab;  
    public int seaLevel = 22;      

    [Header("World Settings")]
    public Transform player;
    public int chunkSize = 16;
    public int viewDistance = 2;
    public float noiseScale = 0.05f;
    public int heightMultiplier = 15;
    public int baseHeight = 20;

    [Header("Tree Settings")]
    [Range(0, 100)]
    public float treeChance = 2f;

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
        GameObject chunkObj = new GameObject($"Chunk_{coord.x}_{coord.z}");
        chunkObj.transform.parent = transform;
        Chunk chunk = new Chunk(coord, chunkObj);
        chunks.Add(coord, chunk);

        for (int x = 0; x < chunkSize; x++)
        {
            for (int z = 0; z < chunkSize; z++)
            {
                int worldX = coord.x * chunkSize + x;
                int worldZ = coord.z * chunkSize + z;
                float noise = SimplexNoise.Noise(worldX * noiseScale, worldZ * noiseScale);
                int surfaceY = Mathf.FloorToInt(noise * heightMultiplier) + baseHeight;

                int finalY = Mathf.Max(surfaceY, seaLevel); 

                for (int y = finalY; y > finalY - 7; y--)
                {
                    GameObject prefab = null;
                    Quaternion rotation = Quaternion.identity;

                    // 1. Su Katmanı (Boşlukları doldur)
                    if (y > surfaceY) 
                    {
                        prefab = waterPrefab;
                    }
                    // 2. Tam Yüzey
                    else if (y == surfaceY)
                    {
                        if (y <= seaLevel) 
                        {
                            prefab = sandPrefab;
                        }
                        else 
                        {
                            prefab = grassPrefab;
                            rotation = Quaternion.Euler(-90f, 0f, 0f);

                            if (Random.Range(0f, 100f) < treeChance)
                            {
                                GenerateTree(new Vector3Int(worldX, y + 1, worldZ), chunkObj.transform, chunk);
                            }
                        }
                    }
                    // 3. Yüzeyin Altı
                    else if (y > surfaceY - 3) 
                    {
                        prefab = (surfaceY <= seaLevel) ? sandPrefab : dirtPrefab;
                    }
                    // 4. Derinler
                    else 
                    {
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

    // PlaceBlock artık ID parametresi almıyor, prefabtan okuyor
    void PlaceBlock(GameObject prefab, Vector3Int pos, Quaternion rot, Transform parent, Chunk chunk)
    {
        if (chunk.blocks.ContainsKey(pos)) return; 

        GameObject blockObj = Instantiate(prefab, (Vector3)pos, rot, parent);
        blockObj.hideFlags = HideFlags.HideInHierarchy;
        
        Block block = blockObj.GetComponent<Block>();
        // block.blockID ataması SİLİNDİ. Prefab üzerindeki değer korunur.
        
        chunk.blocks[pos] = block;
        // Örnek: Taş (Stone) üretirken %0.1 şansla bu bloğu koy
if (prefab == stonePrefab && Random.Range(0, 1000) < 1)
{
    prefab = dimensionBlockPrefab; // Müfettiş (Inspector) üzerinden atadığın özel blok
}
    }

    void GenerateTree(Vector3Int pos, Transform parent, Chunk chunk)
    {
        int height = Random.Range(4, 7); 
        for (int i = 0; i < height; i++)
        {
            PlaceBlock(logPrefab, pos + Vector3Int.up * i, Quaternion.identity, parent, chunk);
        }

        Vector3Int leafCenter = pos + Vector3Int.up * height;
        for (int x = -2; x <= 2; x++)
        {
            for (int y = -1; y <= 2; y++)
            {
                for (int z = -2; z <= 2; z++)
                {
                    Vector3Int lPos = leafCenter + new Vector3Int(x, y, z);
                    if (Vector3.Distance(leafCenter, lPos) < 2.8f)
                    {
                        PlaceBlock(leafPrefab, lPos, Quaternion.identity, parent, chunk);
                    }
                }
            }
        }
    }

    // Yardımcı Fonksiyonlar (Aynı kalıyor)
    public bool HasBlock(Vector3Int worldPos)
    {
        Vector3Int cc = GetChunkCoord(worldPos);
        return chunks.ContainsKey(cc) && chunks[cc].blocks.ContainsKey(worldPos);
    }

    public void RemoveBlockManually(GameObject blockObj)
    {
        Vector3Int pos = Vector3Int.RoundToInt(blockObj.transform.position);
        Vector3Int cc = GetChunkCoord(pos);
        if (chunks.ContainsKey(cc)) chunks[cc].blocks.Remove(pos);
        Destroy(blockObj);
        UpdateNeighbors(pos);
    }

    public void RegisterNewBlock(GameObject blockObj, Vector3Int worldPos)
    {
        Vector3Int cc = GetChunkCoord(worldPos);
        if (chunks.ContainsKey(cc))
        {
            blockObj.transform.parent = chunks[cc].root.transform;
            Block b = blockObj.GetComponent<Block>();
            chunks[cc].blocks[worldPos] = b;
            b.CheckVisibility(this);
            UpdateNeighbors(worldPos);
        }
    }

    void UpdateNeighbors(Vector3Int pos)
    {
        Vector3Int[] dirs = { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right, Vector3Int.forward, Vector3Int.back };
        foreach (var d in dirs)
        {
            Vector3Int nPos = pos + d;
            Vector3Int cc = GetChunkCoord(nPos);
            if (chunks.ContainsKey(cc) && chunks[cc].blocks.TryGetValue(nPos, out Block b))
                b.CheckVisibility(this);
        }
    }

    Vector3Int GetChunkCoord(Vector3Int pos) => new Vector3Int(Mathf.FloorToInt((float)pos.x / chunkSize), 0, Mathf.FloorToInt((float)pos.z / chunkSize));

    // --- SUYUN AŞAĞI YAYILMASI İÇİN GEREKEN YENİ KISIM ---
public void RegisterNewWater(Vector3Int worldPos)
{
    Chunk chunk = GetChunkFromPos(worldPos);
    if (chunk != null)
    {
        // PlaceBlock fonksiyonunu çağırarak o koordinata su yerleştirir
        PlaceBlock(waterPrefab, worldPos, Quaternion.identity, chunk.root.transform, chunk);
    }
}

// Koordinata bakarak hangi Chunk içinde olduğunu bulan yardımcı fonksiyon
private Chunk GetChunkFromPos(Vector3Int pos)
{
    Vector3Int cc = new Vector3Int(
        Mathf.FloorToInt((float)pos.x / chunkSize),
        0,
        Mathf.FloorToInt((float)pos.z / chunkSize)
    );
    return chunks.ContainsKey(cc) ? chunks[cc] : null;
}
}