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
        if (!Input.GetMouseButton(0)) return;

        Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));

        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance))
        {
            // Önce optimized block'u dene
            OptimizedBlock optBlock = hit.collider.GetComponent<OptimizedBlock>();
            if (optBlock != null)
            {
                optBlock.health -= Time.deltaTime * breakSpeed;

                if (optBlock.health <= 0)
                {
                    int dropID = (optBlock.blockID == 2) ? 3 : optBlock.blockID;
                    AddToInventory(dropID);
                    worldGenerator.RemoveBlockManually(hit.collider.gameObject);
                }
                return;
            }
            
            // Değilse eski Block'u dene
            Block b = hit.collider.GetComponent<Block>();
            if (b != null)
            {
                b.health -= Time.deltaTime * breakSpeed;

                if (b.health <= 0)
                {
                    int dropID = (b.blockID == 2) ? 3 : b.blockID;
                    AddToInventory(dropID);
                    worldGenerator.RemoveBlockManually(hit.collider.gameObject);
                }
            }
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
            
            // Grass veya Log için özel material sistemi
            if (id == 0) // Grass
            {
                optBlock.Initialize(
                    worldGenerator.grassTopMaterial, 
                    worldGenerator.grassSideMaterial, 
                    worldGenerator.dirtMaterial, 
                    id
                );
            }
            else if (id == 4) // Log
            {
                optBlock.Initialize(
                    worldGenerator.logTopMaterial, 
                    worldGenerator.logSideMaterial, 
                    worldGenerator.logTopMaterial, 
                    id
                );
            }
            else
            {
                Material mat = GetMaterialByID(id);
                if (mat != null)
                {
                    optBlock.Initialize(mat, id);
                }
                else
                {
                    Destroy(newBlock);
                    return;
                }
            }

            worldGenerator.RegisterNewBlock(newBlock, gridPos);
            inventoryCounts[selectedSlot]--;
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
            if (inventoryCounts[i] <= 0 || slotBlockIDs[i] == -1)
            {
                slotIcons[i].enabled = false;
                slotTexts[i].text = "";
                continue;
            }

            slotIcons[i].enabled = true;
            if (slotBlockIDs[i] < blockIcons.Length)
            {
                slotIcons[i].sprite = blockIcons[slotBlockIDs[i]];
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