using UnityEngine;

public class OptimizedBlock : MonoBehaviour
{
    public int blockID;
    public float health = 1f;
    
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private BoxCollider boxCollider;
    private Material[] materials;
    private Color originalColor;
    
    private bool[] visibleFaces = new bool[6];
    private bool[] lastVisibleFaces = new bool[6];
    
    private static readonly Vector2[] uvs = new Vector2[]
    {
        new Vector2(0, 0),
        new Vector2(1, 0),
        new Vector2(0, 1),
        new Vector2(1, 1)
    };

    void Awake()
    {
        meshFilter = gameObject.AddComponent<MeshFilter>();
        meshRenderer = gameObject.AddComponent<MeshRenderer>();
        boxCollider = gameObject.AddComponent<BoxCollider>();
        
        for (int i = 0; i < 6; i++)
        {
            visibleFaces[i] = true;
            lastVisibleFaces[i] = true;
        }
    }

    public void Initialize(Material topMat, Material sideMat, Material bottomMat, int id)
    {
        blockID = id;
        
        // SORUN BURADAYDI! Health hiç atanmıyordu
        // Block ID'ye göre health ayarla
        switch (id)
        {
            case 0: health = 1.5f; break;  // Grass
            case 1: health = 1.5f; break; // Dirt
            case 2: health = 5.0f; break;  // Stone - EN YAVAS
            case 3: health = 5.0f; break;  // Cobblestone
            case 4: health = 2.25f; break;  // Log
            case 5: health = 0.6f; break;  // Leaf - EN HIZLI
            case 6: health = 1.0f; break;  // Sand
            case 9: health = 1.75f; break;  // Cactus
            default: health = 1.0f; break;
        }
        
        materials = new Material[3];
        materials[0] = topMat    != null ? new Material(topMat)    : new Material(Shader.Find("Standard"));
        materials[1] = sideMat   != null ? new Material(sideMat)   : new Material(Shader.Find("Standard"));
        materials[2] = bottomMat != null ? new Material(bottomMat) : new Material(Shader.Find("Standard"));
        
        meshRenderer.materials = materials;
        
        if (topMat != null)
            originalColor = topMat.color;
        
        GenerateMesh();
    }
    
    public void Initialize(Material blockMaterial, int id)
    {
        Initialize(blockMaterial, blockMaterial, blockMaterial, id);
    }

    public void UpdateVisibility(OptimizedChunkWorldGenerator world)
    {
        Vector3Int pos = Vector3Int.RoundToInt(transform.position);
        
        bool[] newVisibleFaces = new bool[6];
        newVisibleFaces[0] = !world.HasBlock(pos + Vector3Int.up);
        newVisibleFaces[1] = !world.HasBlock(pos + Vector3Int.down);
        newVisibleFaces[2] = !world.HasBlock(pos + Vector3Int.forward);
        newVisibleFaces[3] = !world.HasBlock(pos + Vector3Int.back);
        newVisibleFaces[4] = !world.HasBlock(pos + Vector3Int.right);
        newVisibleFaces[5] = !world.HasBlock(pos + Vector3Int.left);
        
        bool anyVisible = false;
        bool hasChanged = false;

        for (int i = 0; i < 6; i++)
        {
            if (newVisibleFaces[i] != visibleFaces[i]) hasChanged = true;
            visibleFaces[i] = newVisibleFaces[i];
            if (visibleFaces[i]) anyVisible = true;
        }

        if (!hasChanged && gameObject.activeSelf == anyVisible) return;

        if (gameObject.activeSelf != anyVisible)
            gameObject.SetActive(anyVisible);

        if (anyVisible)
            GenerateMesh();
    }

    void GenerateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "Block_" + blockID;
        
        var vertices = new System.Collections.Generic.List<Vector3>();
        var uvCoords = new System.Collections.Generic.List<Vector2>();
        
        var topTriangles = new System.Collections.Generic.List<int>();
        var sideTriangles = new System.Collections.Generic.List<int>();
        var bottomTriangles = new System.Collections.Generic.List<int>();

