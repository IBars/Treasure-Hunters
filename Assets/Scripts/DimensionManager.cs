using UnityEngine;

public class DimensionManager : MonoBehaviour
{
    // Singleton yapısı: Diğer scriptlerin bu scripti kolayca bulmasını sağlar
    public static DimensionManager Instance { get; private set; }

    public GameObject floorPrefab;
    public GameObject wallPrefab;

    private void Awake()
    {
        // Eğer sahnede başka bir Instance varsa onu sil, bunu tut
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }

    public void CreateEmptyRoom(Vector3 center)
{
    int outerSize = 18;
    int halfSize = outerSize / 2;

    for (int x = -halfSize; x < halfSize; x++)
    {
        for (int z = -halfSize; z < halfSize; z++)
        {
            // ZEMİN (Y: -1)
            Instantiate(floorPrefab, center + new Vector3(x, -1, z), floorPrefab.transform.rotation);
            
            // TAVAN (Y: 16)
            Instantiate(floorPrefab, center + new Vector3(x, 16, z), floorPrefab.transform.rotation);

            // DUVARLAR
            if (x == -halfSize || x == halfSize - 1 || z == -halfSize || z == halfSize - 1)
            {
                for (int y = 0; y < 16; y++)
                {
                    Instantiate(wallPrefab, center + new Vector3(x, y, z), wallPrefab.transform.rotation);
                }
            }
        }
    }
}
}