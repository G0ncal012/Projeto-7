using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Menu inicial do jogo: aparece assim que a cena carrega, por cima de tudo.
/// "Jogar" gera o mapa, "Configurações" mostra um painel (ainda vazio) e "Sair" fecha o jogo.
/// Cria-se sozinho — não é preciso colocar nada na cena.
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    private static readonly Color cBg = new Color32(0x9A, 0xDB, 0x9A, 0xFF);
    private static readonly Color cTitle = new Color(0.09f, 0.22f, 0.12f, 1f);
    private static readonly Color cButton = new Color(0.16f, 0.20f, 0.26f, 1f);
    private static readonly Color cButtonHover = new Color(0.26f, 0.33f, 0.42f, 1f);
    private static readonly Color cButtonText = Color.white;

    private const float PreviewRotationSpeed = 22f; // graus por segundo

    // Permite a outros sistemas (ex: PauseMenuUI) saber se o menu inicial ainda está aberto
    public static bool IsMainMenuOpen { get; private set; } = true;

    private GameObject canvasObj;
    private GameObject menuPanel;
    private GameObject settingsPanel;
    private bool isOpen = true;

    private PlayerController playerController;
    private Transform previewPivot;
    private RenderTexture previewTexture;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        IsMainMenuOpen = true;

        if (FindFirstObjectByType<MainMenuUI>() != null) return;

        GameObject root = new GameObject("MainMenu");
        root.AddComponent<MainMenuUI>();
    }

    private void Awake()
    {
        Time.timeScale = 0f;

        // Impede que o jogador rode a câmara enquanto o menu está aberto
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerController = playerObj.GetComponent<PlayerController>();
            if (playerController != null) playerController.enabled = false;
        }

        BuildUI();
        BuildCharacterPreview();
    }

    private void Update()
    {
        if (!isOpen) return;

        // Garante que o cursor fica disponível para clicar nos botões,
        // mesmo que o PlayerController o tenha bloqueado
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (previewPivot != null)
            previewPivot.Rotate(Vector3.up, PreviewRotationSpeed * Time.unscaledDeltaTime, Space.Self);
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

        canvasObj = new GameObject("MainMenuCanvas");
        canvasObj.transform.SetParent(transform, false);

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        CreateImg(canvasObj.transform, "Background", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, cBg);

        // Painel principal — lado esquerdo do ecrã
        menuPanel = CreatePanel(canvasObj.transform, "MenuPanel", new Vector2(0.27f, 0.5f), new Vector2(560f, 620f));

        CreateTMP(menuPanel.transform, "Title", new Vector2(0f, 0.74f), new Vector2(1f, 0.94f),
                  "Hugo's Adventure", 64f, cTitle, TextAlignmentOptions.Center);

        CreateButton(menuPanel.transform, "PlayButton", "Jogar", 0.56f, OnPlayClicked);
        CreateButton(menuPanel.transform, "SettingsButton", "Configurações", 0.42f, OnSettingsClicked);
        CreateButton(menuPanel.transform, "QuitButton", "Sair", 0.28f, OnQuitClicked);

        // Painel de configurações (ainda vazio) — mesma posição, troca com o principal
        settingsPanel = CreatePanel(canvasObj.transform, "SettingsPanel", new Vector2(0.27f, 0.5f), new Vector2(560f, 620f));

        CreateTMP(settingsPanel.transform, "SettingsTitle", new Vector2(0f, 0.74f), new Vector2(1f, 0.94f),
                  "Configurações", 50f, cTitle, TextAlignmentOptions.Center);
        CreateTMP(settingsPanel.transform, "VolumeLabel", new Vector2(0f, 0.58f), new Vector2(1f, 0.66f),
                  "Volume da Música", 26f, cTitle, TextAlignmentOptions.Center);
        CreateSlider(settingsPanel.transform, "VolumeSlider", 0.50f, AudioController.GetVolume(), AudioController.SetVolume);
        CreateButton(settingsPanel.transform, "BackButton", "Voltar", 0.28f, OnBackClicked);

        settingsPanel.SetActive(false);
    }

    private GameObject CreatePanel(Transform parent, string goName, Vector2 anchor, Vector2 size)
    {
        GameObject go = new GameObject(goName);
        go.transform.SetParent(parent, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
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
        rt.sizeDelta = new Vector2(380f, 76f);
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

        CreateTMP(go.transform, "Label", Vector2.zero, Vector2.one, label, 30f, cButtonText, TextAlignmentOptions.Center);

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

        CreateImg(go.transform, "Background", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(cTitle.r, cTitle.g, cTitle.b, 0.18f));

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

    // ── Pré-visualização do personagem (lado direito) ───────────────────────

    private void BuildCharacterPreview()
    {
        MapGenerator generator = FindFirstObjectByType<MapGenerator>();
        if (generator == null || generator.playerPrefab == null)
        {
            Debug.LogWarning("[MainMenuUI] playerPrefab não encontrado — pré-visualização do personagem não criada.");
            return;
        }

        // Palco bem longe do mapa, para não interferir em nada
        Vector3 stagePos = new Vector3(0f, -3000f, 0f);

        GameObject stage = new GameObject("MenuCharacterStage");
        stage.transform.SetParent(transform, false);
        stage.transform.position = stagePos;

        previewPivot = new GameObject("CharacterPivot").transform;
        previewPivot.SetParent(stage.transform, false);
        previewPivot.localPosition = Vector3.zero;

        // Instancia o prefab do jogador já desativado, para o PlayerController nunca correr
        // (nada de câmaras roubadas), e fica-se só com o modelo visual (Hugo)
        GameObject temp = Instantiate(generator.playerPrefab);
        temp.SetActive(false);

        Transform model = temp.transform.childCount > 0 ? temp.transform.GetChild(0) : temp.transform;
        model.SetParent(previewPivot, false);
        model.localPosition = Vector3.zero;
        model.localRotation = Quaternion.identity;
        model.gameObject.SetActive(true);

        Destroy(temp);

        // T-pose: desliga o Animator para mostrar a pose de descanso do modelo
        Animator animator = model.GetComponentInChildren<Animator>();
        if (animator != null) animator.enabled = false;

        // Limites do modelo — para o assentar no palco e enquadrar a câmara corretamente
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
        Bounds bounds = new Bounds(model.position, Vector3.zero);
        bool hasBounds = false;
        foreach (Renderer r in renderers)
        {
            if (!hasBounds) { bounds = r.bounds; hasBounds = true; }
            else bounds.Encapsulate(r.bounds);
        }
        if (!hasBounds) bounds = new Bounds(model.position, Vector3.one * 2f);

        float lift = stagePos.y - bounds.min.y;
        model.position += Vector3.up * lift;
        bounds.center += Vector3.up * lift;

        float height = Mathf.Max(bounds.size.y, 0.5f);
        float fov = 35f;
        float distance = (height * 0.5f) / Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad) * 1.2f;

        previewTexture = new RenderTexture(512, 640, 16);
        previewTexture.Create();

        GameObject camObj = new GameObject("MenuCharacterCamera");
        camObj.transform.SetParent(stage.transform, false);
        camObj.transform.position = bounds.center + new Vector3(0f, 0f, distance);
        camObj.transform.LookAt(bounds.center);

        Camera previewCam = camObj.AddComponent<Camera>();
        previewCam.clearFlags = CameraClearFlags.SolidColor;
        previewCam.backgroundColor = cBg;
        previewCam.fieldOfView = fov;
        previewCam.nearClipPlane = 0.05f;
        previewCam.farClipPlane = distance + height * 4f;
        previewCam.targetTexture = previewTexture;

        // RawImage no lado direito do ecrã, a mostrar o que a câmara do palco filma
        GameObject rawImgObj = new GameObject("CharacterPreview");
        rawImgObj.transform.SetParent(canvasObj.transform, false);

        RectTransform rt = rawImgObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.56f, 0.08f);
        rt.anchorMax = new Vector2(0.94f, 0.92f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        RawImage rawImg = rawImgObj.AddComponent<RawImage>();
        rawImg.texture = previewTexture;
    }

    // ── Ações dos botões ─────────────────────────────────────────────────────

    private void OnPlayClicked()
    {
        isOpen = false;
        IsMainMenuOpen = false;
        canvasObj.SetActive(false);

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (playerController != null) playerController.enabled = true;

        MapGenerator generator = FindFirstObjectByType<MapGenerator>();
        if (generator != null)
            generator.GenerateMap();
        else
            Debug.LogWarning("[MainMenuUI] MapGenerator não encontrado na cena.");

        if (previewTexture != null)
        {
            previewTexture.Release();
            Destroy(previewTexture);
        }

        Destroy(gameObject);
    }

    private void OnSettingsClicked()
    {
        menuPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    private void OnBackClicked()
    {
        settingsPanel.SetActive(false);
        menuPanel.SetActive(true);
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
