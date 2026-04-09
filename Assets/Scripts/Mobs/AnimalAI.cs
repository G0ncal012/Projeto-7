using UnityEngine;

/// <summary>
/// IA de animal. Por defeito é passivo (foge quando atacado).
/// Se canAttack=true, ataca o player de volta (ex: Wolf).
///
/// Como configurar o prefab:
///   1. Arrasta o FBX para a Scene.
///   2. Adiciona ao root: Health, AnimalAI, Rigidbody (freeze X/Z), Capsule Collider.
///   3. Tag: "Animal".
///   4. Arrasta o prefab de comida para foodDropPrefab.
///   5. No Animator Controller adiciona parâmetros: Speed (Float), Death (Trigger).
/// </summary>
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(Rigidbody))]
public class AnimalAI : MonoBehaviour, IHitable
{
    [Header("Movimento")]
    [SerializeField] private float wanderSpeed = 2f;
    [SerializeField] private float fleeSpeed = 5f;
    [SerializeField] private float wanderRadius = 12f;
    [SerializeField] private float idleTime = 3f;
    [SerializeField] private float fleeDuration = 5f;

    [Header("Ataque (opcional — ex: Wolf)")]
    [SerializeField] private bool canAttack = false;
    [SerializeField] private float attackRange = 1.8f;
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackCooldown = 1.5f;

    [Header("Drops")]
    [SerializeField] private GameObject foodDropPrefab;
    [SerializeField] private int foodDropAmount = 2;

    [Header("Animações")]
    [SerializeField] private string animSpeed = "Speed";
    [SerializeField] private string animDeath = "Death";
    [SerializeField] private string animAttack = "Attack";

    // ── Estado ───────────────────────────────────────────────────────────────
    private enum State { Idle, Wander, Flee, Chase, Attack, Dead }
    private State state = State.Idle;

    private Health health;
    private Rigidbody rb;
    private Transform player;
    private Animator animator;

    private Vector3 spawnPoint;
    private Vector3 wanderTarget;
    private float idleTimer;
    private float fleeTimer;
    private float lastAttackTime = -999f;

    // ── Unity ─────────────────────────────────────────────────────────────────
    void Awake()
    {
        health = GetComponent<Health>();
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        animator = GetComponentInChildren<Animator>();
        health.OnDeath += Die;
    }

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
        Invoke(nameof(InitSpawnPoint), 0.5f);
    }

    private void InitSpawnPoint()
    {
        spawnPoint = transform.position;
        idleTimer = Random.Range(1f, idleTime);
        SetNewWanderTarget();
        state = State.Idle;
    }

    void Update()
    {
        if (state == State.Dead) return;

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        UpdateState();
    }

    void FixedUpdate()
    {
        if (state == State.Dead) return;

        switch (state)
        {
            case State.Wander:
                MoveTo(wanderTarget, wanderSpeed);
                break;
            case State.Flee:
                if (player != null) FleeFrom(player.position);
                break;
            case State.Chase:
                if (player != null) MoveTo(player.position, fleeSpeed);
                break;
        }
    }

    // ── IHitable ──────────────────────────────────────────────────────────────
    public void TakeDamage(float damage)
    {
        if (state == State.Dead) return;
        health.TakeDamage(damage);
        Debug.Log($"[AnimalAI] {name} recebeu {damage} dano. Vida: {health.CurrentHP:F0}/{health.MaxHP:F0}");

        if (canAttack)
        {
            state = State.Chase;
        }
        else
        {
            fleeTimer = fleeDuration;
            state = State.Flee;
        }
    }

    // ── Lógica de estados ─────────────────────────────────────────────────────
    private void UpdateState()
    {
        float distToPlayer = player != null ? Vector3.Distance(transform.position, player.position) : float.MaxValue;

        switch (state)
        {
            case State.Idle:
                idleTimer -= Time.deltaTime;
                if (idleTimer <= 0f)
                {
                    SetNewWanderTarget();
                    state = State.Wander;
                }
                break;

            case State.Wander:
                float dx = transform.position.x - wanderTarget.x;
                float dz = transform.position.z - wanderTarget.z;
                if (dx * dx + dz * dz < 0.5f)
                {
                    idleTimer = Random.Range(1f, idleTime);
                    state = State.Idle;
                    SetAnimSpeed(0f);
                }
                break;

            case State.Flee:
                fleeTimer -= Time.deltaTime;
                if (fleeTimer <= 0f)
                {
                    SetNewWanderTarget();
                    state = State.Wander;
                }
                break;

            case State.Chase:
                if (distToPlayer <= attackRange)
                    state = State.Attack;
                break;

            case State.Attack:
                if (distToPlayer > attackRange * 1.3f)
                {
                    state = State.Chase;
                    break;
                }
                if (Time.time >= lastAttackTime + attackCooldown)
                {
                    lastAttackTime = Time.time;
                    Health playerHealth = player?.GetComponent<Health>();
                    playerHealth?.TakeDamage(attackDamage);
                    if (animator != null) animator.SetTrigger(animAttack);
                    Debug.Log($"[AnimalAI] {name} atacou o player por {attackDamage}.");
                }
                break;
        }
    }

    // ── Movimento ─────────────────────────────────────────────────────────────
    private void MoveTo(Vector3 target, float speed)
    {
        Vector3 dir = target - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) return;
        dir.Normalize();

        transform.rotation = Quaternion.Slerp(transform.rotation,
            Quaternion.LookRotation(dir), Time.fixedDeltaTime * 6f);

        rb.linearVelocity = new Vector3(dir.x * speed, rb.linearVelocity.y, dir.z * speed);
        SetAnimSpeed(speed);
    }

    private void FleeFrom(Vector3 threat)
    {
        Vector3 dir = transform.position - threat;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) dir = Random.insideUnitSphere;
        dir.Normalize();

        transform.rotation = Quaternion.Slerp(transform.rotation,
            Quaternion.LookRotation(dir), Time.fixedDeltaTime * 6f);

        rb.linearVelocity = new Vector3(dir.x * fleeSpeed, rb.linearVelocity.y, dir.z * fleeSpeed);
        SetAnimSpeed(fleeSpeed);
    }

    private void SetAnimSpeed(float speed)
    {
        if (animator != null)
            animator.SetFloat(animSpeed, speed);
    }

    private void SetNewWanderTarget()
    {
        Vector2 rnd = Random.insideUnitCircle * wanderRadius;
        wanderTarget = spawnPoint + new Vector3(rnd.x, 0f, rnd.y);
    }

    // ── Morte ─────────────────────────────────────────────────────────────────
    private void Die()
    {
        state = State.Dead;
        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;

        SetAnimSpeed(0f);
        if (animator != null)
            animator.SetTrigger(animDeath);

        if (foodDropPrefab != null)
        {
            Transform container = GetOrCreateContainer("--- Comida ---");
            for (int i = 0; i < foodDropAmount; i++)
            {
                Vector3 offset = new Vector3(Random.Range(-0.5f, 0.5f), 0.3f, Random.Range(-0.5f, 0.5f));
                Instantiate(foodDropPrefab, transform.position + offset, Quaternion.identity, container);
            }
        }

        Destroy(gameObject, 3f);
    }

    private static Transform GetOrCreateContainer(string name)
    {
        GameObject existing = GameObject.Find(name);
        if (existing != null) return existing.transform;
        return new GameObject(name).transform;
    }

    // ── Gizmos ────────────────────────────────────────────────────────────────
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0.4f, 0.2f);
        Gizmos.DrawWireSphere(Application.isPlaying ? spawnPoint : transform.position, wanderRadius);

        if (canAttack)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}
