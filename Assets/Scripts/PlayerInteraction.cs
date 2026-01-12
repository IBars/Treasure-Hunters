using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class PlayerInteraction : MonoBehaviour
{
    public ChunkWorldGenerator worldGen;
    public float range = 5f;

    [Header("Envanter Ayarları")]
    public List<GameObject> blockPrefabs = new List<GameObject>(); 
    public int[] inventoryCounts = new int[10]; 
    public int selectedSlot = 0;

    [Header("UI Ayarları")]
    public TextMeshProUGUI[] slotTexts; 

    private float breakTimer = 0f;

    void Update()
    {
        HandleSlotSelection();
        HandleInteraction();
        UpdateUI();
    }

    void HandleSlotSelection()
    {
        // Sayı tuşları ile seçim (1, 2, 3 ... 9, 0)
        for (int i = 0; i < 10; i++)
        {
            KeyCode key = (i == 9) ? KeyCode.Alpha0 : KeyCode.Alpha1 + i;
            if (Input.GetKeyDown(key))
            {
                selectedSlot = i;
            }
        }

        // --- MOUSE SCROLL YÖNÜ DÜZELTİLDİ ---
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        
        if (scroll < 0f) // Aşağı Kaydırma: Sağa git (+)
        {
            selectedSlot = (selectedSlot + 1) % 10;
        }
        else if (scroll > 0f) // Yukarı Kaydırma: Sola git (-)
        {
            selectedSlot = (selectedSlot == 0) ? 9 : selectedSlot - 1;
        }
    }

    void HandleInteraction()
    {
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, range))
        {
            // --- SOL TIK: KIRMA ---
            if (Input.GetMouseButton(0))
            {
                Block target = hit.collider.GetComponent<Block>();
                if (target != null)
                {
                    breakTimer += Time.deltaTime;
                    if (breakTimer >= target.health)
                    {
                        if (target.blockID < inventoryCounts.Length)
                        {
                            inventoryCounts[target.blockID]++;
                        }
                        worldGen.RemoveBlockManually(hit.collider.gameObject);
                        breakTimer = 0;
                    }
                }
            }
            else { breakTimer = 0; }

            // --- SAĞ TIK: KOYMA ---
            if (Input.GetMouseButtonDown(1))
            {
                if (selectedSlot < inventoryCounts.Length && inventoryCounts[selectedSlot] > 0)
                {
                    if (selectedSlot < blockPrefabs.Count && blockPrefabs[selectedSlot] != null)
                    {
                        Vector3 spawnPos = hit.transform.position + hit.normal;
                        Vector3Int finalPos = Vector3Int.RoundToInt(spawnPos);

                        GameObject newBlock = Instantiate(blockPrefabs[selectedSlot], (Vector3)finalPos, Quaternion.identity);
                        
                        Block b = newBlock.GetComponent<Block>() ?? newBlock.AddComponent<Block>();
                        b.blockID = selectedSlot; 
                        b.blockName = blockPrefabs[selectedSlot].name;
                        b.health = 0.6f;

                        worldGen.RegisterNewBlock(newBlock, finalPos);
                        inventoryCounts[selectedSlot]--;
                    }
                }
            }
        }
        else { breakTimer = 0; }
    }

    void UpdateUI()
    {
        int limit = Mathf.Min(inventoryCounts.Length, slotTexts.Length);

        for (int i = 0; i < limit; i++)
        {
            if (slotTexts[i] != null)
            {
                slotTexts[i].text = inventoryCounts[i].ToString();
                
                var slotImage = slotTexts[i].transform.parent.GetComponent<UnityEngine.UI.Image>();
                
                if (slotImage != null)
                {
                    if (i == selectedSlot)
                    {
                        // SEÇİLİ SLOT: Koyu Gri Şeffaf
                        slotImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
                    }
                    else
                    {
                        // DİĞER SLOTLAR: Açık Gri Daha Şeffaf
                        slotImage.color = new Color(0.3f, 0.3f, 0.3f, 0.4f);
                    }
                }
            }
        }
    }
}