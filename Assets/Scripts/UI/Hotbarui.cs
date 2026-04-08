using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Coloca este script num GameObject dentro de um Canvas.
/// O Canvas deve ter:
///   - Render Mode: Screen Space - Overlay
///   - UI Scale Mode: Scale With Screen Size (1920x1080)
/// </summary>
public class HotbarUI : MonoBehaviour
{
    [Header("Referências (auto-encontradas se vazias)")]
    [SerializeField] private BuildingManager buildingManager;
    [SerializeField] private AxeTool axeTool;

    [Header("Tamanho dos slots")]
    [SerializeField] private float slotSize = 72f;
    [SerializeField] private float slotSpacing = 6f;
    [SerializeField] private float bottomPadding = 16f;

    // Cores
    private readonly Color cBg = new Color(0.08f, 0.08f, 0.08f, 0.92f);
    private readonly Color cSelected = new Color(0.88f, 0.68f, 0.08f, 1.00f);
    private readonly Color cBorder = new Color(0.30f, 0.30f, 0.30f, 1.00f);
    private readonly Color cBar = new Color(0.03f, 0.03f, 0.03f, 0.85f);
    private readonly Color cText = Color.white;
    private readonly Color cHint = new Color(0.65f, 0.65f, 0.65f, 0.85f);

    private class SlotUI
    {
        public GameObject root;
        public Image bg, border;
        public TextMeshProUGUI nameLabel, keyLabel;
        public bool isAxeSlot;
    }

    private List<SlotUI> slots = new List<SlotUI>();
    private bool axeActive = false;

    private void EnsureRefs()
    {
        if (buildingManager == null) buildingManager = FindFirstObjectByType<BuildingManager>();
        if (axeTool == null) axeTool = FindFirstObjectByType<AxeTool>();
    }

    // ─────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        EnsureRefs();

        if (buildingManager == null) { Debug.LogWarning("[HotbarUI] BuildingManager não encontrado!"); return; }

