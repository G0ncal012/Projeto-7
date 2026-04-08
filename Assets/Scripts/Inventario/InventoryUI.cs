using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] private KeyCode toggleKey = KeyCode.Tab;

    public static bool IsOpen { get; private set; } = false;
    public static int SelectedHotbarSlot { get; private set; } = 0;

    private GameObject inventoryPanel;

    private SlotUI[] hotbarSlots       = new SlotUI[InventorySystem.HotbarSlots];
    private SlotUI[] invSlots          = new SlotUI[InventorySystem.InventorySlots];
    private SlotUI[] panelHotbarSlots  = new SlotUI[InventorySystem.HotbarSlots];

    private readonly Color cBg        = new Color(0.08f, 0.08f, 0.08f, 0.97f);
    private readonly Color cSlot      = new Color(0.18f, 0.18f, 0.18f, 1.00f);
    private readonly Color cSlotFull  = new Color(0.20f, 0.40f, 0.20f, 1.00f);
    private readonly Color cBorder    = new Color(0.35f, 0.35f, 0.35f, 1.00f);
    private readonly Color cSelected  = new Color(0.70f, 0.55f, 0.05f, 1.00f);
    private readonly Color cDrag      = new Color(0.40f, 0.60f, 0.40f, 1.00f);

    private const float SlotSize = 58f;
    private const float SlotGap  = 5f;

    private SlotUI dragSource = null;
    private GameObject dragIcon = null;
    private Canvas canvas;

    private AxeTool axeTool;
    private PickaxeTool pickaxeTool;
    private BuildingManager buildingManager;
    private TextMeshProUGUI weightLabel;

    private class SlotUI
    {
        public GameObject root;
        public Image bg, border;
        public TextMeshProUGUI qty, itemName;
        public bool isHotbar;
        public int index;
    }

    void Start()
    {
        canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindAnyObjectByType<Canvas>();

        axeTool = FindAnyObjectByType<AxeTool>();
        pickaxeTool = FindAnyObjectByType<PickaxeTool>();
        buildingManager = FindAnyObjectByType<BuildingManager>();

        BuildHotbar();
        BuildInventoryPanel();
        inventoryPanel.SetActive(false);

        if (InventorySystem.Instance != null)
            InventorySystem.Instance.OnInventoryChanged += RefreshUI;

        UpdateEquippedItem();
    }

    void OnDestroy()
    {
        if (InventorySystem.Instance != null)
            InventorySystem.Instance.OnInventoryChanged -= RefreshUI;
    }

    void Update()
    {
        // Refs podem aparecer depois (player gerado em runtime)
        if (axeTool == null) axeTool = FindAnyObjectByType<AxeTool>();
        if (pickaxeTool == null) pickaxeTool = FindAnyObjectByType<PickaxeTool>();
        if (buildingManager == null) buildingManager = FindAnyObjectByType<BuildingManager>();

        // Abre/fecha inventário
        if (Input.GetKeyDown(toggleKey))
            ToggleInventory();

        if (IsOpen)
        {
            HandleDrag();
            return; // não processa scroll/números com inventário aberto
        }

        // Scroll da hotbar
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            int dir = scroll > 0f ? -1 : 1;
            SelectSlot((SelectedHotbarSlot + dir + InventorySystem.HotbarSlots) % InventorySystem.HotbarSlots);
        }

        // Números 1-5
        for (int i = 0; i < InventorySystem.HotbarSlots; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                SelectSlot(i);
                break;
            }
        }
    }

    private void SelectSlot(int index)
    {
        SelectedHotbarSlot = index;
        RefreshHotbarSelection();
        UpdateEquippedItem();
    }

    // Chamado pelo BuildingManager após colocar um objeto
    public static void ConsumeEquippedItem()
    {
        if (InventorySystem.Instance == null) return;
        var inv = InventorySystem.Instance;
        var stack = inv.hotbar[SelectedHotbarSlot];
        if (stack == null) return;

        stack.quantity--;
        if (stack.quantity <= 0)
        {
            inv.hotbar[SelectedHotbarSlot] = null;
            // Desativa construção se ficou sem items
            FindAnyObjectByType<BuildingManager>()?.DeactivateBuildMode();
            FindAnyObjectByType<AxeTool>()?.SetAxeActive(false);
        }
        inv.NotifyChanged();
    }

    private void UpdateEquippedItem()
    {
        if (InventorySystem.Instance == null) return;

        var stack = InventorySystem.Instance.hotbar[SelectedHotbarSlot];
        string itemName = stack != null ? stack.itemName : "";

        // Machado
        if (axeTool != null)
            axeTool.SetAxeActive(itemName == "Machado");

        // Picareta
        if (pickaxeTool != null)
            pickaxeTool.SetPickaxeActive(itemName == "Picareta");

        // Construção
        if (buildingManager != null)
        {
            if (itemName == "Floor")
                ActivateBuilding(SelectedBuildingType.floor);
            else if (itemName == "Wall")
                ActivateBuilding(SelectedBuildingType.wall);
            else
                buildingManager.DeactivateBuildMode();
        }
    }

    private void ActivateBuilding(SelectedBuildingType type)
    {
        var slots = buildingManager.GetHotbarSlots();
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].buildType == type)
            {
                buildingManager.SelectHotbarSlot(i);
                return;
            }
        }
    }

    private void ToggleInventory()
    {
        IsOpen = !IsOpen;
        inventoryPanel.SetActive(IsOpen);
        Cursor.lockState = IsOpen ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible   = IsOpen;
        if (IsOpen) RefreshUI();
    }

    // ── Drag & Drop ──────────────────────────────────────────────────────────

    private void HandleDrag()
    {
        if (Input.GetMouseButtonDown(0) && dragSource == null)
        {
            SlotUI slot = GetSlotUnderMouse();
            if (slot != null && GetStack(slot) != null)
            {
                dragSource = slot;
                CreateDragIcon(GetStack(slot).itemName);
            }
        }

        if (dragIcon != null)
            dragIcon.transform.position = Input.mousePosition;

        if (Input.GetMouseButtonUp(0) && dragSource != null)
        {
            SlotUI target = GetSlotUnderMouse();
            if (target != null && target != dragSource)
                SwapSlots(dragSource, target);
            DestroyDragIcon();
            dragSource = null;
            RefreshUI();
            UpdateEquippedItem();
        }
    }

    private void CreateDragIcon(string name)
    {
        dragIcon = new GameObject("DragIcon");
        dragIcon.transform.SetParent(canvas.transform, false);
        RectTransform rt = dragIcon.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(SlotSize, SlotSize);
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        Image img = dragIcon.AddComponent<Image>();
        img.color = cDrag; img.raycastTarget = false;
        GameObject tGo = new GameObject("T");
        tGo.transform.SetParent(dragIcon.transform, false);
        RectTransform trt = tGo.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
        TextMeshProUGUI tmp = tGo.AddComponent<TextMeshProUGUI>();
        tmp.text = name; tmp.fontSize = 8f;
        tmp.color = Color.white; tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
    }

    private void DestroyDragIcon()
    {
        if (dragIcon != null) { Destroy(dragIcon); dragIcon = null; }
    }

    private SlotUI GetSlotUnderMouse()
    {
        PointerEventData ped = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(ped, results);
        foreach (var r in results)
        {
            foreach (var s in hotbarSlots)
                if (s != null && r.gameObject == s.root) return s;
            foreach (var s in panelHotbarSlots)
                if (s != null && r.gameObject == s.root) return s;
            foreach (var s in invSlots)
                if (s != null && r.gameObject == s.root) return s;
        }
        return null;
    }

    private void SwapSlots(SlotUI a, SlotUI b)
    {
        var tmp = GetStack(a);
        SetStack(a, GetStack(b));
        SetStack(b, tmp);
    }

    private InventorySystem.ItemStack GetStack(SlotUI slot)
    {
        var inv = InventorySystem.Instance;
        if (inv == null) return null;
        return slot.isHotbar ? inv.hotbar[slot.index] : inv.inventory[slot.index];
    }

    private void SetStack(SlotUI slot, InventorySystem.ItemStack stack)
    {
        var inv = InventorySystem.Instance;
        if (inv == null) return;
        if (slot.isHotbar) inv.hotbar[slot.index] = stack;
        else inv.inventory[slot.index] = stack;
    }

    // ── Hotbar sempre visível ─────────────────────────────────────────────────

    private void BuildHotbar()
    {
        float totalW = InventorySystem.HotbarSlots * SlotSize + (InventorySystem.HotbarSlots - 1) * SlotGap + 16f;
        GameObject hotbarObj = new GameObject("InventoryHotbar");
        hotbarObj.transform.SetParent(canvas.transform, false);
        RectTransform hrt = hotbarObj.AddComponent<RectTransform>();
        hrt.anchorMin = new Vector2(0.5f, 0f);
        hrt.anchorMax = new Vector2(0.5f, 0f);
        hrt.pivot = new Vector2(0.5f, 0f);
        hrt.sizeDelta = new Vector2(totalW, SlotSize + 16f);
        hrt.anchoredPosition = new Vector2(0f, 12f);
        hotbarObj.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.05f, 0.88f);

        float startX = -(InventorySystem.HotbarSlots * (SlotSize + SlotGap) - SlotGap) / 2f + SlotSize / 2f;
        for (int i = 0; i < InventorySystem.HotbarSlots; i++)
        {
            float x = startX + i * (SlotSize + SlotGap);
            hotbarSlots[i] = CreateSlot(hotbarObj.transform, x, 0f, true, i);

            // Número do slot
            GameObject numGo = new GameObject("Num");
            numGo.transform.SetParent(hotbarSlots[i].root.transform, false);
            RectTransform nrt = numGo.AddComponent<RectTransform>();
            nrt.anchorMin = new Vector2(0f, 0.75f); nrt.anchorMax = new Vector2(0.4f, 1f);
            nrt.offsetMin = new Vector2(3f, 0f); nrt.offsetMax = Vector2.zero;
            TextMeshProUGUI num = numGo.AddComponent<TextMeshProUGUI>();
            num.text = (i + 1).ToString(); num.fontSize = 9f;
            num.color = new Color(0.6f, 0.6f, 0.6f); num.alignment = TextAlignmentOptions.Left;
        }

        RefreshHotbarSelection();
    }

    private void RefreshHotbarSelection()
    {
        for (int i = 0; i < InventorySystem.HotbarSlots; i++)
        {
            if (hotbarSlots[i] == null) continue;
            bool sel = i == SelectedHotbarSlot;
            var stack = InventorySystem.Instance?.hotbar[i];
            hotbarSlots[i].bg.color    = sel ? cSelected : (stack != null ? cSlotFull : cSlot);
            hotbarSlots[i].border.color = sel ? Color.white : cBorder;
            hotbarSlots[i].root.transform.localScale = sel ? Vector3.one * 1.08f : Vector3.one;
        }
    }

    // ── Painel inventário ─────────────────────────────────────────────────────

    private void BuildInventoryPanel()
    {
        float cols   = InventorySystem.InventoryCols;
        float rows   = InventorySystem.InventoryRows;
        float panelW = cols * SlotSize + (cols - 1) * SlotGap + 40f;
        float panelH = 50f + SlotSize + 30f + rows * SlotSize + (rows - 1) * SlotGap + 50f;

        inventoryPanel = new GameObject("InventoryPanel");
        inventoryPanel.transform.SetParent(canvas.transform, false);
        RectTransform prt = inventoryPanel.AddComponent<RectTransform>();
        prt.anchorMin = new Vector2(0.5f, 0.5f); prt.anchorMax = new Vector2(0.5f, 0.5f);
        prt.pivot = new Vector2(0.5f, 0.5f);
        prt.sizeDelta = new Vector2(panelW, panelH);
        prt.anchoredPosition = Vector2.zero;
        inventoryPanel.AddComponent<Image>().color = cBg;

        float cursorY = panelH / 2f - 28f;
        CreateLabel(inventoryPanel.transform, "Inventário", cursorY, 16f);

        cursorY -= 30f;
        CreateLabel(inventoryPanel.transform, "Acesso Rápido", cursorY, 9f, new Color(0.6f, 0.6f, 0.6f));
        cursorY -= 10f + SlotSize / 2f;
        float hStartX = -(InventorySystem.HotbarSlots * (SlotSize + SlotGap) - SlotGap) / 2f + SlotSize / 2f;
        for (int i = 0; i < InventorySystem.HotbarSlots; i++)
        {
            float x = hStartX + i * (SlotSize + SlotGap);
            panelHotbarSlots[i] = CreateSlot(inventoryPanel.transform, x, cursorY, true, i);
        }

        cursorY -= SlotSize / 2f + 20f;
        CreateLabel(inventoryPanel.transform, "Mochila", cursorY, 9f, new Color(0.6f, 0.6f, 0.6f));
        cursorY -= 10f + SlotSize / 2f;
        float invStartX = -(cols * (SlotSize + SlotGap) - SlotGap) / 2f + SlotSize / 2f;
        for (int row = 0; row < InventorySystem.InventoryRows; row++)
            for (int col = 0; col < InventorySystem.InventoryCols; col++)
            {
                int idx = row * InventorySystem.InventoryCols + col;
                float x = invStartX + col * (SlotSize + SlotGap);
                float y = cursorY - row * (SlotSize + SlotGap);
                invSlots[idx] = CreateSlot(inventoryPanel.transform, x, y, false, idx);
            }

        // Peso
        GameObject weightGo = new GameObject("WeightLabel");
        weightGo.transform.SetParent(inventoryPanel.transform, false);
        RectTransform wrt = weightGo.AddComponent<RectTransform>();
        wrt.anchorMin = new Vector2(0f, 0f);
        wrt.anchorMax = new Vector2(1f, 0f);
        wrt.pivot     = new Vector2(0.5f, 0f);
        wrt.sizeDelta = new Vector2(0f, 24f);
        wrt.anchoredPosition = new Vector2(0f, 10f);
        weightLabel = weightGo.AddComponent<TextMeshProUGUI>();
        weightLabel.fontSize  = 11f;
        weightLabel.color     = Color.white;
        weightLabel.alignment = TextAlignmentOptions.Center;
        weightLabel.text      = $"0.0 / {InventorySystem.MaxWeight} kg";
    }

    // ── Refresh ───────────────────────────────────────────────────────────────

    private void RefreshUI()
    {
        if (InventorySystem.Instance == null) return;
        var inv = InventorySystem.Instance;

        for (int i = 0; i < InventorySystem.HotbarSlots; i++)
        {
            var s = inv.hotbar[i];
            bool sel = i == SelectedHotbarSlot;
            UpdateSlot(hotbarSlots[i], s, sel);
            if (IsOpen) UpdateSlot(panelHotbarSlots[i], s, sel);
        }

        if (!IsOpen) return;
        for (int i = 0; i < InventorySystem.InventorySlots; i++)
            UpdateSlot(invSlots[i], inv.inventory[i], false);

        if (weightLabel != null)
        {
            float w = inv.GetTotalWeight();
            weightLabel.text  = $"{w:F1} / {InventorySystem.MaxWeight} kg";
            weightLabel.color = w >= InventorySystem.MaxWeight    ? Color.red :
                                w >= InventorySystem.HeavyThreshold ? new Color(1f, 0.6f, 0f) :
                                Color.white;
        }
    }

    private void UpdateSlot(SlotUI slot, InventorySystem.ItemStack s, bool selected)
    {
        if (slot == null) return;
        slot.bg.color       = selected ? cSelected : (s != null ? cSlotFull : cSlot);
        slot.border.color   = selected ? Color.white : cBorder;
        slot.qty.text       = s != null ? $"x{s.quantity}" : "";
        slot.itemName.text  = s != null ? s.itemName : "";
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private SlotUI CreateSlot(Transform parent, float x, float y, bool isHotbar, int index)
    {
        SlotUI s = new SlotUI { isHotbar = isHotbar, index = index };
        s.root = new GameObject("Slot");
        s.root.transform.SetParent(parent, false);
        RectTransform rt = s.root.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(SlotSize, SlotSize);
        rt.anchoredPosition = new Vector2(x, y);
        s.border = s.root.AddComponent<Image>();
        s.border.color = cBorder;

        GameObject inner = new GameObject("Bg");
        inner.transform.SetParent(s.root.transform, false);
        RectTransform irt = inner.AddComponent<RectTransform>();
        irt.anchorMin = Vector2.zero; irt.anchorMax = Vector2.one;
        irt.offsetMin = new Vector2(2f, 2f); irt.offsetMax = new Vector2(-2f, -2f);
        s.bg = inner.AddComponent<Image>();
        s.bg.color = cSlot;

        GameObject nameGo = new GameObject("Name");
        nameGo.transform.SetParent(s.root.transform, false);
        RectTransform nrt = nameGo.AddComponent<RectTransform>();
        nrt.anchorMin = new Vector2(0f, 0.5f); nrt.anchorMax = new Vector2(1f, 1f);
        nrt.offsetMin = new Vector2(2f, 0f); nrt.offsetMax = new Vector2(-2f, 0f);
        s.itemName = nameGo.AddComponent<TextMeshProUGUI>();
        s.itemName.fontSize = 7f; s.itemName.color = Color.white;
        s.itemName.alignment = TextAlignmentOptions.Center;

        GameObject qtyGo = new GameObject("Qty");
        qtyGo.transform.SetParent(s.root.transform, false);
        RectTransform qrt = qtyGo.AddComponent<RectTransform>();
        qrt.anchorMin = new Vector2(0f, 0f); qrt.anchorMax = new Vector2(1f, 0.5f);
        qrt.offsetMin = new Vector2(2f, 2f); qrt.offsetMax = new Vector2(-2f, 0f);
        s.qty = qtyGo.AddComponent<TextMeshProUGUI>();
        s.qty.fontSize = 9f; s.qty.color = new Color(0.8f, 0.9f, 0.8f);
        s.qty.alignment = TextAlignmentOptions.BottomRight;

        return s;
    }

    private void CreateLabel(Transform parent, string text, float y, float size, Color? color = null)
    {
        GameObject go = new GameObject("Label");
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0.5f); rt.anchorMax = new Vector2(1f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(0f, size + 4f);
        rt.anchoredPosition = new Vector2(0f, y);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = size;
        tmp.color = color ?? Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
    }
}
