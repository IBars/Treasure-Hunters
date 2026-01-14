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
        HandleBuilding();
        HandleHighlight();
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

        Ray ray = Camera.main.ScreenPointToRay(
            new Vector3(Screen.width / 2, Screen.height / 2)
        );

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

        if (inventoryCounts[selectedSlot] <= 0 || slotBlockIDs[selectedSlot] == -1)
            return;

        Ray ray = Camera.main.ScreenPointToRay(
            new Vector3(Screen.width / 2, Screen.height / 2)
        );

        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance))
        {
            Vector3 spawnPos = hit.transform.position + hit.normal;
            GameObject prefab = null;

            int id = slotBlockIDs[selectedSlot];
            if (id == 0) prefab = worldGenerator.grassPrefab;
            else if (id == 1) prefab = worldGenerator.dirtPrefab;
            else if (id == 2) prefab = worldGenerator.stonePrefab;
            else if (id == 3) prefab = worldGenerator.cobblePrefab;

            if (prefab != null)
            {
                GameObject newBlock = Instantiate(prefab, spawnPos, Quaternion.identity);
                worldGenerator.RegisterNewBlock(newBlock, Vector3Int.RoundToInt(spawnPos));
                inventoryCounts[selectedSlot]--;
                UpdateUI();
                UpdateSelectionUI();
            }
        }
    }

    void HandleHighlight()
    {
        Ray ray = Camera.main.ScreenPointToRay(
            new Vector3(Screen.width / 2, Screen.height / 2)
        );

        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance))
        {
            Block b = hit.collider.GetComponent<Block>();
            if (b != null)
            {
                if (lastHighlightedBlock != b)
                {
                    if (lastHighlightedBlock != null)
                        lastHighlightedBlock.Highlight(false);

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
            if (inventoryCounts[i] > 0 && slotBlockIDs[i] != -1)
            {
                slotIcons[i].gameObject.SetActive(true);
                slotIcons[i].enabled = true;
                slotIcons[i].color = Color.white;

                if (slotBlockIDs[i] >= 0 && slotBlockIDs[i] < blockIcons.Length)
                    slotIcons[i].sprite = blockIcons[slotBlockIDs[i]];

                slotTexts[i].text = inventoryCounts[i].ToString();
            }
            else
            {
                slotIcons[i].enabled = false;
                slotTexts[i].text = "";
                if (inventoryCounts[i] <= 0)
                    slotBlockIDs[i] = -1;
            }
        }
    }

    void UpdateSelectionUI()
    {
        for (int i = 0; i < slotIcons.Length; i++)
        {
            Image slotBg = slotIcons[i].transform.parent.GetComponent<Image>();
            if (slotBg != null)
                slotBg.color = (i == selectedSlot)
                    ? new Color(0.3f, 0.3f, 0.3f, 1f)
                    : new Color(0.7f, 0.7f, 0.7f, 1f);
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
