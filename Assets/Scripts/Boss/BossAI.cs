using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Health))]
[RequireComponent(typeof(Rigidbody))]
public class BossAI : MonoBehaviour, IHitable
{
    // Evento estático — CastleArena subscreve para remover a barreira ao morrer
    public static event System.Action OnBossDied;
    public static BossAI ActiveBoss { get; private set; }

    [Header("Stats")]
    [SerializeField] private float maxHP = 150f;

    [Header("Fase 1 (HP > 50%)")]
    [SerializeField] private float p1MoveSpeed = 2.5f;
    [SerializeField] private float p1Damage = 20f;
    [SerializeField] private float p1AttackCooldown = 1.5f;

    [Header("Fase 2 (HP ≤ 50%)")]
    [SerializeField] private float p2MoveSpeed = 4.5f;
    [SerializeField] private float p2Damage = 35f;
    [SerializeField] private float p2AttackCooldown = 0.9f;
    [SerializeField] private int minionCount = 3;

    [Header("Combate")]
    [SerializeField] private float detectionRange = 40f;
    [SerializeField] private float attackRange = 2f;

    // ── Estado ───────────────────────────────────────────────────────────────
    private enum Phase { One, Two }
    private enum State { Idle, Chase, Attack, Dead }

    private Phase currentPhase = Phase.One;
    private State state = State.Idle;

    private Health health;
    private Rigidbody rb;
    private Transform player;
    private Animator animator;

    private float lastAttackTime = -999f;
    private bool minionsSpawned = false;

    private float MoveSpeed => currentPhase == Phase.One ? p1MoveSpeed : p2MoveSpeed;
    private float Damage    => currentPhase == Phase.One ? p1Damage    : p2Damage;
    private float Cooldown  => currentPhase == Phase.One ? p1AttackCooldown : p2AttackCooldown;

    // ── Unity ─────────────────────────────────────────────────────────────────
    private void Awake()
    {
        // Remove MobAI se presente (prefab reutilizado do esqueleto normal)
        MobAI existingAI = GetComponent<MobAI>();
        if (existingAI != null) Destroy(existingAI);

        health = GetComponent<Health>();
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        health.SetMaxHP(maxHP, refillHP: true);
        health.OnDamaged += OnDamaged;
        health.OnDeath += Die;

        animator = GetComponentInChildren<Animator>();

        ActiveBoss = this;
    }

    private void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

