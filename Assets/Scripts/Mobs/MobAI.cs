using UnityEngine;

/// <summary>
/// IA genérica para mobs. Requer um componente Health na mesma GameObject.
/// 
/// Como usar:
///   1. Importa o modelo 3D do mob (FAB ou outro).
///   2. Cria um prefab com o modelo.
///   3. Adiciona os componentes: Health, MobAI, Rigidbody (com Freeze Rotation X/Z), Capsule Collider.
///   4. Configura os parâmetros no Inspector.
///
/// Mob 1 (lento e forte) — ex. Zumbi:
///   moveSpeed=2, runSpeed=3, maxHP=150 (no Health), damage=20, detectionRange=10, attackRange=1.5
/// Mob 2 (rápido e fraco) — ex. Lobo:
///   moveSpeed=5, runSpeed=8, maxHP=60 (no Health), damage=10, detectionRange=15, attackRange=1.5
/// </summary>
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(Rigidbody))]
public class MobAI : MonoBehaviour
{
    // ── Configurações ─────────────────────────────────────────────────────────
    [Header("Movimento")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float runSpeed = 4f;

    [Header("Deteção e Combate")]
    [Tooltip("Distância a que o mob deteta o player")]
    [SerializeField] private float detectionRange = 12f;
    [Tooltip("Distância a que o mob ataca")]
    [SerializeField] private float attackRange = 1.8f;
    [Tooltip("Dano por ataque ao player")]
    [SerializeField] private float damage = 15f;
    [Tooltip("Tempo entre ataques (segundos)")]
    [SerializeField] private float attackCooldown = 1.5f;

    [Header("Patrulha")]
    [Tooltip("Distância máxima do ponto de spawn para patrulhar")]
    [SerializeField] private float patrolRadius = 8f;
    [Tooltip("Segundos parado no ponto de patrulha antes de se mover")]
    [SerializeField] private float idleTimeAtWaypoint = 2f;

    [Header("Drops ao morrer")]
    [SerializeField] private GameObject[] dropPrefabs;
    [Tooltip("Quantidade de cada drop")]
    [SerializeField] private int dropAmount = 1;

    // ── Estado interno ────────────────────────────────────────────────────────
    private enum State { Idle, Patrol, Chase, Attack, Dead }
    private State state = State.Idle;

    private Health health;
    private Rigidbody rb;
    private Transform player;

    private Vector3 spawnPoint;
    private Vector3 patrolTarget;
    private float idleTimer = 0f;
    private float lastAttackTime = -999f;

    // ── Unity ─────────────────────────────────────────────────────────────────
    void Awake()
    {
        health = GetComponent<Health>();
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        spawnPoint = transform.position;

        health.OnDeath += Die;
    }

    void Start()
    {
        // Tenta encontrar o player pela tag
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;

        SetNewPatrolTarget();
    }

    void Update()
    {
        if (state == State.Dead) return;

        UpdateState();
    }

    void FixedUpdate()
    {
        if (state == State.Dead) return;

        switch (state)
        {
            case State.Patrol:
                MoveTowards(patrolTarget, moveSpeed);
                break;
            case State.Chase:
                if (player != null) MoveTowards(player.position, runSpeed);
                break;
        }
    }

    // ── Lógica de estado ──────────────────────────────────────────────────────
    private void UpdateState()
    {
        float distToPlayer = player != null ? Vector3.Distance(transform.position, player.position) : float.MaxValue;

        switch (state)
        {
            case State.Idle:
                idleTimer -= Time.deltaTime;
                if (idleTimer <= 0f)
                {
                    SetNewPatrolTarget();
                    state = State.Patrol;
                }
                if (distToPlayer <= detectionRange)
                    state = State.Chase;
                break;

            case State.Patrol:
                if (distToPlayer <= detectionRange)
                {
                    state = State.Chase;
                    break;
                }
                if (Vector3.Distance(transform.position, patrolTarget) < 0.6f)
                {
                    idleTimer = idleTimeAtWaypoint;
                    state = State.Idle;
                }
                break;

            case State.Chase:
                if (distToPlayer > detectionRange * 1.5f)
                {
                    // Perdeu o player — volta a patrulhar
                    SetNewPatrolTarget();
                    state = State.Patrol;
                    break;
                }
                if (distToPlayer <= attackRange)
                    state = State.Attack;
                break;

            case State.Attack:
                if (distToPlayer > attackRange * 1.3f)
                {
                    state = State.Chase;
                    break;
                }
                TryAttack();
                break;
        }
    }

    private void TryAttack()
    {
        if (Time.time < lastAttackTime + attackCooldown) return;
        lastAttackTime = Time.time;

        if (player == null) return;

        // Aplica dano ao player se ele tiver o componente Health
        Health playerHealth = player.GetComponent<Health>();
        if (playerHealth != null)
            playerHealth.TakeDamage(damage);

        Debug.Log($"[MobAI] {name} atacou o player por {damage} de dano.");
    }

    private void MoveTowards(Vector3 target, float speed)
    {
        Vector3 dir = (target - transform.position);
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.01f) return;

        dir.Normalize();

        // Roda suavemente para o alvo
        Quaternion targetRot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.fixedDeltaTime * 8f);

        Vector3 vel = dir * speed;
        vel.y = rb.linearVelocity.y;
        rb.linearVelocity = vel;
    }

    private void SetNewPatrolTarget()
    {
        Vector2 rnd = Random.insideUnitCircle * patrolRadius;
        patrolTarget = spawnPoint + new Vector3(rnd.x, 0f, rnd.y);
    }

    private void Die()
    {
        state = State.Dead;
        rb.linearVelocity = Vector3.zero;

        // Dropa itens
        foreach (var prefab in dropPrefabs)
        {
            if (prefab == null) continue;
            for (int i = 0; i < dropAmount; i++)
            {
                Vector3 offset = new Vector3(Random.Range(-0.5f, 0.5f), 0.3f, Random.Range(-0.5f, 0.5f));
                Instantiate(prefab, transform.position + offset, Quaternion.identity);
            }
        }

        Destroy(gameObject, 3f);
    }

    // ── Gizmos ────────────────────────────────────────────────────────────────
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 0f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = new Color(1f, 0f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
        Gizmos.DrawWireSphere(Application.isPlaying ? spawnPoint : transform.position, patrolRadius);
    }
}
