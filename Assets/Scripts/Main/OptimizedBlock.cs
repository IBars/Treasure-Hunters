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
        
        // Yeni görünürlük durumunu hesapla
        bool[] newVisibleFaces = new bool[6];
        newVisibleFaces[0] = !world.HasBlock(pos + Vector3Int.up);      // Top
        newVisibleFaces[1] = !world.HasBlock(pos + Vector3Int.down);    // Bottom
        newVisibleFaces[2] = !world.HasBlock(pos + Vector3Int.forward); // North
        newVisibleFaces[3] = !world.HasBlock(pos + Vector3Int.back);    // South
        newVisibleFaces[4] = !world.HasBlock(pos + Vector3Int.right);   // East
        newVisibleFaces[5] = !world.HasBlock(pos + Vector3Int.left);    // West
        
        // Değişiklik var mı kontrol et
        bool hasChanged = false;
        for (int i = 0; i < 6; i++)
        {
            if (newVisibleFaces[i] != visibleFaces[i])
            {
                hasChanged = true;
                visibleFaces[i] = newVisibleFaces[i];
            }
        }
        
        // Değişiklik yoksa mesh güncelleme
        if (!hasChanged) return;
        
        // Eğer hiçbir yüz görünmüyorsa bloğu deaktif et
        bool anyVisible = false;
        for (int i = 0; i < 6; i++)
        {
            if (visibleFaces[i])
            {
                anyVisible = true;
                break;
            }
        }
        
        if (gameObject.activeSelf != anyVisible)
        {
            gameObject.SetActive(anyVisible);
        }
        
        if (anyVisible)
        {
            GenerateMesh();
        }
    }

    void GenerateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "Block_" + blockID;
        
        var vertices = new System.Collections.Generic.List<Vector3>();
        var triangles = new System.Collections.Generic.List<int>();
        var uvCoords = new System.Collections.Generic.List<Vector2>();
        
        // Submesh için triangle listleri
        var topTriangles = new System.Collections.Generic.List<int>();
        var sideTriangles = new System.Collections.Generic.List<int>();
        var bottomTriangles = new System.Collections.Generic.List<int>();
        
        // TOP FACE (Y+) - Submesh 0
        if (visibleFaces[0])
        {
            int vIndex = vertices.Count;
            vertices.Add(new Vector3(-0.5f, 0.5f, -0.5f));
            vertices.Add(new Vector3(0.5f, 0.5f, -0.5f));
            vertices.Add(new Vector3(-0.5f, 0.5f, 0.5f));
            vertices.Add(new Vector3(0.5f, 0.5f, 0.5f));
            
            uvCoords.AddRange(uvs);
            
            topTriangles.Add(vIndex + 0);
            topTriangles.Add(vIndex + 2);
            topTriangles.Add(vIndex + 1);
            topTriangles.Add(vIndex + 2);
            topTriangles.Add(vIndex + 3);
            topTriangles.Add(vIndex + 1);
        }
        
        // BOTTOM FACE (Y-) - Submesh 2
        if (visibleFaces[1])
        {
            int vIndex = vertices.Count;
            vertices.Add(new Vector3(-0.5f, -0.5f, -0.5f));
            vertices.Add(new Vector3(0.5f, -0.5f, -0.5f));
            vertices.Add(new Vector3(-0.5f, -0.5f, 0.5f));
            vertices.Add(new Vector3(0.5f, -0.5f, 0.5f));
            
            uvCoords.AddRange(uvs);
            
            bottomTriangles.Add(vIndex + 0);
            bottomTriangles.Add(vIndex + 1);
            bottomTriangles.Add(vIndex + 2);
            bottomTriangles.Add(vIndex + 2);
            bottomTriangles.Add(vIndex + 1);
            bottomTriangles.Add(vIndex + 3);
        }
        
        // NORTH FACE (Z+) - Submesh 1
        if (visibleFaces[2])
        {
            int vIndex = vertices.Count;
            vertices.Add(new Vector3(-0.5f, -0.5f, 0.5f));
            vertices.Add(new Vector3(0.5f, -0.5f, 0.5f));
            vertices.Add(new Vector3(-0.5f, 0.5f, 0.5f));
            vertices.Add(new Vector3(0.5f, 0.5f, 0.5f));
            
            uvCoords.AddRange(uvs);
            
            sideTriangles.Add(vIndex + 0);
            sideTriangles.Add(vIndex + 2);
            sideTriangles.Add(vIndex + 1);
            sideTriangles.Add(vIndex + 2);
            sideTriangles.Add(vIndex + 3);
            sideTriangles.Add(vIndex + 1);
        }
        
        // SOUTH FACE (Z-) - Submesh 1
        if (visibleFaces[3])
        {
            int vIndex = vertices.Count;
            vertices.Add(new Vector3(-0.5f, -0.5f, -0.5f));
            vertices.Add(new Vector3(0.5f, -0.5f, -0.5f));
            vertices.Add(new Vector3(-0.5f, 0.5f, -0.5f));
            vertices.Add(new Vector3(0.5f, 0.5f, -0.5f));
            
            uvCoords.AddRange(uvs);
            
            sideTriangles.Add(vIndex + 0);
            sideTriangles.Add(vIndex + 1);
            sideTriangles.Add(vIndex + 2);
            sideTriangles.Add(vIndex + 2);
            sideTriangles.Add(vIndex + 1);
            sideTriangles.Add(vIndex + 3);
        }
        
        // EAST FACE (X+) - Submesh 1
        if (visibleFaces[4])
        {
            int vIndex = vertices.Count;
            vertices.Add(new Vector3(0.5f, -0.5f, -0.5f));
            vertices.Add(new Vector3(0.5f, -0.5f, 0.5f));
            vertices.Add(new Vector3(0.5f, 0.5f, -0.5f));
            vertices.Add(new Vector3(0.5f, 0.5f, 0.5f));
            
            uvCoords.AddRange(uvs);
            
            sideTriangles.Add(vIndex + 0);
            sideTriangles.Add(vIndex + 2);
            sideTriangles.Add(vIndex + 1);
            sideTriangles.Add(vIndex + 2);
            sideTriangles.Add(vIndex + 3);
            sideTriangles.Add(vIndex + 1);
        }
        
        // WEST FACE (X-) - Submesh 1
        if (visibleFaces[5])
        {
            int vIndex = vertices.Count;
            vertices.Add(new Vector3(-0.5f, -0.5f, -0.5f));
            vertices.Add(new Vector3(-0.5f, -0.5f, 0.5f));
            vertices.Add(new Vector3(-0.5f, 0.5f, -0.5f));
            vertices.Add(new Vector3(-0.5f, 0.5f, 0.5f));
            
            uvCoords.AddRange(uvs);
            
            sideTriangles.Add(vIndex + 0);
            sideTriangles.Add(vIndex + 1);
            sideTriangles.Add(vIndex + 2);
            sideTriangles.Add(vIndex + 2);
            sideTriangles.Add(vIndex + 1);
            sideTriangles.Add(vIndex + 3);
        }
        
        mesh.vertices = vertices.ToArray();
        mesh.uv = uvCoords.ToArray();
        
        // 3 submesh ayarla
        mesh.subMeshCount = 3;
        mesh.SetTriangles(topTriangles.ToArray(), 0);      // Top material
        mesh.SetTriangles(sideTriangles.ToArray(), 1);     // Side material
        mesh.SetTriangles(bottomTriangles.ToArray(), 2);   // Bottom material
        
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        
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