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
        new Vector2(0, 0), new Vector2(1, 0),
        new Vector2(0, 1), new Vector2(1, 1)
    };

    // Shader.Find yerine sabit referans — URP/HDRP/Built-in fark etmez
    private static Material _fallback;
    private static Material Fallback
    {
        get
        {
            if (_fallback != null) return _fallback;
            Shader s = Shader.Find("Universal Render Pipeline/Lit")
                    ?? Shader.Find("HDRP/Lit")
                    ?? Shader.Find("Standard")
                    ?? Shader.Find("Diffuse");
            _fallback = s != null
                ? new Material(s) { color = Color.magenta }
                : new Material(Shader.Find("Hidden/InternalErrorShader"));
            return _fallback;
        }
    }

    void Awake()
    {
        meshFilter   = gameObject.AddComponent<MeshFilter>();
        meshRenderer = gameObject.AddComponent<MeshRenderer>();
        boxCollider  = gameObject.AddComponent<BoxCollider>();

        for (int i = 0; i < 6; i++)
        {
            visibleFaces[i]     = true;
            lastVisibleFaces[i] = true;
        }
    }

    public void Initialize(Material topMat, Material sideMat, Material bottomMat, int id)
    {
        blockID = id;

        switch (id)
        {
            case 0: health = 1.5f;  break; // Grass
            case 1: health = 1.5f;  break; // Dirt
            case 2: health = 5.0f;  break; // Stone
            case 3: health = 5.0f;  break; // Cobblestone
            case 4: health = 2.25f; break; // Log
            case 5: health = 0.6f;  break; // Leaf
            case 6: health = 1.0f;  break; // Sand
            case 9: health = 1.75f; break; // Cactus
            default: health = 1.0f; break;
        }

        materials    = new Material[3];
        materials[0] = topMat    != null ? new Material(topMat)    : new Material(Fallback);
        materials[1] = sideMat   != null ? new Material(sideMat)   : new Material(Fallback);
        materials[2] = bottomMat != null ? new Material(bottomMat) : new Material(Fallback);

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

        bool[] newVisible = new bool[6];
        newVisible[0] = !world.HasBlock(pos + Vector3Int.up);
        newVisible[1] = !world.HasBlock(pos + Vector3Int.down);
        newVisible[2] = !world.HasBlock(pos + Vector3Int.forward);
        newVisible[3] = !world.HasBlock(pos + Vector3Int.back);
        newVisible[4] = !world.HasBlock(pos + Vector3Int.right);
        newVisible[5] = !world.HasBlock(pos + Vector3Int.left);

        bool anyVisible = false;
        bool changed    = false;

        for (int i = 0; i < 6; i++)
        {
            if (newVisible[i] != visibleFaces[i]) changed = true;
            visibleFaces[i] = newVisible[i];
            if (visibleFaces[i]) anyVisible = true;
        }

        if (!changed && gameObject.activeSelf == anyVisible) return;

        if (gameObject.activeSelf != anyVisible)
            gameObject.SetActive(anyVisible);

        if (anyVisible) GenerateMesh();
    }

    void GenerateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "Block_" + blockID;

        var verts  = new System.Collections.Generic.List<Vector3>();
        var uv     = new System.Collections.Generic.List<Vector2>();
        var topTri  = new System.Collections.Generic.List<int>();
        var sideTri = new System.Collections.Generic.List<int>();
        var botTri  = new System.Collections.Generic.List<int>();

        // Yukarı
        if (visibleFaces[0]) {
            int v = verts.Count;
            verts.Add(new Vector3(-0.5f, 0.5f,  0.5f)); verts.Add(new Vector3( 0.5f, 0.5f,  0.5f));
            verts.Add(new Vector3(-0.5f, 0.5f, -0.5f)); verts.Add(new Vector3( 0.5f, 0.5f, -0.5f));
            uv.AddRange(uvs);
            topTri.AddRange(new[]{ v,v+1,v+2, v+2,v+1,v+3 });
        }
        // Aşağı
        if (visibleFaces[1]) {
            int v = verts.Count;
            verts.Add(new Vector3(-0.5f,-0.5f,-0.5f)); verts.Add(new Vector3( 0.5f,-0.5f,-0.5f));
            verts.Add(new Vector3(-0.5f,-0.5f, 0.5f)); verts.Add(new Vector3( 0.5f,-0.5f, 0.5f));
            uv.AddRange(uvs);
            botTri.AddRange(new[]{ v,v+1,v+2, v+2,v+1,v+3 });
        }
        // İleri
        if (visibleFaces[2]) {
            int v = verts.Count;
            verts.Add(new Vector3( 0.5f,-0.5f, 0.5f)); verts.Add(new Vector3(-0.5f,-0.5f, 0.5f));
            verts.Add(new Vector3( 0.5f, 0.5f, 0.5f)); verts.Add(new Vector3(-0.5f, 0.5f, 0.5f));
            uv.AddRange(uvs);
            sideTri.AddRange(new[]{ v,v+2,v+1, v+1,v+2,v+3 });
        }
        // Geri
        if (visibleFaces[3]) {
            int v = verts.Count;
            verts.Add(new Vector3(-0.5f,-0.5f,-0.5f)); verts.Add(new Vector3( 0.5f,-0.5f,-0.5f));
            verts.Add(new Vector3(-0.5f, 0.5f,-0.5f)); verts.Add(new Vector3( 0.5f, 0.5f,-0.5f));
            uv.AddRange(uvs);
            sideTri.AddRange(new[]{ v,v+2,v+1, v+1,v+2,v+3 });
        }
        // Sağ
        if (visibleFaces[4]) {
            int v = verts.Count;
            verts.Add(new Vector3( 0.5f,-0.5f,-0.5f)); verts.Add(new Vector3( 0.5f,-0.5f, 0.5f));
            verts.Add(new Vector3( 0.5f, 0.5f,-0.5f)); verts.Add(new Vector3( 0.5f, 0.5f, 0.5f));
            uv.AddRange(uvs);
            sideTri.AddRange(new[]{ v,v+2,v+1, v+1,v+2,v+3 });
        }
        // Sol
        if (visibleFaces[5]) {
            int v = verts.Count;
            verts.Add(new Vector3(-0.5f,-0.5f, 0.5f)); verts.Add(new Vector3(-0.5f,-0.5f,-0.5f));
            verts.Add(new Vector3(-0.5f, 0.5f, 0.5f)); verts.Add(new Vector3(-0.5f, 0.5f,-0.5f));
            uv.AddRange(uvs);
            sideTri.AddRange(new[]{ v,v+2,v+1, v+1,v+2,v+3 });
        }

        mesh.vertices    = verts.ToArray();
        mesh.subMeshCount = 3;
        mesh.SetTriangles(topTri.ToArray(),  0);
        mesh.SetTriangles(sideTri.ToArray(), 1);
        mesh.SetTriangles(botTri.ToArray(),  2);
        mesh.uv = uv.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        meshFilter.mesh = mesh;
    }

    public void Highlight(bool on)
    {
        if (materials == null) return;
        foreach (var mat in materials)
            if (mat != null)
                mat.color = on ? originalColor * 0.7f : originalColor;
    }
}