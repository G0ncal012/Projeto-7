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
        Debug.Log("[Player] Morreu. A fazer respawn...");

        if (InventorySystem.Instance != null)
            InventorySystem.Instance.ClearAll();

        rb.linearVelocity = Vector3.zero;
        transform.position = spawnPoint;
        health.SetMaxHP(health.MaxHP, refillHP: true);

        isInvincible = true;
        Invoke(nameof(EndInvincibility), invincibilityDuration);
    }

    private void EndInvincibility()
    {
        isInvincible = false;
    }

    public bool IsInvincible => isInvincible;
}
