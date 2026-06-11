using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Comer comida equipada na hotbar mantendo o botão direito do rato premido (estilo Minecraft).
/// Mostra uma barra de progresso enquanto come; ao completar, restaura fome e gasta 1 unidade.
/// </summary>
public class FoodConsumption : MonoBehaviour
{
    [Tooltip("Tempo (segundos) a segurar o botão direito para comer.")]
    [SerializeField] private float eatDuration = 2f;

    // Comidas válidas e quanta fome restauram. Acrescenta aqui novos alimentos.
    private static readonly Dictionary<string, float> foods = new Dictionary<string, float>
    {
        { "Carne", 30f },
    };

    private float eatTimer = 0f;
    private string eating = null;

    private GameObject barRoot;
    private Image barFill;
    private TextMeshProUGUI barLabel;

    void Update()
    {
        // Não comer com menus abertos.
        if (InventoryUI.IsOpen) { Reset(); return; }
        if (InventorySystem.Instance == null) { Reset(); return; }

        var stack = InventorySystem.Instance.hotbar[InventoryUI.SelectedHotbarSlot];
        string equipped = stack != null ? stack.itemName : null;

        bool isFood = equipped != null && foods.ContainsKey(equipped);

        if (isFood && Input.GetMouseButton(1))
        {
            if (eating != equipped) eatTimer = 0f; // trocou de comida a meio
            eating = equipped;
            eatTimer += Time.deltaTime;
            ShowBar(eatTimer / eatDuration, equipped);

            if (eatTimer >= eatDuration)
            {
                HungerSystem.Instance?.Eat(foods[equipped]);
                InventorySystem.Instance.RemoveItem(equipped, 1);
                Reset();
            }
        }
        else
        {
            Reset();
        }
    }

    private void Reset()
    {
        eatTimer = 0f;
        eating = null;
        if (barRoot != null && barRoot.activeSelf) barRoot.SetActive(false);
    }

    private void ShowBar(float t, string foodName)
    {
        if (barRoot == null) BuildBar();
        if (barRoot == null) return;

        barRoot.SetActive(true);
        if (barFill != null) barFill.fillAmount = Mathf.Clamp01(t);
        if (barLabel != null) barLabel.text = $"A comer {foodName}...";
    }

    private void BuildBar()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        barRoot = new GameObject("EatProgress");
        barRoot.transform.SetParent(canvas.transform, false);
        RectTransform rt = barRoot.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.18f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(220f, 18f);
        rt.anchoredPosition = Vector2.zero;
        barRoot.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(barRoot.transform, false);
        RectTransform frt = fill.AddComponent<RectTransform>();
        frt.anchorMin = new Vector2(0f, 0f);
        frt.anchorMax = new Vector2(1f, 1f);
        frt.offsetMin = new Vector2(2f, 2f);
        frt.offsetMax = new Vector2(-2f, -2f);
        barFill = fill.AddComponent<Image>();
        barFill.color = new Color(0.7f, 0.25f, 0.2f, 0.95f);
        barFill.type = Image.Type.Filled;
        barFill.fillMethod = Image.FillMethod.Horizontal;
        barFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        barFill.fillAmount = 0f;

        GameObject label = new GameObject("Label");
        label.transform.SetParent(barRoot.transform, false);
        RectTransform lrt = label.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;
        barLabel = label.AddComponent<TextMeshProUGUI>();
        barLabel.fontSize = 11f;
        barLabel.color = Color.white;
        barLabel.alignment = TextAlignmentOptions.Center;

        barRoot.SetActive(false);
    }
}
