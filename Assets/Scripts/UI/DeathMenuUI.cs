using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Menu de morte: aparece quando o player morre, com as opções
/// "Renascer" (volta ao spawn com vida cheia) e "Menu Principal" (recarrega a cena).
/// Cria-se sozinho — não é preciso colocar nada na cena.
/// </summary>
public class DeathMenuUI : MonoBehaviour
{
    private static readonly Color cOverlay = new Color(0f, 0f, 0f, 0.7f);
    private static readonly Color cBox = new Color(0.18f, 0.05f, 0.05f, 0.97f);
    private static readonly Color cTitle = Color.white;
    private static readonly Color cButton = new Color(0.35f, 0.12f, 0.12f, 1f);
    private static readonly Color cButtonHover = new Color(0.5f, 0.18f, 0.18f, 1f);
    private static readonly Color cButtonText = Color.white;

    /// <summary>True enquanto o menu de morte está visível — usado pelo PauseMenuUI para ignorar o Esc.</summary>
    public static bool IsDeathMenuOpen { get; private set; } = false;

    private GameObject canvasObj;
    private GameObject deathPanel;

    private PlayerController playerController;
    private PlayerRespawn playerRespawn;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        // RuntimeInitializeOnLoadMethod só corre uma vez no arranque da app — recriar
        // manualmente sempre que a cena recarrega (ex: voltar ao Menu Principal).
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;

        CreateMenu();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        IsDeathMenuOpen = false;
        CreateMenu();
    }

    private static void CreateMenu()
    {
        if (FindFirstObjectByType<DeathMenuUI>() != null) return;

        GameObject root = new GameObject("DeathMenu");
        root.AddComponent<DeathMenuUI>();
    }

    private void Awake()
    {
        BuildUI();
    }

    private void OnEnable()
    {
        PlayerRespawn.OnPlayerDied += ShowDeathMenu;
    }

    private void OnDisable()
    {
        PlayerRespawn.OnPlayerDied -= ShowDeathMenu;
    }

    // ── Mostrar / esconder ───────────────────────────────────────────────────

    private void ShowDeathMenu()
    {
        IsDeathMenuOpen = true;
        Time.timeScale = 0f;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerController = playerObj.GetComponent<PlayerController>();
            playerRespawn = playerObj.GetComponent<PlayerRespawn>();
            if (playerController != null) playerController.enabled = false;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        canvasObj.SetActive(true);
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

        canvasObj = new GameObject("DeathMenuCanvas");
        canvasObj.transform.SetParent(transform, false);

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 950;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        CreateImg(canvasObj.transform, "Overlay", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, cOverlay);

        deathPanel = CreatePanel(canvasObj.transform);
        CreateImg(deathPanel.transform, "Box", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, cBox);

        CreateTMP(deathPanel.transform, "Title", new Vector2(0f, 0.78f), new Vector2(1f, 0.94f),
                  "Morreste", 56f, cTitle, TextAlignmentOptions.Center);

        CreateButton(deathPanel.transform, "RespawnButton", "Renascer", 0.50f, OnRespawnClicked);
        CreateButton(deathPanel.transform, "MainMenuButton", "Menu Principal", 0.34f, OnMainMenuClicked);

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
        rt.sizeDelta = new Vector2(520f, 480f);
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

    // ── Ações dos botões ─────────────────────────────────────────────────────

    private void OnRespawnClicked()
    {
        if (playerRespawn != null) playerRespawn.Respawn();
        if (playerController != null) playerController.enabled = true;

        IsDeathMenuOpen = false;
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        canvasObj.SetActive(false);
    }

    private void OnMainMenuClicked()
    {
        IsDeathMenuOpen = false;
        Time.timeScale = 1f;
        canvasObj.SetActive(false);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
