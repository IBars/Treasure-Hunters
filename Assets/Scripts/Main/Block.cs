using UnityEngine;

public class Block : MonoBehaviour
{
    public int blockID;
    public float health = 1f;
    Renderer rend;
    Color originalColor;

    void Awake()
    {
        rend = GetComponent<Renderer>();
        if (rend != null) originalColor = rend.material.color;
    }

    public void Highlight(bool on)
    {
        if (rend == null) return;
        rend.material.color = on ? originalColor * 0.7f : originalColor;
    }

    public void CheckVisibility(ChunkWorldGenerator world)
    {
        Vector3Int pos = Vector3Int.RoundToInt(transform.position);

        // 6 yönde de blok varsa bu blok görünmezdir
        bool isCovered = 
            world.HasBlock(pos + Vector3Int.up) &&
            world.HasBlock(pos + Vector3Int.down) &&
            world.HasBlock(pos + Vector3Int.left) &&
            world.HasBlock(pos + Vector3Int.right) &&
            world.HasBlock(pos + Vector3Int.forward) &&
            world.HasBlock(pos + Vector3Int.back);

        if (gameObject.activeSelf == isCovered)
            gameObject.SetActive(!isCovered);
    }
}