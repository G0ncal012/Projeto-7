using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Item de comida no chão. Ao apanhar restaura fome diretamente.
/// </summary>
public class FoodPickup : MonoBehaviour
{
    [SerializeField] private float hungerRestore = 30f;
    [SerializeField] private string foodName = "Carne";
    [SerializeField] private float pickupRange = 3f;
    [SerializeField] private KeyCode pickupKey = KeyCode.F;

    private GameObject promptUI;
    private TextMeshProUGUI promptText;

    void Start() => CreatePromptUI();

    void Update()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        bool lookingAt = Physics.Raycast(ray, out RaycastHit hit, pickupRange)
                         && hit.collider != null
                         && hit.collider.gameObject == gameObject;

        if (lookingAt)
        {
            ShowPrompt($"[F] Comer {foodName} (+{hungerRestore} fome)");
            if (Input.GetKeyDown(pickupKey))
                Eat();
        }
        else
        {
            HidePrompt();
        }
    }

    void OnDestroy()
    {
        if (promptUI != null) Destroy(promptUI);
    }

    private void Eat()
    {
        HungerSystem.Instance?.Eat(hungerRestore);
        Destroy(gameObject);
    }

    private void ShowPrompt(string text)
    {
        if (promptUI != null) { promptUI.SetActive(true); if (promptText != null) promptText.text = text; }
    }

    private void HidePrompt()
    {
        if (promptUI != null) promptUI.SetActive(false);
    }

    private void CreatePromptUI()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        promptUI = new GameObject($"FoodPrompt_{GetInstanceID()}");
        promptUI.transform.SetParent(canvas.transform, false);

        Image bg = promptUI.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.65f);
        RectTransform rt = promptUI.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.2f);
        rt.anchorMax = new Vector2(0.5f, 0.2f);
        rt.sizeDelta = new Vector2(280f, 40f);
        rt.anchoredPosition = Vector2.zero;

        GameObject textGo = new GameObject("Text");
        textGo.transform.SetParent(promptUI.transform, false);
        RectTransform trt = textGo.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(8f, 0f); trt.offsetMax = new Vector2(-8f, 0f);

        promptText = textGo.AddComponent<TextMeshProUGUI>();
        promptText.fontSize = 14f;
        promptText.color = Color.white;
        promptText.alignment = TextAlignmentOptions.Center;

        promptUI.SetActive(false);
    }
}
