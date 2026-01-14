using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ChunkWorldGenerator : MonoBehaviour
{
    [Header("Block Prefabs")]
    public GameObject grassPrefab;
    public GameObject dirtPrefab;
    public GameObject stonePrefab;
    public GameObject cobblePrefab; // 🔥 EKLENDİ

    public Transform player;

    [Header("World Settings")]
    public int chunkSize = 16;
    public int viewDistance = 2;
    public float noiseScale = 0.05f;
    public int heightMultiplier = 15;
    public int baseHeight = 20;

    private Dictionary<Vector3Int, GameObject> chunks = new Dictionary<Vector3Int, GameObject>();
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
        HashSet<Vector3Int> active = new HashSet<Vector3Int>();

        for (int x = -viewDistance; x <= viewDistance; x++)
        {
            for (int z = -viewDistance; z <= viewDistance; z++)
            {
                Vector3Int coord = new Vector3Int(center.x + x, 0, center.z + z);
                active.Add(coord);

                if (!chunks.ContainsKey(coord))
                    yield return StartCoroutine(CreateChunk(coord));
                else
                    chunks[coord].SetActive(true);
            }
        }

        foreach (var c in new List<Vector3Int>(chunks.Keys))
        {
            if (!active.Contains(c))
                chunks[c].SetActive(false);
        }

        generating = false;
    }

    IEnumerator CreateChunk(Vector3Int coord)
    {
        GameObject chunk = new GameObject($"Chunk_{coord.x}_{coord.z}");
        chunk.transform.parent = transform;
        chunks.Add(coord, chunk);

        int blocksPerFrame = 400;
        int counter = 0;

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

                    if (y == surfaceY)
                    {
                        prefab = grassPrefab;
                        id = 0;
                    }
                    else if (y >= surfaceY - 5)
                    {
                        prefab = dirtPrefab;
                        id = 1;
                    }
                    else
                    {
                        prefab = stonePrefab;
                        id = 2;
                    }

                    GameObject blockObj = Instantiate(
                        prefab,
                        new Vector3(worldX, y, worldZ),
                        Quaternion.identity,
                        chunk.transform
                    );

                    Block block = blockObj.GetComponent<Block>() ?? blockObj.AddComponent<Block>();
                    block.blockID = id;

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

    // ===============================
    // 🔧 PlayerInteraction UYUMLULUK
    // ===============================

    public void RemoveBlockManually(GameObject block)
    {
        if (block != null)
            Destroy(block);
    }

    public void RegisterNewBlock(GameObject block, Vector3Int worldPos)
    {
        Vector3Int chunkCoord = new Vector3Int(
            Mathf.FloorToInt((float)worldPos.x / chunkSize),
            0,
            Mathf.FloorToInt((float)worldPos.z / chunkSize)
        );

        if (chunks.ContainsKey(chunkCoord))
        {
            block.transform.parent = chunks[chunkCoord].transform;
        }
        else
        {
            // Güvenlik: chunk yoksa world altında kalsın
            block.transform.parent = transform;
        }
    }
}
