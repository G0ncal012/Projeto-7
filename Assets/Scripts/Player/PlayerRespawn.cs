using UnityEngine;

[RequireComponent(typeof(Health))]
[RequireComponent(typeof(Rigidbody))]
public class PlayerRespawn : MonoBehaviour
{
    [Tooltip("Segundos de invencibilidade após respawn")]
    [SerializeField] private float invincibilityDuration = 2f;

    private Health health;
    private Rigidbody rb;
    private Vector3 spawnPoint;
    private bool isInvincible = false;

    /// <summary>Disparado quando o player morre — usado pelo DeathMenuUI para mostrar o menu.</summary>
    public static event System.Action OnPlayerDied;

    void Awake()
    {
        health = GetComponent<Health>();
        rb = GetComponent<Rigidbody>();
        spawnPoint = transform.position;
        health.OnDeath += OnDie;
    }

    void OnDestroy()
    {
        health.OnDeath -= OnDie;
    }

    private void OnDie()
    {
        Debug.Log("[Player] Morreu.");
        rb.linearVelocity = Vector3.zero;
        OnPlayerDied?.Invoke();
    }

    /// <summary>Repõe o player no ponto de spawn com vida cheia. Chamado pelo DeathMenuUI.</summary>
    public void Respawn()
    {
        if (InventorySystem.Instance != null)
            InventorySystem.Instance.ClearAll();

        rb.linearVelocity = Vector3.zero;
        transform.position = spawnPoint;
        health.SetMaxHP(health.MaxHP, refillHP: true);
        HungerSystem.Instance?.ResetHunger();

        isInvincible = true;
        Invoke(nameof(EndInvincibility), invincibilityDuration);

        FindFirstObjectByType<NightMobSpawner>()?.ResetMobs();
        FindFirstObjectByType<CastleArena>()?.ResetArena();
    }

    private void EndInvincibility()
    {
        isInvincible = false;
    }

    /// <summary>Atualiza o ponto de spawn para a posição atual. Chamado pelo MapGenerator
    /// depois de colocar o player numa posição segura no mapa.</summary>
    public void UpdateSpawnPoint()
    {
        spawnPoint = transform.position;
    }

    public bool IsInvincible => isInvincible;
}