        if (visibleFaces[0]) {
            int v = vertices.Count;
            vertices.Add(new Vector3(-0.5f, 0.5f, 0.5f));
            vertices.Add(new Vector3(0.5f, 0.5f, 0.5f));
            vertices.Add(new Vector3(-0.5f, 0.5f, -0.5f));
            vertices.Add(new Vector3(0.5f, 0.5f, -0.5f));
            uvCoords.AddRange(uvs);
            topTriangles.AddRange(new int[] { v, v + 1, v + 2, v + 2, v + 1, v + 3 });
        }
        
        if (visibleFaces[1]) {
            int v = vertices.Count;
            vertices.Add(new Vector3(-0.5f, -0.5f, -0.5f));
            vertices.Add(new Vector3(0.5f, -0.5f, -0.5f));
            vertices.Add(new Vector3(-0.5f, -0.5f, 0.5f));
            vertices.Add(new Vector3(0.5f, -0.5f, 0.5f));
            uvCoords.AddRange(uvs);
            bottomTriangles.AddRange(new int[] { v, v + 1, v + 2, v + 2, v + 1, v + 3 });
        }

        if (visibleFaces[2]) {
            int v = vertices.Count;
            vertices.Add(new Vector3(0.5f, -0.5f, 0.5f));
            vertices.Add(new Vector3(-0.5f, -0.5f, 0.5f));
            vertices.Add(new Vector3(0.5f, 0.5f, 0.5f));
            vertices.Add(new Vector3(-0.5f, 0.5f, 0.5f));
            uvCoords.AddRange(uvs);
            sideTriangles.AddRange(new int[] { v, v + 2, v + 1, v + 1, v + 2, v + 3 });
        }

        if (visibleFaces[3]) {
            int v = vertices.Count;
            vertices.Add(new Vector3(-0.5f, -0.5f, -0.5f));
            vertices.Add(new Vector3(0.5f, -0.5f, -0.5f));
            vertices.Add(new Vector3(-0.5f, 0.5f, -0.5f));
            vertices.Add(new Vector3(0.5f, 0.5f, -0.5f));
            uvCoords.AddRange(uvs);
            sideTriangles.AddRange(new int[] { v, v + 2, v + 1, v + 1, v + 2, v + 3 });
        }

        if (visibleFaces[4]) {
            int v = vertices.Count;
            vertices.Add(new Vector3(0.5f, -0.5f, -0.5f));
            vertices.Add(new Vector3(0.5f, -0.5f, 0.5f));
            vertices.Add(new Vector3(0.5f, 0.5f, -0.5f));
            vertices.Add(new Vector3(0.5f, 0.5f, 0.5f));
            uvCoords.AddRange(uvs);
            sideTriangles.AddRange(new int[] { v, v + 2, v + 1, v + 1, v + 2, v + 3 });
        }

        if (visibleFaces[5]) {
            int v = vertices.Count;
            vertices.Add(new Vector3(-0.5f, -0.5f, 0.5f));
            vertices.Add(new Vector3(-0.5f, -0.5f, -0.5f));
            vertices.Add(new Vector3(-0.5f, 0.5f, 0.5f));
            vertices.Add(new Vector3(-0.5f, 0.5f, -0.5f));
            uvCoords.AddRange(uvs);
            sideTriangles.AddRange(new int[] { v, v + 2, v + 1, v + 1, v + 2, v + 3 });
        }

        mesh.vertices = vertices.ToArray();
        mesh.subMeshCount = 3;
        mesh.SetTriangles(topTriangles.ToArray(), 0);
        mesh.SetTriangles(sideTriangles.ToArray(), 1);
        mesh.SetTriangles(bottomTriangles.ToArray(), 2);
        mesh.uv = uvCoords.ToArray();
        
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        
        meshFilter.mesh = mesh;
    }

    public void Highlight(bool on)
    {
        if (materials == null || materials.Length == 0) return;
        
        foreach (var mat in materials)
        {
            if (mat != null)
                mat.color = on ? originalColor * 0.7f : originalColor;
        }
    }
}