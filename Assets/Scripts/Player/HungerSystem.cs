using UnityEngine;

/// <summary>
/// Sistema de fome do player.
/// - Drena ao longo do tempo (mais rápido com inventário pesado).
/// - Drena ao bater em qualquer coisa (ConsumeOnHit).
/// - Quando chega a 0 causa dano de starvação.
/// - Eat(amount) restaura fome.
/// </summary>
[RequireComponent(typeof(Health))]
public class HungerSystem : MonoBehaviour
{
    public static HungerSystem Instance { get; private set; }

    [Header("Fome")]
    [SerializeField] private float maxHunger = 100f;
    [Tooltip("Fome consumida por segundo base")]
    [SerializeField] private float baseDecayRate = 1f;
    [Tooltip("Fome extra por segundo por cada kg no inventário")]
    [SerializeField] private float weightDecayMultiplier = 0.05f;

    [Header("Ações")]
    [Tooltip("Fome consumida por cada golpe")]
    [SerializeField] private float hungerPerHit = 2f;

    [Header("Starvação")]
    [Tooltip("Dano por segundo quando fome = 0")]
    [SerializeField] private float starvationDamagePerSecond = 5f;

    private float currentHunger;
    private Health health;

    public float CurrentHunger => currentHunger;
    public float MaxHunger => maxHunger;
    public event System.Action<float, float> OnHungerChanged;

    void Awake()
    {
        Instance = this;
        currentHunger = maxHunger;
        health = GetComponent<Health>();
    }

    void Update()
    {
        float weight = InventorySystem.Instance != null ? InventorySystem.Instance.GetTotalWeight() : 0f;
        float decay = baseDecayRate + weight * weightDecayMultiplier;

        currentHunger = Mathf.Max(0f, currentHunger - decay * Time.deltaTime);
        OnHungerChanged?.Invoke(currentHunger, maxHunger);

        if (currentHunger <= 0f)
            health.TakeDamage(starvationDamagePerSecond * Time.deltaTime);
    }

    public void ConsumeOnHit()
    {
        currentHunger = Mathf.Max(0f, currentHunger - hungerPerHit);
        OnHungerChanged?.Invoke(currentHunger, maxHunger);
    }

    public void Eat(float amount)
    {
        currentHunger = Mathf.Min(maxHunger, currentHunger + amount);
        OnHungerChanged?.Invoke(currentHunger, maxHunger);
        Debug.Log($"[HungerSystem] Comeu. Fome: {currentHunger:F0}/{maxHunger:F0}");
    }

    public void ResetHunger()
    {
        currentHunger = maxHunger;
        OnHungerChanged?.Invoke(currentHunger, maxHunger);
    }
}
