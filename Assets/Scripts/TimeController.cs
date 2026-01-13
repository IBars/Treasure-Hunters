using UnityEngine;

public class TimeController : MonoBehaviour
{
    [Header("Skybox Listesi")]
    public Material[] skyboxes; // 5 adet skybox'ı buraya sürükle
    
    [Header("Güneş Ayarları")]
    public Light sunLight; // Sahnedeki Directional Light
    
    [Header("Zaman Ayarları")]
    [Range(0, 24)] public float currentTime = 12f; // 0-24 arası saat
    public float daySpeed = 0.1f; // Günün akış hızı

    void Update()
    {
        // Zamanı ilerlet
        currentTime += Time.deltaTime * daySpeed;
        if (currentTime >= 24) currentTime = 0;

        UpdateEnvironment();
    }

    void UpdateEnvironment()
    {
        // Basitçe saate göre skybox seçimi (5 adet olduğu için)
        // 0-4: Gece, 5-9: Sabah, 10-15: Öğle, 16-19: Akşamüstü, 20-23: Akşam
        int index = Mathf.FloorToInt((currentTime / 24f) * skyboxes.Length);
        
        if (RenderSettings.skybox != skyboxes[index])
        {
            RenderSettings.skybox = skyboxes[index];
        }

        // Güneşi döndür (Güneşin doğup batması için)
        // Saat 12'de güneş tam tepede (90 derece) olsun diye:
        float sunAngle = (currentTime / 24f) * 360f - 90f;
        sunLight.transform.localRotation = Quaternion.Euler(sunAngle, 170f, 0f);

        // Gece olunca ışığı kıs
        sunLight.intensity = (currentTime > 6 && currentTime < 18) ? 1f : 0.1f;
        
        // Skybox değiştikten sonra yansımaları güncelle (Önemli!)
        DynamicGI.UpdateEnvironment();
    }
}