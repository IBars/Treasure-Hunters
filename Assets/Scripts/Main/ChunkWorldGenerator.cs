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

    [Header("Special Prefabs")]
    public GameObject dimensionBlockPrefab;
    public GameObject waterPrefab;

    [Header("World Settings")]
    public Transform player;
    public int chunkSize = 16;
    public int viewDistance = 3;
    public float noiseScale = 0.05f;
    public int heightMultiplier = 15;
    public int baseHeight = 20;
    public int seaLevel = 22;

    [Header("Tree Settings")]
    [Range(0, 100)]
    public float treeChance = 2f;

    [Header("Performance")]
    public bool enableFaceCulling = true;
    public float visibilityUpdateInterval = 2f;
    public int blocksPerVisibilityUpdate = 50;

    [Header("Frustum Culling")]
    public Camera mainCamera;
    public bool enableFrustumCulling = true;
    public float frustumUpdateInterval = 0.1f;
    private float lastFrustumUpdate;

    [Header("Spawn Protection")]
    public bool enableSpawnWait = true;
    private CharacterController playerCC;

    // Fallback materyal — hiçbir şey null'a düşmez
    private Material fallbackMaterial;

    private Dictionary<Vector3Int, Chunk> chunks = new Dictionary<Vector3Int, Chunk>();
    private HashSet<Vector3Int> blockPositions = new HashSet<Vector3Int>();

    private Vector3Int targetCenter  = new Vector3Int(999, 0, 999);
    private Vector3Int lastPlayerChunk = new Vector3Int(999, 0, 999);
    private bool manageRunning = false;

    // ───────────────────────────────────────────────────────────────────────
    void Start()
    {
        // Hiçbir materyal atanmamış olsa bile kırılmayacak fallback
        fallbackMaterial = new Material(Shader.Find("Standard"));
        fallbackMaterial.color = Color.magenta;

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (player != null)
        {
            playerCC = player.GetComponent<CharacterController>();
            if (enableSpawnWait && playerCC != null)
                playerCC.enabled = false;

            targetCenter = WorldToChunk(player.position);
            StartCoroutine(SpawnSequence());
        }
    }

    void Update()
    {
        if (!player) return;

        Vector3Int current = WorldToChunk(player.position);
        if (current != lastPlayerChunk)
        {
            lastPlayerChunk = current;
            targetCenter    = current;
            if (!manageRunning)
                StartCoroutine(ChunkLoop());
        }

        if (enableFrustumCulling && Time.time - lastFrustumUpdate > frustumUpdateInterval)
        {
            UpdateFrustumCulling();
            lastFrustumUpdate = Time.time;
        }
    }

    // ─── SPAWN ─────────────────────────────────────────────────────────────
    IEnumerator SpawnSequence()
    {
        // 1. Oyuncunun altındaki chunk'ı hemen üret
        Vector3Int spawnChunk = targetCenter;
        yield return StartCoroutine(CreateChunk(spawnChunk));

        // 2. Oyuncuyu doğru zemine koy ve serbest bırak
        if (enableSpawnWait && playerCC != null)
        {
            int sy = GetSurfaceY(Mathf.FloorToInt(player.position.x),
                                 Mathf.FloorToInt(player.position.z));
            Vector3 p = player.position;
            p.y = sy + 1.5f;
            player.position = p;
            playerCC.enabled = true;
        }

        // 3. Etrafını arka planda üret
        yield return StartCoroutine(ChunkLoop());
    }

    // ─── ANA CHUNK DÖNGÜSÜ ────────────────────────────────────────────────
    IEnumerator ChunkLoop()
    {
        manageRunning = true;

        while (true)
        {
            Vector3Int center = targetCenter;

            // viewDistance içindeki tüm chunk'ları mesafeye göre sırala
            List<Vector3Int> needed = GetSortedNeeded(center);

            bool anyCreated = false;
            foreach (Vector3Int coord in needed)
            {
                if (!chunks.ContainsKey(coord))
                {
                    yield return StartCoroutine(CreateChunk(coord));
                    anyCreated = true;

                    // Oyuncu hareket ettiyse listeyi yeniden hesapla
                    if (targetCenter != center) break;
                }
            }

            // Menzil dışına çıkan chunk'ları gizle
            HashSet<Vector3Int> neededSet = new HashSet<Vector3Int>(needed);
            foreach (var pair in chunks)
                if (!neededSet.Contains(pair.Key) && pair.Value.root != null)
                    pair.Value.root.SetActive(false);

            // Her şey tamam ve oyuncu duruyor → çık
            if (!anyCreated && targetCenter == center)
                break;

            yield return null;
        }

        manageRunning = false;
    }

    List<Vector3Int> GetSortedNeeded(Vector3Int center)
    {
        var list = new List<Vector3Int>();
        for (int x = -viewDistance; x <= viewDistance; x++)
            for (int z = -viewDistance; z <= viewDistance; z++)
                list.Add(new Vector3Int(center.x + x, 0, center.z + z));

        list.Sort((a, b) =>
        {
            float da = (new Vector2(a.x, a.z) - new Vector2(center.x, center.z)).sqrMagnitude;
            float db = (new Vector2(b.x, b.z) - new Vector2(center.x, center.z)).sqrMagnitude;
            return da.CompareTo(db);
        });
        return list;
    }

    // ─── CHUNK OLUŞTURMA ──────────────────────────────────────────────────
    IEnumerator CreateChunk(Vector3Int coord)
    {
        if (chunks.ContainsKey(coord)) yield break;

        GameObject chunkObj = new GameObject($"Chunk_{coord.x}_{coord.z}");
        chunkObj.transform.parent = transform;
        Chunk chunk = new Chunk(coord, chunkObj);
        chunks.Add(coord, chunk);

        int batchCount = 0;
        const int MAX_PER_FRAME = 80;

        for (int x = 0; x < chunkSize; x++)
        {
            for (int z = 0; z < chunkSize; z++)
            {
                int worldX = coord.x * chunkSize + x;
                int worldZ = coord.z * chunkSize + z;

                float noise    = SimplexNoise.Noise(worldX * noiseScale, worldZ * noiseScale);
                int surfaceY   = Mathf.FloorToInt(noise * heightMultiplier) + baseHeight;
                int topY       = Mathf.Max(surfaceY, seaLevel);

                for (int y = topY; y > topY - 7; y--)
                {
                    Vector3Int pos = new Vector3Int(worldX, y, worldZ);

                    // ── Su katmanı ──────────────────────────────────────
                    if (y > surfaceY)
                    {
                        if (waterPrefab != null)
                        {
                            Instantiate(waterPrefab, (Vector3)pos, Quaternion.identity, chunkObj.transform);
                            blockPositions.Add(pos);
                        }
                        continue;
                    }

                    // ── Blok türünü belirle ─────────────────────────────
                    int      blockID  = -1;
                    Material top      = null;
                    Material side     = null;
                    Material bottom   = null;

                    bool isSand = surfaceY <= seaLevel;

                    if (y == surfaceY)
                    {
                        if (isSand)
                        {
                            blockID = 6;
                            top = side = bottom = Safe(sandMaterial);
                        }
                        else
                        {
                            blockID = 0;
                            top    = Safe(grassTopMaterial);
                            side   = Safe(grassSideMaterial);
                            bottom = Safe(dirtMaterial);

                            if (Random.Range(0f, 100f) < treeChance)
                                GenerateTree(new Vector3Int(worldX, y + 1, worldZ),
                                             chunkObj.transform, chunk);
                        }
                    }
                    else if (y > surfaceY - 3)
                    {
                        if (isSand) { blockID = 6; top = side = bottom = Safe(sandMaterial); }
                        else        { blockID = 1; top = side = bottom = Safe(dirtMaterial); }
                    }
                    else
                    {
                        blockID = 2;
                        top = side = bottom = Safe(stoneMaterial);
                    }

                    if (blockID < 0) continue;

                    // ── Blok GameObject oluştur ─────────────────────────
                    try
                    {
                        GameObject blockObj = new GameObject($"B_{worldX}_{y}_{worldZ}");
                        blockObj.transform.position = (Vector3)pos;
                        blockObj.transform.parent   = chunkObj.transform;

                        OptimizedBlock ob = blockObj.AddComponent<OptimizedBlock>();
                        ob.Initialize(top, side, bottom, blockID);

                        chunk.optimizedBlocks[pos] = ob;
                        blockPositions.Add(pos);
                    }
                    catch (System.Exception e)
                    {
                        // Tek bir blok patlarsa tüm chunk durmasın
                        Debug.LogWarning($"[TerrainGen] Blok atlandı {pos}: {e.Message}");
                        continue;
                    }

                    batchCount++;
                    if (batchCount >= MAX_PER_FRAME)
                    {
                        batchCount = 0;
                        yield return null;
                    }
                }
            }
        }
    }

    // Null materyali fallback ile değiştirir — asla null dönmez
    Material Safe(Material mat) => mat != null ? mat : fallbackMaterial;

    // ─── FRUSTUM CULLING ───────────────────────────────────────────────────
    void UpdateFrustumCulling()
    {
        if (mainCamera == null) return;
        Plane[] planes  = GeometryUtility.CalculateFrustumPlanes(mainCamera);
        Vector3Int center = targetCenter;

        foreach (var pair in chunks)
        {
            if (pair.Value.root == null) continue;
            Vector3Int coord = pair.Key;

            float d = (new Vector2(coord.x, coord.z) -
                       new Vector2(center.x, center.z)).sqrMagnitude;

            if (d > (viewDistance + 1f) * (viewDistance + 1f))
            {
                if (pair.Value.root.activeSelf)
                    pair.Value.root.SetActive(false);
                continue;
            }

            Vector3 ctr = new Vector3(coord.x * chunkSize + chunkSize * 0.5f,
                                      baseHeight * 0.5f,
                                      coord.z * chunkSize + chunkSize * 0.5f);

            bool vis = GeometryUtility.TestPlanesAABB(planes,
                new Bounds(ctr, new Vector3(chunkSize, baseHeight + heightMultiplier, chunkSize)));

            if (pair.Value.root.activeSelf != vis)
                pair.Value.root.SetActive(vis);
        }
    }

    // ─── YARDIMCI ──────────────────────────────────────────────────────────
    Vector3Int WorldToChunk(Vector3 p) =>
        new Vector3Int(Mathf.FloorToInt(p.x / chunkSize), 0,
                       Mathf.FloorToInt(p.z / chunkSize));

    public int GetSurfaceY(int wx, int wz)
    {
        float n = SimplexNoise.Noise(wx * noiseScale, wz * noiseScale);
        return Mathf.FloorToInt(n * heightMultiplier) + baseHeight;
    }

    public bool HasBlock(Vector3Int pos) => blockPositions.Contains(pos);

    public void RemoveBlockManually(GameObject blockObj)
    {
        Vector3Int pos = Vector3Int.RoundToInt(blockObj.transform.position);
        blockPositions.Remove(pos);
        Vector3Int cc = WorldToChunk(blockObj.transform.position);
        if (chunks.ContainsKey(cc)) chunks[cc].optimizedBlocks.Remove(pos);
        Destroy(blockObj);
    }

    public void RegisterNewBlock(GameObject blockObj, Vector3Int pos)
    {
        blockPositions.Add(pos);
        Vector3Int cc = WorldToChunk(blockObj.transform.position);
        if (!chunks.ContainsKey(cc)) return;
        OptimizedBlock ob = blockObj.GetComponent<OptimizedBlock>();
        if (ob != null) chunks[cc].optimizedBlocks[pos] = ob;
    }

    public void RegisterNewWater(Vector3Int pos)
    {
        if (HasBlock(pos) || waterPrefab == null) return;
        Vector3Int cc = WorldToChunk((Vector3)pos);
        Transform parent = chunks.ContainsKey(cc) ? chunks[cc].root.transform : transform;
        Instantiate(waterPrefab, (Vector3)pos, Quaternion.identity, parent);
        blockPositions.Add(pos);
    }

    // ─── AĞAÇ ──────────────────────────────────────────────────────────────
    void GenerateTree(Vector3Int basePos, Transform parent, Chunk chunk)
    {
        for (int i = 0; i < 5; i++)
            PlaceBlock(new Vector3Int(basePos.x, basePos.y + i, basePos.z),
                       Safe(logTopMaterial), Safe(logSideMaterial), Safe(logTopMaterial),
                       4, parent, chunk);

        Material lm = Safe(leafMaterial);
        for (int dx = -2; dx <= 2; dx++)
        for (int dz = -2; dz <= 2; dz++)
        for (int dy = 3; dy <= 5; dy++)
        {
            if (Mathf.Abs(dx) == 2 && Mathf.Abs(dz) == 2) continue;
            Vector3Int lp = new Vector3Int(basePos.x + dx, basePos.y + dy, basePos.z + dz);
            if (!HasBlock(lp)) PlaceBlock(lp, lm, lm, lm, 5, parent, chunk);
        }
    }

    void PlaceBlock(Vector3Int pos, Material top, Material side, Material bot,
                    int id, Transform parent, Chunk chunk)
    {
        try
        {
            GameObject go = new GameObject($"B_{pos.x}_{pos.y}_{pos.z}");
            go.transform.position = (Vector3)pos;
            go.transform.parent   = parent;
            OptimizedBlock ob = go.AddComponent<OptimizedBlock>();
            ob.Initialize(top, side, bot, id);
            chunk.optimizedBlocks[pos] = ob;
            blockPositions.Add(pos);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[TerrainGen] PlaceBlock atlandı {pos}: {e.Message}");
        }
    }
}