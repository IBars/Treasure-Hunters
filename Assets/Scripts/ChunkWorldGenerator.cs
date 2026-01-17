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
    public GameObject logPrefab; 
    public GameObject leafPrefab; 

    public Transform player;



    [Header("World Settings")]

    public int chunkSize = 16;

    public int viewDistance = 2;

    public float noiseScale = 0.05f;

    public int heightMultiplier = 15;

    public int baseHeight = 20;


    [Header("Tree Settings")]
    [Range(0, 100)]
    public float treeChance = 2f; // EKLE: %2 ağaç çıkma şansı



    [Header("Performance")]

    [Tooltip("Bir karede kaç sütun (x) işlenecek? (16 yaparsan tek karede biter)")]

    public int columnsPerFrame = 4;



    private Dictionary<Vector3Int, Chunk> chunks = new Dictionary<Vector3Int, Chunk>();

    private Vector3Int lastPlayerChunk = new Vector3Int(999, 0, 999);

    private bool generating = false;



    void Awake() => Instance = this;



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

            // En üstten aşağıya doğru blokları yerleştiriyoruz
            for (int y = surfaceY; y > surfaceY - 7; y--)
            {
                GameObject prefab;
                int id;
                Quaternion rotation = Quaternion.identity;

                // Blok tipini belirle
                if (y == surfaceY) 
                { 
                    prefab = grassPrefab; 
                    id = 0; 
                    rotation = Quaternion.Euler(-90f, 0f, 0f); 

                    // AĞAÇ OLUŞTURMA: Sadece en üst blok çimense ve şans yaver giderse
                    if (Random.Range(0f, 100f) < treeChance)
                    {
                        GenerateTree(new Vector3Int(worldX, y + 1, worldZ), chunkObj.transform, chunk);
                    }
                }
                else if (y > surfaceY - 3) 
                { 
                    prefab = dirtPrefab; 
                    id = 1; 
                }
                else 
                { 
                    prefab = stonePrefab; 
                    id = 2; 
                }

                // Bloğu dünyaya yerleştir
                Vector3Int pos = new Vector3Int(worldX, y, worldZ);
                PlaceBlock(prefab, pos, id, rotation, chunkObj.transform, chunk);
            }
        }

        // Performans ayarı: Belirli sütun sayısında bir kare bekle
        if (x % columnsPerFrame == 0 && x != 0)
            yield return null;
    }

    // Görünürlük kontrolü (Face Culling simülasyonu)
    foreach (var b in chunk.blocks.Values) b.CheckVisibility(this);
    
    // Kenar chunk'ları güncelle
    UpdateBorderChunks(coord);
    
    // Performans için objeleri birleştir
    StaticBatchingUtility.Combine(chunkObj);
}



    // ... (Geri kalan HasBlock, RemoveBlockManually, RegisterNewBlock, UpdateNeighbors, GetChunkCoord aynı kalıyor)

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



    void UpdateBorderChunks(Vector3Int coord)

    {

        Vector3Int[] neighbors = { Vector3Int.left, Vector3Int.right, Vector3Int.forward, Vector3Int.back };

        foreach (var n in neighbors)

        {

            Vector3Int nCoord = coord + n;

            if (chunks.ContainsKey(nCoord))

                foreach (var b in chunks[nCoord].blocks.Values) b.CheckVisibility(this);

        }

    }



    Vector3Int GetChunkCoord(Vector3Int pos) => new Vector3Int(Mathf.FloorToInt((float)pos.x / chunkSize), 0, Mathf.FloorToInt((float)pos.z / chunkSize));

    void PlaceBlock(GameObject prefab, Vector3Int pos, int id, Quaternion rot, Transform parent, Chunk chunk)
{
    if (chunk.blocks.ContainsKey(pos)) return; // Orada zaten blok varsa koyma

    GameObject blockObj = Instantiate(prefab, (Vector3)pos, rot, parent);
    blockObj.hideFlags = HideFlags.HideInHierarchy;
    
    Block block = blockObj.GetComponent<Block>();
    block.blockID = id;
    chunk.blocks[pos] = block;
}

void GenerateTree(Vector3Int pos, Transform parent, Chunk chunk)
{
    int height = Random.Range(4, 7); // 4-6 blok yüksekliğinde

    // Gövde (Odunlar)
    for (int i = 0; i < height; i++)
    {
        PlaceBlock(logPrefab, pos + Vector3Int.up * i, 4, Quaternion.identity, parent, chunk);
    }

    // Yapraklar
    Vector3Int leafCenter = pos + Vector3Int.up * height;
    for (int x = -2; x <= 2; x++)
    {
        for (int y = -1; y <= 2; y++)
        {
            for (int z = -2; z <= 2; z++)
            {
                Vector3Int lPos = leafCenter + new Vector3Int(x, y, z);
                // Küresel yaprak yapısı için mesafe kontrolü
                if (Vector3.Distance(leafCenter, lPos) < 2.8f)
                {
                    PlaceBlock(leafPrefab, lPos, 5, Quaternion.identity, parent, chunk);
                }
            }
        }
    }
}



}