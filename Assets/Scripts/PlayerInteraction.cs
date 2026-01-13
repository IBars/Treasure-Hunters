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
    public int[] slotBlockIDs = new int[10]; // -1: Boş, 0: Grass, 1: Dirt

    [Header("UI Elemanları")]
    public Image[] slotIcons;
    public TextMeshProUGUI[] slotTexts;
    public Sprite[] blockIcons; 

    [Header("Elde Tutma ve Seçim")]
    public GameObject[] handBlocks; // Eldeki 3D modeller
    public int selectedSlot = 0;
    public float breakSpeed = 2.0f; // Blok kırma hızı (Yüksek sayı = Daha hızlı)

    void Start()
    {
        for (int i = 0; i < 10; i++)
        {
            slotBlockIDs[i] = -1;
        }
        UpdateUI();
        UpdateSelectionUI();
    }

    void Update()
    {
        HandleSelection();
        HandleMining();
        HandleBuilding();
    }

    void HandleSelection()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            if (scroll > 0f) selectedSlot = (selectedSlot <= 0) ? 9 : selectedSlot - 1;
            else if (scroll < 0f) selectedSlot = (selectedSlot >= 9) ? 0 : selectedSlot + 1;
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
        // GetMouseButton(0) - Basılı tutulduğu sürece çalışır
        if (Input.GetMouseButton(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));
            if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance))
            {
                Block b = hit.collider.GetComponent<Block>();
                if (b != null)
                {
                    // Bloğa zamanla hasar ver
                    b.health -= Time.deltaTime * breakSpeed;

                    if (b.health <= 0)
                    {
                        AddToInventory(b.blockID);
                        worldGenerator.RemoveBlockManually(hit.collider.gameObject);
                    }
                }
            }
        }
    }

    void HandleBuilding()
    {
        if (Input.GetMouseButtonDown(1)) // Sağ Tık: Koyma
        {
            if (inventoryCounts[selectedSlot] > 0 && slotBlockIDs[selectedSlot] != -1)
            {
                Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));
                if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance))
                {
                    Vector3 spawnPos = hit.transform.position + hit.normal;
                    GameObject prefab = (slotBlockIDs[selectedSlot] == 0) ? worldGenerator.grassPrefab : worldGenerator.dirtPrefab;
                    
                    GameObject newBlock = Instantiate(prefab, spawnPos, Quaternion.identity);
                    worldGenerator.RegisterNewBlock(newBlock, Vector3Int.RoundToInt(spawnPos));
                    
                    inventoryCounts[selectedSlot]--;
                    UpdateUI();
                    UpdateSelectionUI(); 
                }
            }
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
            if (inventoryCounts[i] > 0)
            {
                slotIcons[i].enabled = true;
                slotIcons[i].sprite = blockIcons[slotBlockIDs[i]];
                slotTexts[i].text = inventoryCounts[i].ToString();
            }
            else
            {
                slotIcons[i].enabled = false;
                slotTexts[i].text = "";
                // Eğer miktar 0 ise ID'yi sıfırla ki slot boşalsın
                if(inventoryCounts[i] <= 0) slotBlockIDs[i] = -1;
            }
        }
    }

    void UpdateSelectionUI()
    {
        // 1. Renk Değişimi (Seçili koyu gri, diğerleri açık gri)
        for (int i = 0; i < slotIcons.Length; i++)
        {
            Image slotBg = slotIcons[i].transform.parent.GetComponent<Image>();
            if (i == selectedSlot)
                slotBg.color = new Color(0.3f, 0.3f, 0.3f, 1f); // Koyu Gri
            else
                slotBg.color = new Color(0.7f, 0.7f, 0.7f, 1f); // Açık Gri
        }

        // 2. Eldeki 3D Model Kontrolü
        for (int i = 0; i < handBlocks.Length; i++)
        {
            if (handBlocks[i] != null)
            {
                bool shouldShow = (slotBlockIDs[selectedSlot] == i && inventoryCounts[selectedSlot] > 0);
                handBlocks[i].SetActive(shouldShow);
            }
        }
    }
}