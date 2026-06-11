using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CraftingUI : MonoBehaviour
{
    private GameObject craftPanel;
    private GameObject recipeContainer;
    private bool isBuilt = false;

    private readonly Color cBg       = new Color(0.08f, 0.08f, 0.08f, 0.97f);
    private readonly Color cRecipe   = new Color(0.15f, 0.15f, 0.15f, 1.00f);
    private readonly Color cCanCraft = new Color(0.15f, 0.35f, 0.15f, 1.00f);
    private readonly Color cBtn      = new Color(0.20f, 0.50f, 0.20f, 1.00f);
    private readonly Color cBtnOff   = new Color(0.30f, 0.30f, 0.30f, 1.00f);

    void Start()
    {
        // Constrói logo no Start se o CraftingSystem já estiver pronto
        TryBuild();
    }

    void Update()
    {
        if (!isBuilt) TryBuild();

        if (craftPanel != null)
        {
            bool shouldShow = InventoryUI.IsOpen;
            if (craftPanel.activeSelf != shouldShow)
                craftPanel.SetActive(shouldShow);

            if (shouldShow) RefreshRecipes();
        }
    }

    private void TryBuild()
    {
        if (isBuilt) return;
        if (CraftingSystem.Instance == null) return;
        if (CraftingSystem.Instance.recipes == null) return;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        isBuilt = true;
        BuildPanel(canvas);
    }

    private void BuildPanel(Canvas canvas)
    {
        craftPanel = new GameObject("CraftPanel");
        craftPanel.transform.SetParent(canvas.transform, false);

        // Painel à direita do inventário (que ocupa ~413px ao centro, de -206 a +206),
        // sem sobrepor. Pivot à esquerda para encostar à borda direita do inventário.
        RectTransform rt = craftPanel.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.sizeDelta = new Vector2(300f, 460f);
        rt.anchoredPosition = new Vector2(220f, 0f);
        craftPanel.AddComponent<Image>().color = cBg;

        // Título
        GameObject title = new GameObject("Title");
        title.transform.SetParent(craftPanel.transform, false);
        RectTransform trt = title.AddComponent<RectTransform>();
        trt.anchorMin = new Vector2(0f, 1f);
        trt.anchorMax = new Vector2(1f, 1f);
        trt.pivot = new Vector2(0.5f, 1f);
        trt.sizeDelta = new Vector2(0f, 34f);
        trt.anchoredPosition = new Vector2(0f, -8f);
        TextMeshProUGUI titleTmp = title.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "Craft"; titleTmp.fontSize = 18f;
        titleTmp.color = Color.white;
        titleTmp.alignment = TextAlignmentOptions.Center;

        // Viewport com recorte (mask) — área visível por onde as receitas fazem scroll.
        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(craftPanel.transform, false);
        RectTransform vrt = viewport.AddComponent<RectTransform>();
        vrt.anchorMin = new Vector2(0f, 0f);
        vrt.anchorMax = new Vector2(1f, 1f);
        vrt.offsetMin = new Vector2(8f, 8f);
        vrt.offsetMax = new Vector2(-8f, -46f);
        viewport.AddComponent<Image>().color = Color.clear;
        viewport.AddComponent<RectMask2D>();

        ScrollRect scroll = viewport.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 20f;
        scroll.viewport = vrt;

        // Content — empilha as receitas; altura definida em BuildRecipes.
        recipeContainer = new GameObject("Recipes");
        recipeContainer.transform.SetParent(viewport.transform, false);
        RectTransform rct = recipeContainer.AddComponent<RectTransform>();
        rct.anchorMin = new Vector2(0f, 1f);
        rct.anchorMax = new Vector2(1f, 1f);
        rct.pivot = new Vector2(0.5f, 1f);
        rct.anchoredPosition = Vector2.zero;
        rct.sizeDelta = new Vector2(0f, 0f);
        scroll.content = rct;

        BuildRecipes();

        craftPanel.SetActive(false);
    }

    private void BuildRecipes()
    {
        if (CraftingSystem.Instance?.recipes == null) return;

        var list = CraftingSystem.Instance.recipes.recipes;
        float cardH = 80f;
        float gap = 8f;

        // Altura total do conteúdo para o ScrollRect poder percorrer todas as receitas.
        RectTransform contentRt = recipeContainer.GetComponent<RectTransform>();
        contentRt.sizeDelta = new Vector2(0f, list.Count * (cardH + gap) + gap);

        for (int i = 0; i < list.Count; i++)
        {
            int idx = i;
            var recipe = list[i];

            GameObject card = new GameObject($"Recipe_{recipe.recipeName}");
            card.transform.SetParent(recipeContainer.transform, false);
            RectTransform crt = card.AddComponent<RectTransform>();
            crt.anchorMin = new Vector2(0f, 1f);
            crt.anchorMax = new Vector2(1f, 1f);
            crt.pivot = new Vector2(0.5f, 1f);
            crt.sizeDelta = new Vector2(0f, cardH);
            crt.anchoredPosition = new Vector2(0f, -(gap + i * (cardH + gap)));
            card.AddComponent<Image>().color = cRecipe;

            // Nome
            MakeLabel(card.transform, recipe.recipeName,
                new Vector2(0f, 0.62f), new Vector2(1f, 1f), 11f, Color.white);

            // Ingredientes
            string ingText = "";
            foreach (var ing in recipe.ingredients)
                ingText += $"{ing.itemName} x{ing.quantity}  ";
            MakeLabel(card.transform, ingText.Trim(),
                new Vector2(0f, 0.38f), new Vector2(1f, 0.65f), 8f,
                new Color(0.7f, 0.7f, 0.7f));

            // Botão
            GameObject btn = new GameObject("CraftBtn");
            btn.transform.SetParent(card.transform, false);
            RectTransform brt = btn.AddComponent<RectTransform>();
            brt.anchorMin = new Vector2(0.1f, 0.05f);
            brt.anchorMax = new Vector2(0.9f, 0.36f);
            brt.offsetMin = brt.offsetMax = Vector2.zero;
            Image btnImg = btn.AddComponent<Image>();
            btnImg.color = cBtn;
            Button btnComp = btn.AddComponent<Button>();
            btnComp.targetGraphic = btnImg;
            btnComp.onClick.AddListener(() => {
                CraftingSystem.Instance.TryCraft(list[idx]);
            });

            MakeLabel(btn.transform, "Craftar",
                Vector2.zero, Vector2.one, 9f, Color.white);
        }
    }

    private void RefreshRecipes()
    {
        if (CraftingSystem.Instance?.recipes == null) return;
        var list = CraftingSystem.Instance.recipes.recipes;
        int i = 0;

        foreach (Transform child in recipeContainer.transform)
        {
            if (i >= list.Count) break;
            bool can = CraftingSystem.Instance.CanCraft(list[i]);

            Image cardImg = child.GetComponent<Image>();
            if (cardImg != null) cardImg.color = can ? cCanCraft : cRecipe;

            Transform btn = child.Find("CraftBtn");
            if (btn != null)
            {
                Image btnImg = btn.GetComponent<Image>();
                if (btnImg != null) btnImg.color = can ? cBtn : cBtnOff;
            }
            i++;
        }
    }

    private void MakeLabel(Transform parent, string text,
        Vector2 anchorMin, Vector2 anchorMax, float size, Color color)
    {
        GameObject go = new GameObject("Label");
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = new Vector2(4f, 0f); rt.offsetMax = new Vector2(-4f, 0f);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = size;
        tmp.color = color; tmp.alignment = TextAlignmentOptions.Center;
    }
}
