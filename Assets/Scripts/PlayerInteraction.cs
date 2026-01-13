using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public class PlayerInteraction : MonoBehaviour
{
    public ChunkWorldGenerator worldGen;
    public float range = 5f;

    [Header("Envanter Ayarları")]
    public List<GameObject> blockPrefabs; // Blokların prefabları (0: Grass, 1: Dirt vb.)
    public Sprite[] blockIcons;           // Senin yaptığın şeffaf Sprite'lar (ID sırasına göre)
    
    [HideInInspector] public int[] inventoryCounts; 
    [HideInInspector] public int[] slotBlockIDs;    
    public int selectedSlot = 0;

    [Header("UI Ayarları (Slot Sayısı Kadar Sürükle)")]
    public TextMeshProUGUI[] slotTexts; 
    public Image[] slotIcons;            

    private float breakTimer = 0f;

    void Awake()
    {
        // Kaç tane slot sürüklediysen envanter o kadar büyük olur (Hata önleyici)
        int slotCount = slotTexts.Length;
        
        // Eğer slot sürüklemeyi unuttuysan kodun çökmesini engelle
        if (slotCount == 0) slotCount = 10; 

        inventoryCounts = new int[slotCount];
        slotBlockIDs = new int[slotCount];

        for (int i = 0; i < slotCount; i++)
        {
            slotBlockIDs[i] = -1;
            inventoryCounts[i] = 0;
        }
    }

    void Update()
    {
        HandleSlotSelection();
        HandleInteraction();
        UpdateUI();
    }

    void HandleSlotSelection()
    {
        for (int i = 0; i < slotTexts.Length; i++)
        {
            KeyCode key = (i == 9) ? KeyCode.Alpha0 : KeyCode.Alpha1 + i;
            if (Input.GetKeyDown(key)) selectedSlot = i;
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll < 0f) selectedSlot = (selectedSlot + 1) % slotTexts.Length;
        else if (scroll > 0f) selectedSlot = (selectedSlot == 0) ? slotTexts.Length - 1 : selectedSlot - 1;
    }

    void HandleInteraction()
    {
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, range))
        {
            if (Input.GetMouseButton(0))
            {
                Block target = hit.collider.GetComponent<Block>();
                if (target != null)
                {
                    breakTimer += Time.deltaTime;
                    if (breakTimer >= target.health)
                    {
                        AddToInventory(target.blockID);
                        worldGen.RemoveBlockManually(hit.collider.gameObject);
                        breakTimer = 0;
                    }
                }
            }
            else { breakTimer = 0; }

            if (Input.GetMouseButtonDown(1))
            {
                if (selectedSlot < slotBlockIDs.Length)
                {
                    int currentBlockID = slotBlockIDs[selectedSlot];
                    if (currentBlockID != -1 && inventoryCounts[selectedSlot] > 0)
                    {
                        PlaceBlock(currentBlockID);
                    }
                }
            }
        }
        else { breakTimer = 0; }
    }

    void AddToInventory(int blockID)
    {
        for (int i = 0; i < slotBlockIDs.Length; i++)
        {
            if (slotBlockIDs[i] == blockID)
            {
                inventoryCounts[i]++;
                return;
            }
        }

        for (int i = 0; i < slotBlockIDs.Length; i++)
        {
            if (slotBlockIDs[i] == -1)
            {
                slotBlockIDs[i] = blockID;
                inventoryCounts[i] = 1;
                return;
            }
        }
    }

    void PlaceBlock(int blockID)
    {
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out RaycastHit hit, range))
        {
            Vector3 spawnPos = hit.transform.position + hit.normal;
            Vector3Int finalPos = Vector3Int.RoundToInt(spawnPos);

            GameObject newBlock = Instantiate(blockPrefabs[blockID], (Vector3)finalPos, Quaternion.identity);
            Block b = newBlock.GetComponent<Block>() ?? newBlock.AddComponent<Block>();
            b.blockID = blockID;
            b.health = 0.6f;

            worldGen.RegisterNewBlock(newBlock, finalPos);
            
            inventoryCounts[selectedSlot]--;
            if (inventoryCounts[selectedSlot] <= 0)
            {
                slotBlockIDs[selectedSlot] = -1;
            }
        }
    }

    void UpdateUI()
    {
        // Dizinin gerçek uzunluğu kadar dön (Index hatasını bitiren yer burası)
        for (int i = 0; i < slotTexts.Length; i++)
        {
            if (slotTexts[i] == null) continue;

            // Sayı Yazısı
            slotTexts[i].text = (inventoryCounts[i] > 0) ? inventoryCounts[i].ToString() : "";

            // İkon (Sprite)
            if (i < slotIcons.Length && slotIcons[i] != null)
            {
                int idInSlot = slotBlockIDs[i];
                if (idInSlot != -1 && idInSlot < blockIcons.Length && blockIcons[idInSlot] != null)
                {
                    slotIcons[i].sprite = blockIcons[idInSlot];
                    slotIcons[i].color = Color.white;
                }
                else
                {
                    slotIcons[i].sprite = null;
                    slotIcons[i].color = new Color(0, 0, 0, 0);
                }
            }

            // Seçili Slot Görseli
            Image slotBg = slotTexts[i].transform.parent.GetComponent<Image>();
            if (slotBg != null)
            {
                slotBg.color = (i == selectedSlot) ? new Color(0.1f, 0.1f, 0.1f, 0.8f) : new Color(0.3f, 0.3f, 0.3f, 0.4f);
            }
        }
    }
}