        // Barra de vida do boss no topo do ecrã
        if (GetComponent<EntityHealthBar>() != null)
            Destroy(GetComponent<EntityHealthBar>());
    }

    private void Update()
    {
        if (state == State.Dead) return;

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
            else return;
        }

        CheckPhaseTransition();
        UpdateState();
    }

    private void FixedUpdate()
    {
        if (state == State.Dead || state == State.Idle) return;

        if (state == State.Chase && player != null)
            MoveTowards(player.position);
    }

    // ── IHitable ──────────────────────────────────────────────────────────────
    public void TakeDamage(float damage)
    {
        if (state == State.Dead) return;
        health.TakeDamage(damage);
        Debug.Log($"[Boss] Recebeu {damage} dano. HP: {health.CurrentHP:F0}/{health.MaxHP:F0}");
    }

    // ── Fases ────────────────────────────────────────────────────────────────
    private void CheckPhaseTransition()
    {
        if (currentPhase == Phase.One && health.CurrentHP <= health.MaxHP * 0.5f)
        {
            currentPhase = Phase.Two;
            Debug.Log("[Boss] Fase 2 ativada!");

            if (!minionsSpawned)
            {
                minionsSpawned = true;
                StartCoroutine(SpawnMinions());
            }
        }
    }

    private IEnumerator SpawnMinions()
    {
        yield return new WaitForSeconds(0.5f);

        GameObject[] skeletonPrefabs = Resources.LoadAll<GameObject>("Mobs");
        if (skeletonPrefabs == null || skeletonPrefabs.Length == 0) yield break;

        GameObject prefab = skeletonPrefabs[0];
        for (int i = 0; i < minionCount; i++)
        {
            float angle = i * (360f / minionCount) * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Sin(angle) * 5f, 0f, Mathf.Cos(angle) * 5f);
            Vector3 spawnPos = transform.position + offset;

            // Ajusta Y ao terreno
            if (Physics.Raycast(spawnPos + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 30f))
                spawnPos.y = hit.point.y + 0.1f;

            Instantiate(prefab, spawnPos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
        }

        Debug.Log($"[Boss] {minionCount} minions invocados.");
    }

    // ── Estado ───────────────────────────────────────────────────────────────
    private void UpdateState()
    {
        if (player == null) return;
        float dist = Vector3.Distance(transform.position, player.position);

        switch (state)
        {
            case State.Idle:
                if (dist <= detectionRange) state = State.Chase;
                break;

            case State.Chase:
                if (dist <= attackRange) state = State.Attack;
                break;

            case State.Attack:
                if (dist > attackRange * 1.4f) { state = State.Chase; break; }
                if (Time.time >= lastAttackTime + Cooldown)
                {
                    lastAttackTime = Time.time;
                    PlayerRespawn respawn = player.GetComponent<PlayerRespawn>();
                    if (respawn != null && respawn.IsInvincible) break;
                    player.GetComponent<Health>()?.TakeDamage(Damage);
                    if (animator != null) animator.SetTrigger("Attack");
                    Debug.Log($"[Boss] Atacou player por {Damage}.");
                }
                break;
        }
    }

    // ── Movimento ─────────────────────────────────────────────────────────────
    private void MoveTowards(Vector3 target)
    {
        Vector3 dir = (target - transform.position);
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) return;
        dir.Normalize();

        transform.rotation = Quaternion.Slerp(transform.rotation,
            Quaternion.LookRotation(dir), Time.fixedDeltaTime * 8f);

        rb.linearVelocity = new Vector3(dir.x * MoveSpeed, rb.linearVelocity.y, dir.z * MoveSpeed);

        if (animator != null) animator.SetFloat("Speed", MoveSpeed);
    }

    // ── Dano recebido ─────────────────────────────────────────────────────────
    private void OnDamaged(float current, float max)
    {
        if (state != State.Dead && state == State.Idle)
            state = State.Chase;
    }

    // ── Morte ─────────────────────────────────────────────────────────────────
    private void Die()
    {
        if (state == State.Dead) return;
        state = State.Dead;

        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;

        foreach (Collider col in GetComponentsInChildren<Collider>())
            col.enabled = false;

        if (animator != null)
        {
            animator.SetFloat("Speed", 0f);
            animator.SetTrigger("Death");
        }

        ActiveBoss = null;
        OnBossDied?.Invoke();
        Debug.Log("[Boss] Boss derrotado!");

        Destroy(gameObject, 4f);
    }

    private void OnGUI()
    {
        if (health == null) return;
        float pct = health.MaxHP > 0f ? health.CurrentHP / health.MaxHP : 0f;
        Rect bg   = new Rect(Screen.width / 2f - 150f, 8f, 300f, 24f);
        Rect fill = new Rect(bg.x + 2f, bg.y + 2f, (bg.width - 4f) * pct, bg.height - 4f);
        Rect lbl  = new Rect(bg.x, bg.y, bg.width, bg.height);
        GUI.color = Color.black; GUI.DrawTexture(bg,   Texture2D.whiteTexture);
        GUI.color = Color.red;   GUI.DrawTexture(fill, Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(lbl, $"  Boss HP: {health.CurrentHP:F0} / {health.MaxHP:F0}");
        GUI.color = Color.white;
    }

    private void OnDestroy()
    {
        if (ActiveBoss == this) ActiveBoss = null;
        health.OnDamaged -= OnDamaged;
        health.OnDeath -= Die;
    }
}
