using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BossHealthBarUI : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<BossHealthBarUI>() != null) return;
        GameObject go = new GameObject("BossHealthBarUI");
        DontDestroyOnLoad(go);
        go.AddComponent<BossHealthBarUI>();
    }

    private GameObject panel;
    private Image fillImage;
    private TextMeshProUGUI nameLabel;
    private TextMeshProUGUI phaseLabel;
    private Health bossHealth;
    private bool built = false;

    private void Start()
    {
        BuildUI();
        SetVisible(false);
    }

    private void Update()
    {
        if (!built) return;

        BossAI boss = BossAI.ActiveBoss;

        if (boss == null)
        {
            SetVisible(false);
            bossHealth = null;
            return;
        }

        if (bossHealth == null)
        {
            bossHealth = boss.GetComponent<Health>();
            if (bossHealth != null)
                SetVisible(true);
        }

        if (bossHealth != null)
            UpdateBar(bossHealth.CurrentHP, bossHealth.MaxHP);
    }

    private void UpdateBar(float current, float max)
    {
        if (fillImage != null)
            fillImage.fillAmount = max > 0f ? current / max : 0f;

        if (phaseLabel != null)
        {
            float pct = max > 0f ? current / max : 0f;
            phaseLabel.text = pct > 0.5f ? "FASE 1" : "FASE 2 — ENRAIVECIDO";
            phaseLabel.color = pct > 0.5f ? new Color(0.8f, 0.8f, 0.8f) : new Color(1f, 0.35f, 0.1f);
        }
    }

    private void SetVisible(bool visible)
    {
        if (panel != null) panel.SetActive(visible);
    }

    private void BuildUI()
    {
        // Canvas
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        gameObject.AddComponent<GraphicRaycaster>();

        // Painel de fundo
        panel = new GameObject("BossPanel");
        panel.transform.SetParent(transform, false);
        RectTransform panelRt = panel.AddComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.2f, 0.88f);
        panelRt.anchorMax = new Vector2(0.8f, 1.0f);
        panelRt.offsetMin = Vector2.zero;
        panelRt.offsetMax = Vector2.zero;

        Image panelBg = panel.AddComponent<Image>();
        panelBg.color = new Color(0.04f, 0.04f, 0.04f, 0.88f);

        // Nome do boss
        GameObject namObj = new GameObject("BossName");
        namObj.transform.SetParent(panel.transform, false);
        RectTransform namRt = namObj.AddComponent<RectTransform>();
        namRt.anchorMin = new Vector2(0f, 0.55f);
        namRt.anchorMax = new Vector2(1f, 1f);
        namRt.offsetMin = new Vector2(8f, 0f);
        namRt.offsetMax = new Vector2(-8f, -2f);
        nameLabel = namObj.AddComponent<TextMeshProUGUI>();
        nameLabel.text = "SKELETON KING";
        nameLabel.fontSize = 22f;
        nameLabel.fontStyle = FontStyles.Bold;
        nameLabel.color = new Color(0.95f, 0.85f, 0.6f);
        nameLabel.alignment = TextAlignmentOptions.Center;

        // Barra de fundo (cinzento)
        GameObject barBg = new GameObject("BarBG");
        barBg.transform.SetParent(panel.transform, false);
        RectTransform barBgRt = barBg.AddComponent<RectTransform>();
        barBgRt.anchorMin = new Vector2(0.02f, 0.08f);
        barBgRt.anchorMax = new Vector2(0.98f, 0.52f);
        barBgRt.offsetMin = Vector2.zero;
        barBgRt.offsetMax = Vector2.zero;
        barBg.AddComponent<Image>().color = new Color(0.12f, 0.12f, 0.12f, 1f);

        // Barra de preenchimento (vermelha)
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(barBg.transform, false);
        RectTransform fillRt = fillObj.AddComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = new Vector2(2f, 2f);
        fillRt.offsetMax = new Vector2(-2f, -2f);
        fillImage = fillObj.AddComponent<Image>();
        fillImage.color = new Color(0.85f, 0.1f, 0.1f, 1f);
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillAmount = 1f;

        // Label de fase
        GameObject phaseObj = new GameObject("Phase");
        phaseObj.transform.SetParent(panel.transform, false);
        RectTransform phaseRt = phaseObj.AddComponent<RectTransform>();
        phaseRt.anchorMin = new Vector2(0f, 0f);
        phaseRt.anchorMax = new Vector2(1f, 0.14f);
        phaseRt.offsetMin = Vector2.zero;
        phaseRt.offsetMax = Vector2.zero;
        phaseLabel = phaseObj.AddComponent<TextMeshProUGUI>();
        phaseLabel.text = "FASE 1";
        phaseLabel.fontSize = 11f;
        phaseLabel.color = new Color(0.8f, 0.8f, 0.8f);
        phaseLabel.alignment = TextAlignmentOptions.Center;

        built = true;
    }

    private void OnDestroy() { }
}
