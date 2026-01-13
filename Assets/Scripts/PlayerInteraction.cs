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
    public int[] slotBlockIDs = new int[10]; // -1: Boş, 0: Grass, 1: Dirt vb.

    [Header("UI Elemanları")]
    public Image[] slotIcons;
    public TextMeshProUGUI[] slotTexts;
    public Sprite[] blockIcons; // Sprite listesi (Grass, Dirt)
    public RectTransform selectionFrame;

    [Header("Elde Tutma Ayarları")]
    public GameObject[] handBlocks; // Eldeki 3D modeller (Grass, Dirt)
    public int selectedSlot = 0;

    void Start()
    {
        // Envanteri boş başlat
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
        // Mouse tekerleği ile geçiş
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            if (scroll > 0f) selectedSlot = (selectedSlot <= 0) ? 9 : selectedSlot - 1;
            else if (scroll < 0f) selectedSlot = (selectedSlot >= 9) ? 0 : selectedSlot + 1;
            UpdateSelectionUI();
        }

        // Sayı tuşları (1-0)
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
    // GetMouseButtonDown yerine GetMouseButton (Basılı tutma)
    if (Input.GetMouseButton(0)) 
    {
        Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));
        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance))
        {
            Block b = hit.collider.GetComponent<Block>();
            if (b != null)
            {
                // Time.deltaTime kullanarak zamanla can azaltıyoruz
                // 2.0f değerini artırırsan daha zor kırılır
                b.health -= Time.deltaTime * 2.0f; 

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
            // Elimizde blok var mı kontrolü
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
                    UpdateSelectionUI(); // Elindeki blok biterse gizlemek için
                }
            }
        }
    }

    void AddToInventory(int id)
    {
        // Önce aynı tipten var mı bak
        for (int i = 0; i < 10; i++)
        {
            if (slotBlockIDs[i] == id)
            {
                inventoryCounts[i]++;
                UpdateUI();
                return;
            }
        }

        // Yoksa boş slot bul
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

    void UpdateUI()
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
                slotBlockIDs[i] = -1;
            }
        }
    }

    // ... (scriptin üst kısmı aynı kalacak)

    void UpdateSelectionUI()
{
    // 1. Slotların renklerini sıfırla ve seçili olanı koyulaştır
    for (int i = 0; i < slotIcons.Length; i++)
    {
        // Slotun arkaplan resmine (parent) ulaşıp rengini değiştiriyoruz
        Image slotBg = slotIcons[i].transform.parent.GetComponent<Image>();
        
        if (i == selectedSlot)
        {
            slotBg.color = new Color(0.5f, 0.5f, 0.5f, 1f); // Seçili olanı koyulaştır (Gri yap)
        }
        else
        {
            slotBg.color = Color.white; // Diğerlerini normal (Beyaz/Açık) bırak
        }
    }

    // 2. Elindeki 3D modelleri göster/gizle
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