using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PlayerInteraction : MonoBehaviour
{
    public float interactionDistance = 5f;
    public Transform player;
    public ChunkWorldGenerator worldGenerator;

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

    Block lastHighlightedBlock;

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
        
        // Önce etkileşimi (Boyut Bloğu girişi gibi) kontrol et
        // Eğer bir etkileşim gerçekleşirse blok koyma adımını atla
        if (!HandleInteraction()) 
        {
            HandleBuilding();
        }

        HandleHighlight();
    }

    // Geriye bool döndüren yeni sistem: Bir şeye tıkladıysak true döner
    bool HandleInteraction()
    {
        
        if (Input.GetMouseButtonDown(1)) // Sağ Tık
        {
            Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));
            if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance))
            {
                DimensionBlock dimBlock = hit.collider.GetComponentInParent<DimensionBlock>();
                if (dimBlock != null)
                {
                    dimBlock.Interact(player);
                    return true; // Etkileşim oldu, blok koymayı engelle
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
        if (!Input.GetMouseButton(0)) return;

        Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));

        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance))
        {
            Block b = hit.collider.GetComponent<Block>();
            if (b == null) return;

            b.health -= Time.deltaTime * breakSpeed;

            if (b.health <= 0)
            {
                int dropID = (b.blockID == 2) ? 3 : b.blockID;
                AddToInventory(dropID);
                worldGenerator.RemoveBlockManually(hit.collider.gameObject);
            }
        }
    }

    void HandleBuilding()
{
    if (!Input.GetMouseButtonDown(1)) return;

    Ray ray = Camera.main.ScreenPointToRay(
        new Vector3(Screen.width / 2, Screen.height / 2)
    );

    // Etkileşim kontrolü (DimensionBlock)
    if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance))
    {
        DimensionBlock dimBlock = hit.collider.GetComponentInParent<DimensionBlock>();
        if (dimBlock != null)
        {
            dimBlock.Interact(player);
            return;
        }
    }

    // Envanter kontrolü
    if (inventoryCounts[selectedSlot] <= 0 || slotBlockIDs[selectedSlot] == -1)
        return;

    // Blok koyma
    if (Physics.Raycast(ray, out RaycastHit hitBuilding, interactionDistance))
    {
        Vector3 spawnPos = hitBuilding.transform.position + hitBuilding.normal;
        Vector3Int gridPos = Vector3Int.RoundToInt(spawnPos);

        if (Vector3.Distance(spawnPos, player.position) < 0.8f) return;

        GameObject prefab = GetPrefabByID(slotBlockIDs[selectedSlot]);
        if (prefab != null)
        {
            GameObject newBlock = Instantiate(
                prefab,
                (Vector3)gridPos,
                prefab.transform.rotation // 🔥 prefab rotasyonu korunur
            );

            worldGenerator.RegisterNewBlock(newBlock, gridPos);
            inventoryCounts[selectedSlot]--;
            UpdateUI();
        }
    }
}


// Kod kalabalığını önlemek için yardımcı fonksiyon
GameObject GetPrefabByID(int id)
{
    if (id == 0) return worldGenerator.grassPrefab;
    if (id == 1) return worldGenerator.dirtPrefab;
    if (id == 2) return worldGenerator.stonePrefab;
    if (id == 4) return worldGenerator.logPrefab;  // Log: 4
    if (id == 5) return worldGenerator.leafPrefab; // Leaf: 5
    if (id == 8) return worldGenerator.dimensionBlockPrefab; // Senin yeni bloğun
    return null;
}

    void HandleHighlight()
    {
        Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));

        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance))
        {
            Block b = hit.collider.GetComponent<Block>();
            if (b != null)
            {
                if (lastHighlightedBlock != b)
                {
                    if (lastHighlightedBlock != null) lastHighlightedBlock.Highlight(false);
                    lastHighlightedBlock = b;
                    b.Highlight(true);
                }
                return;
            }
        }

        if (lastHighlightedBlock != null)
        {
            lastHighlightedBlock.Highlight(false);
            lastHighlightedBlock = null;
        }
    }

    void AddToInventory(int id)
    {
        for (int i = 0; i < 10; i++)
        {
            if (slotBlockIDs[i] == id)
            {
                inventoryCounts[i]++;
                UpdateUI();
                return;
            }
        }

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
    }

    public void UpdateUI()
{
    for (int i = 0; i < 10; i++)
    {
        // 1. Slot boş mu kontrol et
        if (inventoryCounts[i] <= 0 || slotBlockIDs[i] == -1)
        {
            slotIcons[i].enabled = false;
            slotTexts[i].text = "";
            continue; // Bu slotu atla ve diğerine geç
        }

        // 2. Slot doluysa Image ve Text'i hazırla
        slotIcons[i].enabled = true;
        slotIcons[i].gameObject.SetActive(true);
        slotTexts[i].text = inventoryCounts[i].ToString();

        // 3. SPRITE ÇEKME KISMI (Buraya dikkat)
        int currentID = slotBlockIDs[i];

        // Eğer ID listemizde (blockIcons) bu ID'ye karşılık bir resim varsa bas
        if (currentID >= 0 && currentID < blockIcons.Length)
        {
            if (blockIcons[currentID] != null)
            {
                slotIcons[i].sprite = blockIcons[currentID];
                slotIcons[i].color = Color.white; // Şeffaf kalmış olabilir, beyaza çek
            }
            else
            {
                Debug.LogWarning(currentID + " ID'li blok için Sprite atanmamış!");
                slotIcons[i].enabled = false; // Sprite yoksa resmi kapat ama sayı kalsın
            }
        }
        else
        {
            // ID çok büyükse veya listede yoksa resmi kapat
            slotIcons[i].enabled = false; 
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
            {
                bool show = (slotBlockIDs[selectedSlot] == i && inventoryCounts[selectedSlot] > 0);
                handBlocks[i].SetActive(show);
            }
        }
    }
}