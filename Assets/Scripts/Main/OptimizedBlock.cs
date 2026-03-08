using UnityEngine;

public class OptimizedBlock : MonoBehaviour
{
    public int blockID;
    public float health = 1f;
    
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private MeshCollider meshCollider;
    private Material[] materials;
    private Color originalColor;
    
    // Face visibility flags
    private bool[] visibleFaces = new bool[6]; // Top, Bottom, North, South, East, West
    private bool[] lastVisibleFaces = new bool[6]; // Önceki durum - değişim kontrolü için
    private bool meshNeedsUpdate = false;
    
    // Block textures (UV coordinates için)
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
        meshCollider = gameObject.AddComponent<MeshCollider>();
        
        // Varsayılan olarak tüm yüzler görünür - KESINLIKLE AÇIK OLMALI
        for (int i = 0; i < 6; i++)
        {
            visibleFaces[i] = true;
            lastVisibleFaces[i] = true; // Aynı yap ki ilk mesh oluşsun
        }
    }

    // Çoklu material desteği
    public void Initialize(Material topMat, Material sideMat, Material bottomMat, int id)
    {
        blockID = id;
        
        // 3 submesh: top, sides, bottom
        materials = new Material[3];
        materials[0] = new Material(topMat);    // Yeni instance oluştur
        materials[1] = new Material(sideMat);
        materials[2] = new Material(bottomMat);
        
        meshRenderer.materials = materials;
        
        if (topMat != null)
            originalColor = topMat.color;
        
        GenerateMesh();
    }
    
    // Tek material desteği (eski versiyon için backward compatibility)
    public void Initialize(Material blockMaterial, int id)
    {
        Initialize(blockMaterial, blockMaterial, blockMaterial, id);
    }

    public void UpdateVisibility(OptimizedChunkWorldGenerator world)
{
    Vector3Int pos = Vector3Int.RoundToInt(transform.position);
    
    // 6 tarafı kontrol et
    bool[] newVisibleFaces = new bool[6];
    newVisibleFaces[0] = !world.HasBlock(pos + Vector3Int.up);      // Top
    newVisibleFaces[1] = !world.HasBlock(pos + Vector3Int.down);    // Bottom
    newVisibleFaces[2] = !world.HasBlock(pos + Vector3Int.forward); // North
    newVisibleFaces[3] = !world.HasBlock(pos + Vector3Int.back);    // South
    newVisibleFaces[4] = !world.HasBlock(pos + Vector3Int.right);   // East
    newVisibleFaces[5] = !world.HasBlock(pos + Vector3Int.left);    // West
    
    bool anyVisible = false;
    bool hasChanged = false;

    for (int i = 0; i < 6; i++)
    {
        if (newVisibleFaces[i] != visibleFaces[i]) hasChanged = true;
        visibleFaces[i] = newVisibleFaces[i];
        if (visibleFaces[i]) anyVisible = true;
    }

    // Performans için: Eğer durum değişmediyse işlem yapma
    if (!hasChanged && gameObject.activeSelf == anyVisible) return;

    // Hiçbir yüzü görünmeyen bloğu render etmeyi bırak
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

    // --- TOP FACE (Y+) ---
    if (visibleFaces[0]) {
        int v = vertices.Count;
        vertices.Add(new Vector3(-0.5f, 0.5f, 0.5f));  vertices.Add(new Vector3(0.5f, 0.5f, 0.5f));
        vertices.Add(new Vector3(-0.5f, 0.5f, -0.5f)); vertices.Add(new Vector3(0.5f, 0.5f, -0.5f));
        uvCoords.AddRange(uvs);
        topTriangles.AddRange(new int[] { v, v + 1, v + 2, v + 2, v + 1, v + 3 });
    }
    
    // --- BOTTOM FACE (Y-) ---
    if (visibleFaces[1]) {
        int v = vertices.Count;
        vertices.Add(new Vector3(-0.5f, -0.5f, -0.5f)); vertices.Add(new Vector3(0.5f, -0.5f, -0.5f));
        vertices.Add(new Vector3(-0.5f, -0.5f, 0.5f));  vertices.Add(new Vector3(0.5f, -0.5f, 0.5f));
        uvCoords.AddRange(uvs);
        bottomTriangles.AddRange(new int[] { v, v + 1, v + 2, v + 2, v + 1, v + 3 });
    }

    // --- NORTH FACE (Z+) - İleri (İndeks: 2) ---
    if (visibleFaces[2]) {
        int v = vertices.Count;
        vertices.Add(new Vector3(0.5f, -0.5f, 0.5f));   vertices.Add(new Vector3(-0.5f, -0.5f, 0.5f));
        vertices.Add(new Vector3(0.5f, 0.5f, 0.5f));    vertices.Add(new Vector3(-0.5f, 0.5f, 0.5f));
        uvCoords.AddRange(uvs);
        sideTriangles.AddRange(new int[] { v, v + 2, v + 1, v + 1, v + 2, v + 3 });
    }

    // --- SOUTH FACE (Z-) - Geri (İndeks: 3) ---
    if (visibleFaces[3]) {
        int v = vertices.Count;
        vertices.Add(new Vector3(-0.5f, -0.5f, -0.5f));  vertices.Add(new Vector3(0.5f, -0.5f, -0.5f));
        vertices.Add(new Vector3(-0.5f, 0.5f, -0.5f));   vertices.Add(new Vector3(0.5f, 0.5f, -0.5f));
        uvCoords.AddRange(uvs);
        sideTriangles.AddRange(new int[] { v, v + 2, v + 1, v + 1, v + 2, v + 3 });
    }

    // --- EAST FACE (X+) - Sağ (İndeks: 4) ---
    if (visibleFaces[4]) {
        int v = vertices.Count;
        vertices.Add(new Vector3(0.5f, -0.5f, -0.5f));  vertices.Add(new Vector3(0.5f, -0.5f, 0.5f));
        vertices.Add(new Vector3(0.5f, 0.5f, -0.5f));   vertices.Add(new Vector3(0.5f, 0.5f, 0.5f));
        uvCoords.AddRange(uvs);
        sideTriangles.AddRange(new int[] { v, v + 2, v + 1, v + 1, v + 2, v + 3 });
    }

    // --- WEST FACE (X-) - Sol (İndeks: 5) ---
    if (visibleFaces[5]) {
        int v = vertices.Count;
        vertices.Add(new Vector3(-0.5f, -0.5f, 0.5f));  vertices.Add(new Vector3(-0.5f, -0.5f, -0.5f));
        vertices.Add(new Vector3(-0.5f, 0.5f, 0.5f));   vertices.Add(new Vector3(-0.5f, 0.5f, -0.5f));
        uvCoords.AddRange(uvs);
        sideTriangles.AddRange(new int[] { v, v + 2, v + 1, v + 1, v + 2, v + 3 });
    }

    mesh.vertices = vertices.ToArray();
    mesh.subMeshCount = 3;
    mesh.SetTriangles(topTriangles.ToArray(), 0);
    mesh.SetTriangles(sideTriangles.ToArray(), 1);
    mesh.SetTriangles(bottomTriangles.ToArray(), 2);
    mesh.uv = uvCoords.ToArray();
    
    mesh.RecalculateNormals(); // Işıklandırmanın düzgün olması için şart
    meshFilter.mesh = mesh;
    meshCollider.sharedMesh = mesh;
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