using UnityEngine;

/// <summary>
/// Componente de vida reutilizável — usado em árvores, pedras, mobs e no player.
/// </summary>
public class Health : MonoBehaviour
{
    [Tooltip("Vida máxima")]
    [SerializeField] private float maxHP = 100f;

    private float currentHP;

    /// <summary>Chamado quando o objeto recebe dano. Parâmetros: vidaAtual, vidaMax.</summary>
    public event System.Action<float, float> OnDamaged;

    /// <summary>Chamado quando a vida chega a zero.</summary>
    public event System.Action OnDeath;

    public float MaxHP => maxHP;
    public float CurrentHP => currentHP;
    public bool IsAlive => currentHP > 0f;

    void Awake()
    {
        currentHP = maxHP;
    }

    public void TakeDamage(float damage)
    {
        if (!IsAlive) return;

        currentHP = Mathf.Max(0f, currentHP - damage);
        OnDamaged?.Invoke(currentHP, maxHP);

        if (currentHP <= 0f)
            OnDeath?.Invoke();
    }

    public void Heal(float amount)
    {
        if (!IsAlive) return;
        currentHP = Mathf.Min(maxHP, currentHP + amount);
        OnDamaged?.Invoke(currentHP, maxHP);
    }

    public void SetMaxHP(float value, bool refillHP = true)
    {
        maxHP = value;
        if (refillHP) currentHP = maxHP;
    }
}
