using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawna mobs apenas durante a noite. Requer DayNightCycle na cena.
/// 
/// Como usar:
///   1. Cria um objeto vazio na cena chamado "NightMobSpawner".
///   2. Adiciona este componente.
///   3. Arrasta os prefabs dos mobs para as listas mob1Prefabs / mob2Prefabs no Inspector.
///   4. Garante que o DayNightCycle está na cena.
/// </summary>
public class NightMobSpawner : MonoBehaviour
{
    [Header("Prefabs dos Mobs")]
    [Tooltip("Prefabs do Mob 1 (lento/forte) — ex. Zumbi")]
    [SerializeField] private GameObject[] mob1Prefabs;

    [Tooltip("Prefabs do Mob 2 (rápido/fraco) — ex. Lobo)")]
    [SerializeField] private GameObject[] mob2Prefabs;

    [Header("Limite de Mobs")]
    [Tooltip("Número máximo de mobs de cada tipo em simultâneo")]
    [SerializeField] private int maxMob1 = 3;
    [SerializeField] private int maxMob2 = 4;

    [Header("Spawn")]
    [Tooltip("Distância mínima do player para spawnar")]
    [SerializeField] private float spawnMinDistance = 15f;
    [Tooltip("Distância máxima do player para spawnar")]
    [SerializeField] private float spawnMaxDistance = 30f;
    [Tooltip("Intervalo mínimo entre spawns (segundos)")]
    [SerializeField] private float spawnCooldown = 8f;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    // ── Estado interno ────────────────────────────────────────────────────────
    private Transform player;
    private float lastSpawnTime = -999f;
    private Transform mobContainer;

    private readonly List<GameObject> activeMob1 = new List<GameObject>();
    private readonly List<GameObject> activeMob2 = new List<GameObject>();

    // ── Unity ─────────────────────────────────────────────────────────────────
    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
        else
            Debug.LogWarning("[NightMobSpawner] Player não encontrado. Certifica-te que o Player tem a tag 'Player'.");

        GameObject container = new GameObject("--- Mobs ---");
        mobContainer = container.transform;
    }

    void Update()
    {
        if (debugLogs)
            Debug.Log($"[NightMobSpawner] IsNight={DayNightCycle.IsNight} player={player != null} time={Time.time:F1}");

        if (!DayNightCycle.IsNight)
        {
            // Se amanheceu, destrói todos os mobs ativos
            DestroyAllMobs();
            return;
        }

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
            else return;
        }
        if (Time.time < lastSpawnTime + spawnCooldown) return;

        // Limpeza de referências nulas (mobs mortos)
        activeMob1.RemoveAll(m => m == null);
        activeMob2.RemoveAll(m => m == null);

        bool spawned = false;

        if (activeMob1.Count < maxMob1 && mob1Prefabs != null && mob1Prefabs.Length > 0)
            spawned |= TrySpawn(mob1Prefabs, activeMob1);

        if (activeMob2.Count < maxMob2 && mob2Prefabs != null && mob2Prefabs.Length > 0)
            spawned |= TrySpawn(mob2Prefabs, activeMob2);

        if (spawned)
            lastSpawnTime = Time.time;
    }

    // ── Spawn ─────────────────────────────────────────────────────────────────
    private bool TrySpawn(GameObject[] prefabs, List<GameObject> activeList)
    {
        Vector3 spawnPos;
        if (!FindSpawnPosition(out spawnPos))
        {
            if (debugLogs) Debug.Log("[NightMobSpawner] Não foi possível encontrar posição de spawn.");
            return false;
        }

        GameObject prefab = prefabs[Random.Range(0, prefabs.Length)];
        if (prefab == null) return false;

        GameObject mob = Instantiate(prefab, spawnPos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), mobContainer);
        activeList.Add(mob);

        if (debugLogs) Debug.Log($"[NightMobSpawner] Spawnou {mob.name} em {spawnPos}");
        return true;
    }

    private bool FindSpawnPosition(out Vector3 position)
    {
        // Tenta até 10 vezes encontrar uma posição válida
        for (int i = 0; i < 10; i++)
        {
            Vector2 rnd = Random.insideUnitCircle.normalized * Random.Range(spawnMinDistance, spawnMaxDistance);
            Vector3 candidate = player.position + new Vector3(rnd.x, 0f, rnd.y);

            // Coloca o mob acima do chão e usa Raycast para encontrar a superfície
            candidate.y = player.position.y + 50f;
            if (Physics.Raycast(candidate, Vector3.down, out RaycastHit hit, 200f, ~LayerMask.GetMask("Player")))
            {
                position = hit.point + Vector3.up * 0.1f;
                return true;
            }
        }

        position = Vector3.zero;
        return false;
    }

    private void DestroyAllMobs()
    {
        foreach (var m in activeMob1)
            if (m != null) Destroy(m);
        foreach (var m in activeMob2)
            if (m != null) Destroy(m);

        activeMob1.Clear();
        activeMob2.Clear();
    }

    // ── Gizmos ────────────────────────────────────────────────────────────────
    void OnDrawGizmosSelected()
    {
        if (player == null) return;

        Gizmos.color = new Color(0.5f, 0f, 1f, 0.15f);
        Gizmos.DrawWireSphere(player.position, spawnMaxDistance);

        Gizmos.color = new Color(0.5f, 0f, 1f, 0.3f);
        Gizmos.DrawWireSphere(player.position, spawnMinDistance);
    }
}
