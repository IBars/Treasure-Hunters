using UnityEngine;

public class Block : MonoBehaviour
{
    public int blockID;
    public float health = 1f;

    // SADECE dışarıdan çağrılır
    public void CheckVisibility(ChunkWorldGenerator world)
    {
        Vector3Int pos = Vector3Int.RoundToInt(transform.position);

        bool covered =
            world.HasBlock(pos + Vector3Int.up) &&
            world.HasBlock(pos + Vector3Int.down) &&
            world.HasBlock(pos + Vector3Int.left) &&
            world.HasBlock(pos + Vector3Int.right) &&
            world.HasBlock(pos + new Vector3Int(0, 0, 1)) &&
            world.HasBlock(pos + new Vector3Int(0, 0, -1));

        // gereksiz SetActive çağrısını engelle
        if (gameObject.activeSelf == covered)
            gameObject.SetActive(!covered);
    }
}
