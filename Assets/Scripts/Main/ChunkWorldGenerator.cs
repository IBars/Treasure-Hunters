using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class OptimizedChunkWorldGenerator : MonoBehaviour
{
    public static GameObject InstanceGameObject;
    public static OptimizedChunkWorldGenerator Instance;

    void Awake()
    {
        Instance = this;
        InstanceGameObject = this.gameObject;
    }

    [Header("Block Materials")]
    public Material leafMaterial; 
    
    [Header("Grass Materials")]
    public Material grassTopMaterial;
    public Material grassSideMaterial;
    
    [Header("Log Materials")]
    public Material logTopMaterial;
    public Material logSideMaterial;
    
    [Header("Other Materials")]
    public Material dirtMaterial;
    public Material stoneMaterial;
    public Material cobbleMaterial;
    public Material sandMaterial;
    public Material waterMaterial;
    public Material dimensionBlockMaterial;
    public Material cactusMaterial;

    [Header("Special Prefabs (Non-optimized)")]
    public GameObject dimensionBlockPrefab; 
    public GameObject waterPrefab;

    [Header("World Settings")]
    public Transform player;
    public int chunkSize = 16;
    public int viewDistance = 2;
    public float noiseScale = 0.05f;
    public int heightMultiplier = 15;
    public int baseHeight = 20;
    public int seaLevel = 22;

    [Header("Tree Settings")]
    [Range(0, 100)]
    public float treeChance = 2f;

    [Header("Performance")]
    public int columnsPerFrame = 4;
    public bool enableFaceCulling = true; // ŞİMDİLİK KAPALI - TEST İÇİN
    
    [Header("Update Settings")]
    public float visibilityUpdateInterval = 2f; // 0.5'ten 2'ye çıkardık
    private float lastVisibilityUpdate = 0f;
    public int blocksPerVisibilityUpdate = 50; // Her frame'de kaç blok güncellenir

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
        
        // Periyodik visibility güncellemesi
        if (enableFaceCulling && Time.time - lastVisibilityUpdate > visibilityUpdateInterval)
        {
            UpdateVisibleBlocksAroundPlayer();
            lastVisibilityUpdate = Time.time;
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

        // Tüm chunk'lar oluşturulduktan SONRA face culling yap
        // ÖNEMLI: Bunu asenkron yap, yoksa oyun donar
        if (enableFaceCulling)
        {
            StartCoroutine(UpdateAllChunksVisibility(needed));
        }

        generating = false;
    }
    
    IEnumerator UpdateAllChunksVisibility(HashSet<Vector3Int> chunkCoords)
    {
        int blocksProcessed = 0;
        foreach (var coord in chunkCoords)
        {
            if (chunks.ContainsKey(coord))
            {
                // ToList() ile kopyasını al - enumeration hatası önlenir
                var blocksList = new List<OptimizedBlock>(chunks[coord].optimizedBlocks.Values);
                
                foreach (var block in blocksList)
                {
                    if (block != null)
                    {
                        block.UpdateVisibility(this);
                        blocksProcessed++;
                        
                        // Her 20 blokta bir yield yap
                        if (blocksProcessed % 20 == 0)
                            yield return null;
                    }
                }
            }
        }
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
                    Material blockMat = null;
                    int blockID = -1;
                    bool useSpecialPrefab = false;
                    GameObject specialPrefab = null;

                    // 1. Su Katmanı
                    if (y > surfaceY) 
                    {
                        useSpecialPrefab = true;
                        specialPrefab = waterPrefab;
                    }
                    // 2. Yüzey
                    else if (y == surfaceY)
                    {
                        if (y <= seaLevel) 
                        {
                            blockMat = sandMaterial;
                            blockID = 6;
                        }
                        else 
                        {
                            // Grass block (farklı top ve side)
                            blockID = 0;
                            // PlaceOptimizedBlock'a 3 material göndereceğiz

                            if (Random.Range(0f, 100f) < treeChance)
                            {
                                GenerateTree(new Vector3Int(worldX, y + 1, worldZ), chunkObj.transform, chunk);
                            }
                        }
                    }
                    // 3. Yüzeyin Altı
                    else if (y > surfaceY - 3) 
                    {
                        blockMat = (surfaceY <= seaLevel) ? sandMaterial : dirtMaterial;
                        blockID = (surfaceY <= seaLevel) ? 6 : 1;
                    }
                    // 4. Derinler
                    else 
                    {
                        blockMat = stoneMaterial;
                        blockID = 2;
                        
                        // Dimension block spawn
                        if (Random.Range(0, 1000) < 1)
                        {
                            useSpecialPrefab = true;
                            specialPrefab = dimensionBlockPrefab;
                        }
                    }

                    Vector3Int pos = new Vector3Int(worldX, y, worldZ);
                    
                    if (useSpecialPrefab && specialPrefab != null)
                    {
                        PlaceSpecialBlock(specialPrefab, pos, chunkObj.transform, chunk);
                    }
                    else if (blockID == 0) // Grass - özel material
                    {
                        PlaceOptimizedBlockMultiMat(grassTopMaterial, grassSideMaterial, grassSideMaterial, blockID, pos, chunkObj.transform, chunk);
                    }
                    else if (blockMat != null)
                    {
                        PlaceOptimizedBlock(blockMat, blockID, pos, chunkObj.transform, chunk);
                    }
                }
            }
            if (x % columnsPerFrame == 0 && x != 0) yield return null;
        }
        
        // ÖNEMLİ: İlk oluşumda face culling YAPMA
        // Çünkü komşu chunk'lar henüz olmayabilir
        // Oyun başladıktan sonra UpdateVisibleBlocksAroundPlayer halleder
    }

    void PlaceOptimizedBlock(Material mat, int id, Vector3Int pos, Transform parent, Chunk chunk)
    {
        if (chunk.blocks.ContainsKey(pos)) return;

        GameObject blockObj = new GameObject($"Block_{id}");
        blockObj.transform.position = (Vector3)pos;
        blockObj.transform.parent = parent;
        blockObj.hideFlags = HideFlags.HideInHierarchy;
        
        OptimizedBlock block = blockObj.AddComponent<OptimizedBlock>();
        block.Initialize(mat, id);
        
        chunk.optimizedBlocks[pos] = block;
    }
    
    void PlaceOptimizedBlockMultiMat(Material topMat, Material sideMat, Material bottomMat, int id, Vector3Int pos, Transform parent, Chunk chunk)
    {
        if (chunk.blocks.ContainsKey(pos)) return;

        GameObject blockObj = new GameObject($"Block_{id}");
        blockObj.transform.position = (Vector3)pos;
        blockObj.transform.parent = parent;
        blockObj.hideFlags = HideFlags.HideInHierarchy;
        
        OptimizedBlock block = blockObj.AddComponent<OptimizedBlock>();
        block.Initialize(topMat, sideMat, bottomMat, id);
        
        chunk.optimizedBlocks[pos] = block;
    }
    
    void PlaceSpecialBlock(GameObject prefab, Vector3Int pos, Transform parent, Chunk chunk)
    {
        if (chunk.blocks.ContainsKey(pos)) return;

        GameObject blockObj = Instantiate(prefab, (Vector3)pos, prefab.transform.rotation, parent);
        blockObj.hideFlags = HideFlags.HideInHierarchy;
        
        Block block = blockObj.GetComponent<Block>();
        if (block != null)
        {
            chunk.blocks[pos] = block;
        }
    }

    void GenerateTree(Vector3Int pos, Transform parent, Chunk chunk)
    {
        int height = Random.Range(4, 7);
        
        // Log blokları - çoklu material
        for (int i = 0; i < height; i++)
        {
            PlaceOptimizedBlockMultiMat(logTopMaterial, logSideMaterial, logTopMaterial, 4, pos + Vector3Int.up * i, parent, chunk);
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
                        PlaceOptimizedBlock(leafMaterial, 5, lPos, parent, chunk);
                    }
                }
            }
        }
    }

    IEnumerator UpdateChunkVisibility(Chunk chunk)
    {
        int count = 0;
        foreach (var block in chunk.optimizedBlocks.Values)
        {
            if (block != null)
            {
                block.UpdateVisibility(this);
                count++;
                
                if (count % 20 == 0)
                    yield return null;
            }
        }
    }

    void UpdateVisibleBlocksAroundPlayer()
    {
        if (!enableFaceCulling) return;
        
        Vector3Int playerPos = Vector3Int.RoundToInt(player.position);
        int updateRadius = 8;
        int blocksUpdated = 0;

        // Chunk listesinin kopyasını al
        var chunksList = new List<Chunk>(chunks.Values);
        
        foreach (var chunk in chunksList)
        {
            if (!chunk.root.activeSelf) continue;
            
            // Block listesinin kopyasını al
            var blocksList = new List<KeyValuePair<Vector3Int, OptimizedBlock>>(chunk.optimizedBlocks);
            
            foreach (var kvp in blocksList)
            {
                Vector3Int blockPos = kvp.Key;
                OptimizedBlock block = kvp.Value;
                
                if (block == null || !block.gameObject.activeSelf) continue;
                
                if (Vector3Int.Distance(blockPos, playerPos) < updateRadius)
                {
                    block.UpdateVisibility(this);
                    blocksUpdated++;
                    
                    if (blocksUpdated >= blocksPerVisibilityUpdate)
                        return;
                }
            }
        }
    }

    public bool HasBlock(Vector3Int pos)
    {
        foreach (var chunk in chunks.Values)
        {
            if (chunk.blocks.ContainsKey(pos) || chunk.optimizedBlocks.ContainsKey(pos))
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
                UpdateNeighborBlocks(pos);
                return;
            }
            
            if (chunk.optimizedBlocks.ContainsKey(pos))
            {
                chunk.optimizedBlocks.Remove(pos);
                Destroy(blockObj);
                UpdateNeighborBlocks(pos);
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
            OptimizedBlock optBlock = blockObj.GetComponent<OptimizedBlock>();
            if (optBlock != null)
            {
                chunks[chunkCoord].optimizedBlocks[pos] = optBlock;
                optBlock.UpdateVisibility(this);
                UpdateNeighborBlocks(pos);
            }
            else
            {
                Block block = blockObj.GetComponent<Block>();
                if (block != null)
                {
                    chunks[chunkCoord].blocks[pos] = block;
                }
            }
        }
    }

    void UpdateNeighborBlocks(Vector3Int pos)
    {
        if (!enableFaceCulling) return;
        
        Vector3Int[] neighbors = new Vector3Int[]
        {
            pos + Vector3Int.up,
            pos + Vector3Int.down,
            pos + Vector3Int.forward,
            pos + Vector3Int.back,
            pos + Vector3Int.right,
            pos + Vector3Int.left
        };

        foreach (var neighborPos in neighbors)
        {
            foreach (var chunk in chunks.Values)
            {
                if (chunk.optimizedBlocks.ContainsKey(neighborPos))
                {
                    chunk.optimizedBlocks[neighborPos].UpdateVisibility(this);
                }
            }
        }
    }

    public void RegisterNewWater(Vector3Int pos)
    {
        Vector3Int chunkCoord = new Vector3Int(
            Mathf.FloorToInt(pos.x / (float)chunkSize),
            0,
            Mathf.FloorToInt(pos.z / (float)chunkSize)
        );

        if (chunks.ContainsKey(chunkCoord))
        {
            if (!chunks[chunkCoord].blocks.ContainsKey(pos) && 
                !chunks[chunkCoord].optimizedBlocks.ContainsKey(pos))
            {
                PlaceSpecialBlock(waterPrefab, pos, chunks[chunkCoord].root.transform, chunks[chunkCoord]);
            }
        }
    }
}