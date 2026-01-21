using UnityEngine;

public class WaterBlock : MonoBehaviour
{
    public float checkInterval = 0.5f; // Ne kadar sıklıkla altını kontrol etsin?
    private bool isFlowing = false;

    void Start()
    {
        // Periyodik olarak kontrol etmeye başla (Coroutine kullanarak performansı koruyoruz)
        InvokeRepeating("CheckBelow", 0.1f, checkInterval);
    }

    void CheckBelow()
    {
        Vector3Int myPos = Vector3Int.RoundToInt(transform.position);
        Vector3Int belowPos = myPos + Vector3Int.down;

        // 1. Alt sınır kontrolü
        if (belowPos.y < 0) return;

        // 2. Altı boş mu kontrol et
        if (!ChunkWorldGenerator.Instance.HasBlock(belowPos))
        {
            // Eğer altı boşsa ve henüz oraya su göndermediysek
            ChunkWorldGenerator.Instance.RegisterNewWater(belowPos);
            // Debug.Log("Altı boşaldı, su akıyor: " + belowPos);
        }
    }
}