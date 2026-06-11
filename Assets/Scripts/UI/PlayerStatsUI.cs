using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerStatsUI : MonoBehaviour
{
    private Image healthFill;
    private Image hungerFill;
    private TextMeshProUGUI healthLabel;
    private TextMeshProUGUI hungerLabel;

    private Health playerHealth;
    private HungerSystem playerHunger;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<PlayerStatsUI>() != null) return;
        GameObject go = new GameObject("PlayerStatsUI");
        DontDestroyOnLoad(go);
        go.AddComponent<PlayerStatsUI>();
    }

    private void Awake()
    {
        BuildUI();
    }

    private void BuildUI()
    {
        GameObject canvasObj = new GameObject("StatsCanvas");
        canvasObj.transform.SetParent(transform, false);

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        healthFill = MakeBar(canvasObj.transform, "HealthBar", new Vector2(20f, 58f),
            new Color(0.45f, 0.07f, 0.07f, 0.88f), new Color(0.88f, 0.15f, 0.15f, 1f),
            "HP  100 / 100", out healthLabel);

        hungerFill = MakeBar(canvasObj.transform, "HungerBar", new Vector2(20f, 22f),
            new Color(0.32f, 0.16f, 0.03f, 0.88f), new Color(0.88f, 0.52f, 0.08f, 1f),
            "Fome  100 / 100", out hungerLabel);
    }

    private Image MakeBar(Transform parent, string id, Vector2 anchoredPos,
        Color bgColor, Color fillColor, string initialText, out TextMeshProUGUI label)
    {
        GameObject bg = new GameObject(id + "_Bg");
        bg.transform.SetParent(parent, false);
        RectTransform bgRt = bg.AddComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.zero;
        bgRt.pivot = Vector2.zero;
        bgRt.sizeDelta = new Vector2(260f, 26f);
        bgRt.anchoredPosition = anchoredPos;
        bg.AddComponent<Image>().color = bgColor;

        GameObject fillObj = new GameObject(id + "_Fill");
        fillObj.transform.SetParent(bg.transform, false);
        RectTransform fillRt = fillObj.AddComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = new Vector2(2f, 2f);
        fillRt.offsetMax = new Vector2(-2f, -2f);
        Image fillImg = fillObj.AddComponent<Image>();
        fillImg.color = fillColor;
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillAmount = 1f;

        GameObject labelObj = new GameObject(id + "_Label");
        labelObj.transform.SetParent(bg.transform, false);
        RectTransform labelRt = labelObj.AddComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = new Vector2(6f, 0f);
        labelRt.offsetMax = new Vector2(-6f, 0f);
        label = labelObj.AddComponent<TextMeshProUGUI>();
        label.text = initialText;
        label.fontSize = 13f;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.fontStyle = FontStyles.Bold;

        return fillImg;
    }

    private void Update()
    {
        if (playerHealth == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p == null) return;

            playerHealth = p.GetComponent<Health>();
            if (playerHealth != null)
            {
                playerHealth.OnDamaged += OnHealthChanged;
                OnHealthChanged(playerHealth.CurrentHP, playerHealth.MaxHP);
            }

            playerHunger = p.GetComponent<HungerSystem>();
            if (playerHunger != null)
            {
                playerHunger.OnHungerChanged += OnHungerChanged;
                OnHungerChanged(playerHunger.CurrentHunger, playerHunger.MaxHunger);
            }
        }

        // Sincroniza sempre os valores atuais, garantindo que a barra
        // reflete o HP/Fome reais mesmo que algum evento se perca.
        if (playerHealth != null)
            OnHealthChanged(playerHealth.CurrentHP, playerHealth.MaxHP);

        if (playerHunger != null)
            OnHungerChanged(playerHunger.CurrentHunger, playerHunger.MaxHunger);
    }

    private void OnHealthChanged(float current, float max)
    {
        if (healthFill != null) healthFill.fillAmount = max > 0f ? current / max : 0f;
        if (healthLabel != null) healthLabel.text = $"HP  {current:F0} / {max:F0}";
    }

    private void OnHungerChanged(float current, float max)
    {
        if (hungerFill != null) hungerFill.fillAmount = max > 0f ? current / max : 0f;
        if (hungerLabel != null) hungerLabel.text = $"Fome  {current:F0} / {max:F0}";
    }

    private void OnDestroy()
    {
        if (playerHealth != null) playerHealth.OnDamaged -= OnHealthChanged;
        if (playerHunger != null) playerHunger.OnHungerChanged -= OnHungerChanged;
    }
}
