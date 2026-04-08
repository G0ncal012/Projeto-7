using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class WoodItem : MonoBehaviour
{
    [SerializeField] private string itemName = "Madeira";
    [SerializeField] private float pickupRange = 3f;
    [SerializeField] private KeyCode pickupKey = KeyCode.F;

    // Prompt por instância — não static
    private GameObject promptUI;
    private TextMeshProUGUI promptText;
    private bool promptCreated = false;

    void Start()
    {
        CreatePromptUI();
    }

    void Update()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        bool lookingAt = false;

        if (Physics.Raycast(ray, out RaycastHit hit, pickupRange))
            lookingAt = hit.collider != null && hit.collider.gameObject == gameObject;

        if (lookingAt)
        {
            ShowPrompt($"[F] Apanhar {itemName}");
            if (Input.GetKeyDown(pickupKey))
                Pickup();
        }
        else
        {
            HidePrompt();
        }
    }

    void OnDestroy()
    {
        if (promptUI != null)
            Destroy(promptUI);
    }

    private void Pickup()
    {
        if (InventorySystem.Instance == null)
        {
            Debug.LogWarning("[WoodItem] InventorySystem não encontrado!");
            return;
        }
        if (InventorySystem.Instance.AddItem(itemName))
        {
            Destroy(gameObject);
        }
    }

    private void ShowPrompt(string text)
    {
        if (promptUI != null)
        {
            promptUI.SetActive(true);
            if (promptText != null) promptText.text = text;
        }
    }

    private void HidePrompt()
    {
        if (promptUI != null)
            promptUI.SetActive(false);
    }

    private void CreatePromptUI()
    {
        if (promptCreated) return;
        promptCreated = true;

        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        promptUI = new GameObject($"PickupPrompt_{gameObject.GetInstanceID()}");
        promptUI.transform.SetParent(canvas.transform, false);

        Image bg = promptUI.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.65f);

        RectTransform rt = promptUI.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.2f);
        rt.anchorMax = new Vector2(0.5f, 0.2f);
        rt.sizeDelta = new Vector2(260f, 40f);
        rt.anchoredPosition = Vector2.zero;

        GameObject textGo = new GameObject("Text");
        textGo.transform.SetParent(promptUI.transform, false);
        RectTransform trt = textGo.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(8f, 0f);
        trt.offsetMax = new Vector2(-8f, 0f);

        promptText = textGo.AddComponent<TextMeshProUGUI>();
        promptText.fontSize = 14f;
        promptText.color = Color.white;
        promptText.alignment = TextAlignmentOptions.Center;

        promptUI.SetActive(false);
    }
}
