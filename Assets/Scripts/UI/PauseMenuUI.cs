using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Menu de pausa: prime-se Esc durante o jogo para pausar tudo (mobs, física, animações)
/// e mostrar uma caixa "Pausa" com as opções Continuar, Configurações e Sair.
/// Cria-se sozinho — não é preciso colocar nada na cena.
/// </summary>
public class PauseMenuUI : MonoBehaviour
{
    private static readonly Color cOverlay = new Color(0f, 0f, 0f, 0.6f);
    private static readonly Color cBox = new Color(0.12f, 0.14f, 0.18f, 0.97f);
    private static readonly Color cTitle = Color.white;
    private static readonly Color cButton = new Color(0.22f, 0.26f, 0.33f, 1f);
    private static readonly Color cButtonHover = new Color(0.32f, 0.38f, 0.47f, 1f);
    private static readonly Color cButtonText = Color.white;

    private GameObject canvasObj;
    private GameObject pausePanel;
    private GameObject settingsPanel;
    private bool isPaused = false;

    private PlayerController playerController;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<PauseMenuUI>() != null) return;

        GameObject root = new GameObject("PauseMenu");
        root.AddComponent<PauseMenuUI>();
    }

    private void Awake()
    {
        BuildUI();
    }

    private void Update()
    {
        if (MainMenuUI.IsMainMenuOpen || InventoryUI.IsOpen) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) Resume();
            else Pause();
        }
    }

    // ── Pausar / continuar ───────────────────────────────────────────────────

    private void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        playerController = playerObj != null ? playerObj.GetComponent<PlayerController>() : null;
        if (playerController != null) playerController.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        settingsPanel.SetActive(false);
        pausePanel.SetActive(true);
        canvasObj.SetActive(true);
    }

    private void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f;

        if (playerController != null) playerController.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        canvasObj.SetActive(false);
    }

    // ── Construção da UI ─────────────────────────────────────────────────────

    private void BuildUI()
    {
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        canvasObj = new GameObject("PauseMenuCanvas");
        canvasObj.transform.SetParent(transform, false);

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        CreateImg(canvasObj.transform, "Overlay", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, cOverlay);

        // Caixa de pausa
        pausePanel = CreatePanel(canvasObj.transform);
        CreateImg(pausePanel.transform, "Box", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, cBox);

        CreateTMP(pausePanel.transform, "Title", new Vector2(0f, 0.78f), new Vector2(1f, 0.94f),
                  "Pausa", 56f, cTitle, TextAlignmentOptions.Center);

        CreateButton(pausePanel.transform, "ResumeButton", "Continuar", 0.56f, OnResumeClicked);
        CreateButton(pausePanel.transform, "SettingsButton", "Configurações", 0.42f, OnSettingsClicked);
        CreateButton(pausePanel.transform, "QuitButton", "Sair", 0.28f, OnQuitClicked);

        // Painel de configurações (ainda vazio) — troca com a caixa de pausa
        settingsPanel = CreatePanel(canvasObj.transform);
        CreateImg(settingsPanel.transform, "Box", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, cBox);

        CreateTMP(settingsPanel.transform, "SettingsTitle", new Vector2(0f, 0.78f), new Vector2(1f, 0.94f),
                  "Configurações", 48f, cTitle, TextAlignmentOptions.Center);
        CreateTMP(settingsPanel.transform, "VolumeLabel", new Vector2(0f, 0.60f), new Vector2(1f, 0.68f),
                  "Volume da Música", 26f, cTitle, TextAlignmentOptions.Center);
        CreateSlider(settingsPanel.transform, "VolumeSlider", 0.52f, AudioController.GetVolume(), AudioController.SetVolume);
        CreateButton(settingsPanel.transform, "BackButton", "Voltar", 0.28f, OnBackClicked);

        settingsPanel.SetActive(false);
        canvasObj.SetActive(false);
    }

    private GameObject CreatePanel(Transform parent)
    {
        GameObject go = new GameObject("Panel");
        go.transform.SetParent(parent, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(520f, 560f);
        rt.anchoredPosition = Vector2.zero;

        return go;
    }

    private Button CreateButton(Transform parent, string goName, string label, float anchorY, UnityEngine.Events.UnityAction onClick)
    {
        GameObject go = new GameObject(goName);
        go.transform.SetParent(parent, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, anchorY);
        rt.anchorMax = new Vector2(0.5f, anchorY);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(360f, 72f);
        rt.anchoredPosition = Vector2.zero;

        Image img = go.AddComponent<Image>();
        img.color = cButton;

        Button btn = go.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = cButton;
        colors.highlightedColor = cButtonHover;
        colors.pressedColor = cButtonHover;
        colors.selectedColor = cButton;
        btn.colors = colors;
        btn.onClick.AddListener(onClick);

        CreateTMP(go.transform, "Label", Vector2.zero, Vector2.one, label, 28f, cButtonText, TextAlignmentOptions.Center);

        return btn;
    }

    private Image CreateImg(Transform parent, string goName, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color color)
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

    private TextMeshProUGUI CreateTMP(Transform parent, string goName, Vector2 anchorMin, Vector2 anchorMax,
                                      string text, float size, Color color, TextAlignmentOptions align)
    {
        GameObject go = new GameObject(goName);
        go.transform.SetParent(parent, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = align;

        return tmp;
    }

    private Slider CreateSlider(Transform parent, string goName, float anchorY, float initialValue01, UnityEngine.Events.UnityAction<float> onValueChanged)
    {
        GameObject go = new GameObject(goName);
        go.transform.SetParent(parent, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, anchorY);
        rt.anchorMax = new Vector2(0.5f, anchorY);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(360f, 24f);
        rt.anchoredPosition = Vector2.zero;

        CreateImg(go.transform, "Background", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(1f, 1f, 1f, 0.18f));

        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(go.transform, false);
        RectTransform fillAreaRt = fillArea.AddComponent<RectTransform>();
        fillAreaRt.anchorMin = new Vector2(0f, 0f);
        fillAreaRt.anchorMax = new Vector2(1f, 1f);
        fillAreaRt.offsetMin = new Vector2(5f, 0f);
        fillAreaRt.offsetMax = new Vector2(-5f, 0f);

        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(fillArea.transform, false);
        RectTransform fillRt = fillObj.AddComponent<RectTransform>();
        fillRt.anchorMin = new Vector2(0f, 0f);
        fillRt.anchorMax = new Vector2(0f, 1f);
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;
        Image fillImg = fillObj.AddComponent<Image>();
        fillImg.color = cButtonHover;

        GameObject handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(go.transform, false);
        RectTransform handleAreaRt = handleArea.AddComponent<RectTransform>();
        handleAreaRt.anchorMin = new Vector2(0f, 0f);
        handleAreaRt.anchorMax = new Vector2(1f, 1f);
        handleAreaRt.offsetMin = new Vector2(10f, 0f);
        handleAreaRt.offsetMax = new Vector2(-10f, 0f);

        GameObject handleObj = new GameObject("Handle");
        handleObj.transform.SetParent(handleArea.transform, false);
        RectTransform handleRt = handleObj.AddComponent<RectTransform>();
        handleRt.anchorMin = new Vector2(0f, 0.5f);
        handleRt.anchorMax = new Vector2(0f, 0.5f);
        handleRt.pivot = new Vector2(0.5f, 0.5f);
        handleRt.sizeDelta = new Vector2(20f, 28f);
        Image handleImg = handleObj.AddComponent<Image>();
        handleImg.color = cButton;

        Slider slider = go.AddComponent<Slider>();
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.fillRect = fillRt;
        slider.handleRect = handleRt;
        slider.targetGraphic = handleImg;
        slider.value = initialValue01;
        slider.onValueChanged.AddListener(onValueChanged);

        return slider;
    }

    // ── Ações dos botões ─────────────────────────────────────────────────────

    private void OnResumeClicked() => Resume();

    private void OnSettingsClicked()
    {
        pausePanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    private void OnBackClicked()
    {
        settingsPanel.SetActive(false);
        pausePanel.SetActive(true);
    }

    private void OnQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
