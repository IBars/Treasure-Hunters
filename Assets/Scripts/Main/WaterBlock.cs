using UnityEngine;
public class WaterBlock : MonoBehaviour
{
    public float checkInterval = 0.5f;
    private bool isFlowing = false;
    
    void Start()
    {
        InvokeRepeating("CheckBelow", 0.1f, checkInterval);
    }
    
    void CheckBelow()
    {
        Vector3Int myPos = Vector3Int.RoundToInt(transform.position);
        Vector3Int belowPos = myPos + Vector3Int.down;
        
        // 1. Alt sınır kontrolü
        if (belowPos.y < 0) return;
        
        // 2. Altı boş mu kontrol et
        if (OptimizedChunkWorldGenerator.Instance != null && 
            !OptimizedChunkWorldGenerator.Instance.HasBlock(belowPos))
        {
            OptimizedChunkWorldGenerator.Instance.RegisterNewWater(belowPos);
        }
    }
}