        EnsureCanvasSetup();
        BuildUI();
        Refresh(buildingManager.GetSelectedHotbarIndex(), false);
        buildingManager.OnHotbarSelectionChanged += OnBuildSlotChanged;
    }

    private void OnDestroy()
    {
        if (buildingManager != null)
            buildingManager.OnHotbarSelectionChanged -= OnBuildSlotChanged;
    }

    // Garante que o Canvas pai está configurado corretamente
    private void EnsureCanvasSetup()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }
    }

    // ── Construir a UI ───────────────────────────────────────────────────────

    private void BuildUI()
    {
        List<BuildingManager.HotbarSlot> buildSlots = buildingManager.GetHotbarSlots();
        int buildCount = Mathf.Min(buildSlots.Count, 8);
        int total = buildCount + 1; // +1 machado

        // Container — ancorado ao centro-baixo do Canvas
        GameObject container = new GameObject("HotbarContainer");
        container.transform.SetParent(transform, false);

        RectTransform crt = container.AddComponent<RectTransform>();
        crt.anchorMin = new Vector2(0.5f, 0f);
        crt.anchorMax = new Vector2(0.5f, 0f);
        crt.pivot = new Vector2(0.5f, 0f);
        float totalW = total * slotSize + (total - 1) * slotSpacing;
        crt.sizeDelta = new Vector2(totalW + 24f, slotSize + 16f);
        crt.anchoredPosition = new Vector2(0f, bottomPadding);

        // Barra de fundo
        CreateImg(container.transform, "Bar",
                  Vector2.zero, Vector2.one,
                  new Vector2(-4f, -4f), new Vector2(4f, 4f), cBar);

        // Slots de construção
        for (int i = 0; i < buildCount; i++)
        {
            string name = buildSlots[i].displayName;
            string type = buildSlots[i].buildType == SelectedBuildingType.floor ? "Chão" : "Parede";
            SlotUI s = MakeSlot(container.transform, i, total, name, (i + 1).ToString(), false);
            AddSub(s.root.transform, type, new Color(0.5f, 0.8f, 1f, 0.5f));
            slots.Add(s);
        }

        // Slot do machado
        SlotUI axe = MakeSlot(container.transform, buildCount, total, "Machado", "E", true);
        AddSub(axe.root.transform, "Ferramenta", new Color(1f, 0.6f, 0.15f, 0.55f));
        slots.Add(axe);
    }

    private SlotUI MakeSlot(Transform parent, int index, int total,
                             string label, string key, bool isAxe)
    {
        SlotUI s = new SlotUI { isAxeSlot = isAxe };
        float totalW = total * slotSize + (total - 1) * slotSpacing;
        float posX = (-totalW / 2f + slotSize / 2f) + index * (slotSize + slotSpacing);

        s.root = new GameObject($"Slot_{label}");
        s.root.transform.SetParent(parent, false);
        RectTransform rt = s.root.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(slotSize, slotSize);
        rt.anchoredPosition = new Vector2(posX, 0f);

        Color borderCol = isAxe ? new Color(0.6f, 0.3f, 0.08f, 1f) : cBorder;
        s.border = CreateImg(s.root.transform, "Border",
                             Vector2.zero, Vector2.one,
                             new Vector2(-2f, -2f), new Vector2(2f, 2f), borderCol);

        s.bg = CreateImg(s.root.transform, "BG",
                         Vector2.zero, Vector2.one,
                         Vector2.zero, Vector2.zero, cBg);

        // Ícone simples (bloco colorido)
        Color iconCol = isAxe
            ? new Color(0.72f, 0.72f, 0.78f, 0.5f)
            : new Color(0.45f, 0.70f, 1.00f, 0.28f);
        CreateImg(s.root.transform, "Icon",
                  new Vector2(0.14f, 0.38f), new Vector2(0.86f, 0.74f),
                  Vector2.zero, Vector2.zero, iconCol);

        s.nameLabel = CreateTMP(s.root.transform, "Name",
                                new Vector2(0f, 0.02f), new Vector2(1f, 0.26f),
                                label, 8f, cText, TextAlignmentOptions.Center);
        s.nameLabel.overflowMode = TextOverflowModes.Ellipsis;

        s.keyLabel = CreateTMP(s.root.transform, "Key",
                               new Vector2(0.02f, 0.76f), new Vector2(0.5f, 1f),
                               key, 11f, cHint, TextAlignmentOptions.Left);
        s.keyLabel.fontStyle = FontStyles.Bold;

        return s;
    }

    private void AddSub(Transform parent, string text, Color color)
    {
        CreateTMP(parent, "Sub",
                  new Vector2(0f, 0.26f), new Vector2(1f, 0.42f),
                  text, 6.5f, color, TextAlignmentOptions.Center);
    }

    // ── Lógica de seleção ────────────────────────────────────────────────────

    private void Update()
    {
        // O Player (e o AxeTool) pode ser criado em runtime (ex: MapGenerator),
        // por isso garantimos que a referência existe antes de usar.
        EnsureRefs();

        if (Input.GetKeyDown(KeyCode.E))
        {
            axeActive = !axeActive;

            if (axeActive)
            {
                buildingManager.DeactivateBuildMode();
                if (axeTool != null) axeTool.SetAxeActive(true);
                Refresh(-1, true);
            }
            else
            {
                if (axeTool != null) axeTool.SetAxeActive(false);
                Refresh(buildingManager.GetSelectedHotbarIndex(), false);
            }
        }
    }

    private void OnBuildSlotChanged(int index)
    {
        // Selecionar item de construção desativa o machado
        axeActive = false;
        if (axeTool != null) axeTool.SetAxeActive(false);
        Refresh(index, false);
    }

    private void Refresh(int buildIndex, bool axeOn)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            bool sel;
            if (slots[i].isAxeSlot)
                sel = axeOn;
            else
                sel = (i == buildIndex);

            Color borderTarget = slots[i].isAxeSlot
                ? (sel ? Color.white : new Color(0.6f, 0.3f, 0.08f, 1f))
                : (sel ? Color.white : cBorder);

            slots[i].bg.color = sel ? cSelected : cBg;
            slots[i].border.color = borderTarget;
            slots[i].root.transform.localScale = sel ? Vector3.one * 1.09f : Vector3.one;
        }
    }

    // ── Helpers UI ───────────────────────────────────────────────────────────

    private Image CreateImg(Transform parent, string goName,
                            Vector2 anchorMin, Vector2 anchorMax,
                            Vector2 offsetMin, Vector2 offsetMax, Color color)
    {
        GameObject go = new GameObject(goName);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;
        Image img = go.AddComponent<Image>();
        img.color = color;
        return img;
    }

    private TextMeshProUGUI CreateTMP(Transform parent, string goName,
                                      Vector2 anchorMin, Vector2 anchorMax,
                                      string text, float size, Color color,
                                      TextAlignmentOptions align)
    {
        GameObject go = new GameObject(goName);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = new Vector2(2f, 0f); rt.offsetMax = new Vector2(-2f, 0f);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = size;
        tmp.color = color; tmp.alignment = align;
        return tmp;
    }
}