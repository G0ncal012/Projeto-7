using UnityEngine;
using UnityEngine.UI;

public class EntityHealthBar : MonoBehaviour
{
    [SerializeField] private float barWidth = 2f;
    [SerializeField] private float barHeight = 0.22f;
    [SerializeField] private float heightAbove = 2.2f;
    [SerializeField] private float hideDelay = 3f;

    private Image fillImage;
    private Transform barTransform;
    private Health health;
    private float hideTimer;
    private bool isVisible;

    private static Camera cachedCam;

    private void Awake()
    {
        health = GetComponent<Health>();
        if (health == null) { Destroy(this); return; }

        BuildBar();
        SetVisible(false);

        health.OnDamaged += OnDamaged;
        health.OnDeath += () => SetVisible(false);
    }

    private void BuildBar()
    {
        GameObject canvasObj = new GameObject("HPBar");
        canvasObj.transform.SetParent(transform, false);
        canvasObj.transform.localPosition = new Vector3(0f, heightAbove, 0f);

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 50;

        RectTransform crt = canvasObj.GetComponent<RectTransform>();
        crt.sizeDelta = new Vector2(barWidth, barHeight);
        barTransform = canvasObj.transform;

        GameObject bg = new GameObject("BG");
        bg.transform.SetParent(canvasObj.transform, false);
        RectTransform bgRt = bg.AddComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        bg.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.85f);

        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(canvasObj.transform, false);
        RectTransform fillRt = fillObj.AddComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = new Vector2(barWidth * 0.015f, barHeight * 0.1f);
        fillRt.offsetMax = new Vector2(-barWidth * 0.015f, -barHeight * 0.1f);
        fillImage = fillObj.AddComponent<Image>();
        fillImage.color = new Color(0.85f, 0.15f, 0.15f, 1f);
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillAmount = 1f;
    }

    private void LateUpdate()
    {
        if (!isVisible || barTransform == null) return;

        if (cachedCam == null) cachedCam = Camera.main;
        if (cachedCam != null)
            barTransform.rotation = cachedCam.transform.rotation;

        hideTimer -= Time.deltaTime;
        if (hideTimer <= 0f)
            SetVisible(false);
    }

    private void OnDamaged(float current, float max)
    {
        if (fillImage != null)
            fillImage.fillAmount = max > 0f ? current / max : 0f;

        SetVisible(current > 0f);
        hideTimer = hideDelay;
    }

    private void SetVisible(bool visible)
    {
        isVisible = visible;
        if (barTransform != null)
            barTransform.gameObject.SetActive(visible);
    }

    private void OnDestroy()
    {
        if (health != null)
            health.OnDamaged -= OnDamaged;
    }
}
