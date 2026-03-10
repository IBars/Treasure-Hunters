using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UpdatedPlayerInteraction : MonoBehaviour
{
    public float interactionDistance = 5f;
    public Transform player;
    public OptimizedChunkWorldGenerator worldGenerator;

    [Header("Envanter Verileri")]
    public int[] inventoryCounts = new int[10];
    public int[] slotBlockIDs = new int[10];

    [Header("UI Elemanları")]
    public Image[] slotIcons;
    public TextMeshProUGUI[] slotTexts;
    public Sprite[] blockIcons;

    [Header("Elde Tutma ve Seçim")]
    public GameObject[] handBlocks;
    public int selectedSlot = 0;
    public float breakSpeed = 2.0f;

    private GameObject lastHighlightedBlockObj;
    private OptimizedBlock lastHighlightedOptBlock;
    private Block lastHighlightedBlock;
    
    // Mining için geçici değişkenler
    private GameObject currentMiningBlock;
    private float currentMiningTime = 0f;

    void Start()
    {
        for (int i = 0; i < 10; i++)
            slotBlockIDs[i] = -1;

        UpdateUI();
        UpdateSelectionUI();
    }

    void Update()
    {
        HandleSelection();
        HandleMining();
        
        if (!HandleInteraction()) 
        {
            HandleBuilding();
        }

        HandleHighlight();
    }

    bool HandleInteraction()
    {
        if (Input.GetMouseButtonDown(1))
        {
            Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));
            if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance))
            {
                DimensionBlock dimBlock = hit.collider.GetComponentInParent<DimensionBlock>();
                if (dimBlock != null)
                {
                    dimBlock.Interact(player);
                    return true;
                }
            }
        }
        return false;
    }

    void HandleSelection()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            selectedSlot = scroll > 0
                ? (selectedSlot <= 0 ? 9 : selectedSlot - 1)
                : (selectedSlot >= 9 ? 0 : selectedSlot + 1);

            UpdateSelectionUI();
        }

        for (int i = 0; i < 10; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                selectedSlot = i;
                UpdateSelectionUI();
            }
        }
    }

    void HandleMining()
    {
        if (!Input.GetMouseButton(0))
        {
            // Mouse bırakıldı - mining sıfırla
            currentMiningBlock = null;
            currentMiningTime = 0f;
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));

        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance))
        {
            GameObject hitBlock = hit.collider.gameObject;
            
            // Farklı bloğa geçildiyse sıfırla
            if (currentMiningBlock != hitBlock)
            {
                currentMiningBlock = hitBlock;
                currentMiningTime = 0f;
            }
            
            // OptimizedBlock kontrolü
            OptimizedBlock optBlock = hitBlock.GetComponent<OptimizedBlock>();
            if (optBlock != null)
            {
                currentMiningTime += Time.deltaTime * breakSpeed;
                
                // Health'e göre kır
                if (currentMiningTime >= optBlock.health)
                {
                    int dropID = (optBlock.blockID == 2) ? 3 : optBlock.blockID;
                    AddToInventory(dropID);
                    worldGenerator.RemoveBlockManually(hitBlock);
                    
                    currentMiningBlock = null;
                    currentMiningTime = 0f;
                }
                return;
            }
            
            // Eski Block kontrolü
            Block b = hitBlock.GetComponent<Block>();
            if (b != null)
            {
                currentMiningTime += Time.deltaTime * breakSpeed;
                
                if (currentMiningTime >= b.health)
                {
                    int dropID = (b.blockID == 2) ? 3 : b.blockID;
                    AddToInventory(dropID);
                    worldGenerator.RemoveBlockManually(hitBlock);
                    
                    currentMiningBlock = null;
                    currentMiningTime = 0f;
                }
            }
        }
        else
        {
            // Hiçbir şeye bakmıyorsa sıfırla
            currentMiningBlock = null;
            currentMiningTime = 0f;
        }
    }

    void HandleBuilding()
{
    if (!Input.GetMouseButtonDown(1)) return;

    Ray ray = Camera.main.ScreenPointToRay(
        new Vector3(Screen.width / 2, Screen.height / 2)
    );

    if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance))
    {
        DimensionBlock dimBlock = hit.collider.GetComponentInParent<DimensionBlock>();
        if (dimBlock != null)
        {
            dimBlock.Interact(player);
            return;
        }
    }

    if (inventoryCounts[selectedSlot] <= 0 || slotBlockIDs[selectedSlot] == -1)
        return;

    if (Physics.Raycast(ray, out RaycastHit hitBuilding, interactionDistance))
    {
        Vector3 spawnPos = hitBuilding.transform.position + hitBuilding.normal;
        Vector3Int gridPos = Vector3Int.RoundToInt(spawnPos);

        if (Vector3.Distance(spawnPos, player.position) < 0.8f) return;

        int id = slotBlockIDs[selectedSlot];
        GameObject newBlock = new GameObject($"Block_{id}");
        newBlock.transform.position = (Vector3)gridPos;

        OptimizedBlock optBlock = newBlock.AddComponent<OptimizedBlock>();

        if (id == 0)
            optBlock.Initialize(worldGenerator.grassTopMaterial, worldGenerator.grassSideMaterial, worldGenerator.dirtMaterial, id);
        else if (id == 4)
            optBlock.Initialize(worldGenerator.logTopMaterial, worldGenerator.logSideMaterial, worldGenerator.logTopMaterial, id);
        else
        {
            Material mat = GetMaterialByID(id);
            if (mat != null) optBlock.Initialize(mat, id);
            else { Destroy(newBlock); return; }
        }

        worldGenerator.RegisterNewBlock(newBlock, gridPos);
        inventoryCounts[selectedSlot]--;

        // ✅ FIX 1: Slot boşaldıysa ID'yi sıfırla
        if (inventoryCounts[selectedSlot] <= 0)
        {
            inventoryCounts[selectedSlot] = 0;
            slotBlockIDs[selectedSlot] = -1;
        }

        UpdateUI();
    }
}



    Material GetMaterialByID(int id)
    {
        // Grass ve Log için özel yok, çünkü HandleBuilding'de hallettik
        if (id == 1) return worldGenerator.dirtMaterial;
        if (id == 2) return worldGenerator.stoneMaterial;
        if (id == 3) return worldGenerator.cobbleMaterial;
        if (id == 5) return worldGenerator.leafMaterial;
        if (id == 6) return worldGenerator.sandMaterial;
        if (id == 9) return worldGenerator.cactusMaterial;
        return null;
    }

    void HandleHighlight()
    {
        Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));

        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance))
        {
            OptimizedBlock optBlock = hit.collider.GetComponent<OptimizedBlock>();
            if (optBlock != null)
            {
                if (lastHighlightedBlockObj != hit.collider.gameObject)
                {
                    ClearHighlight();
                    lastHighlightedBlockObj = hit.collider.gameObject;
                    lastHighlightedOptBlock = optBlock;
                    optBlock.Highlight(true);
                }
                return;
            }
            
            Block b = hit.collider.GetComponent<Block>();
            if (b != null)
            {
                if (lastHighlightedBlockObj != hit.collider.gameObject)
                {
                    ClearHighlight();
                    lastHighlightedBlockObj = hit.collider.gameObject;
                    lastHighlightedBlock = b;
                    b.Highlight(true);
                }
                return;
            }
        }

        ClearHighlight();
    }
    
    void ClearHighlight()
    {
        if (lastHighlightedOptBlock != null)
        {
            lastHighlightedOptBlock.Highlight(false);
            lastHighlightedOptBlock = null;
        }
        
        if (lastHighlightedBlock != null)
        {
            lastHighlightedBlock.Highlight(false);
            lastHighlightedBlock = null;
        }
        
        lastHighlightedBlockObj = null;
    }

    void AddToInventory(int id)
    {
        // Önce aynı tipte slot var mı bak
        for (int i = 0; i < 10; i++)
        {
            if (slotBlockIDs[i] == id)
            {
                inventoryCounts[i]++;
                UpdateUI();
                return;
            }
        }

        // Boş slot bul - EN SOLDAN başla
        for (int i = 0; i < 10; i++)
        {
            if (slotBlockIDs[i] == -1)
            {
                slotBlockIDs[i] = id;
                inventoryCounts[i] = 1;
                UpdateUI();
                return;
            }
        }
        
        Debug.Log("Envanter dolu!");
    }

    public void UpdateUI()
{
    for (int i = 0; i < 10; i++)
    {
        if (inventoryCounts[i] <= 0 || slotBlockIDs[i] == -1)
        {
            slotIcons[i].sprite = null;
            slotIcons[i].enabled = false;
            slotTexts[i].text = "";
            continue;
        }

        int id = slotBlockIDs[i];

        // ✅ FIX 2: Sprite'ı ata ve rengi tam opak yap
        if (id >= 0 && id < blockIcons.Length && blockIcons[id] != null)
        {
            slotIcons[i].sprite = blockIcons[id];
            slotIcons[i].color = Color.white; // alpha'yı sıfırla
            slotIcons[i].enabled = true;
            slotTexts[i].text = inventoryCounts[i].ToString();
        }
        else
        {
            Debug.LogWarning($"Block ID {id} için icon bulunamadı! blockIcons dizisi Inspector'da doğru atandı mı?");
            slotIcons[i].enabled = false;
            slotTexts[i].text = inventoryCounts[i].ToString();
        }
    }
}

    void UpdateSelectionUI()
    {
        for (int i = 0; i < slotIcons.Length; i++)
        {
            Image slotBg = slotIcons[i].transform.parent.GetComponent<Image>();
            if (slotBg != null)
                slotBg.color = (i == selectedSlot) ? new Color(0.3f, 0.3f, 0.3f, 1f) : new Color(0.7f, 0.7f, 0.7f, 1f);
        }

        for (int i = 0; i < handBlocks.Length; i++)
        {
            if (handBlocks[i] != null)
                handBlocks[i].SetActive(i == selectedSlot);
        }
    }
}