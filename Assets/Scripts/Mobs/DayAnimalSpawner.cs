using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawna animais durante o dia. Despawna à noite.
/// Requer DayNightCycle na cena.
///
/// Como usar:
///   1. Cria um objeto vazio chamado "DayAnimalSpawner".
///   2. Adiciona este componente.
///   3. Arrasta os prefabs dos animais para animalPrefabs.
/// </summary>
public class DayAnimalSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject[] animalPrefabs;

    [Header("Limite")]
    [SerializeField] private int maxAnimals = 5;

    [Header("Spawn")]
    [SerializeField] private float spawnMinDistance = 15f;
    [SerializeField] private float spawnMaxDistance = 40f;
    [SerializeField] private float spawnCooldown = 10f;
    [Tooltip("Altura mínima do terreno para spawnar (evita água)")]
    [SerializeField] private float minGroundHeight = 0.5f;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private Transform player;
    private float lastSpawnTime = -999f;
    private Transform animalContainer;
    private readonly List<GameObject> activeAnimals = new List<GameObject>();

    void Start()
    {
        animalContainer = new GameObject("--- Animais ---").transform;
    }

    void Update()
    {
        if (DayNightCycle.IsNight)
        {
            DestroyAll();
            return;
        }

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
            else return;
        }

        if (Time.time < lastSpawnTime + spawnCooldown) return;

        activeAnimals.RemoveAll(a => a == null);

        if (activeAnimals.Count < maxAnimals && animalPrefabs != null && animalPrefabs.Length > 0)
        {
            if (TrySpawn())
                lastSpawnTime = Time.time;
        }
    }

    private bool TrySpawn()
    {
        if (!FindSpawnPosition(out Vector3 pos))
        {
            if (debugLogs) Debug.Log("[DayAnimalSpawner] Sem posição de spawn válida.");
            return false;
        }

        GameObject prefab = animalPrefabs[Random.Range(0, animalPrefabs.Length)];
        if (prefab == null) return false;

        GameObject animal = Instantiate(prefab, pos,
            Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), animalContainer);
        activeAnimals.Add(animal);

        if (debugLogs) Debug.Log($"[DayAnimalSpawner] Spawnou {animal.name} em {pos}");
        return true;
    }

    private bool FindSpawnPosition(out Vector3 position)
    {
        for (int i = 0; i < 15; i++)
        {
            Vector2 rnd = Random.insideUnitCircle.normalized * Random.Range(spawnMinDistance, spawnMaxDistance);
            Vector3 candidate = player.position + new Vector3(rnd.x, 0f, rnd.y);
            candidate.y = player.position.y + 50f;

            if (Physics.Raycast(candidate, Vector3.down, out RaycastHit hit, 200f, ~LayerMask.GetMask("Player")))
            {
                if (hit.point.y < minGroundHeight) continue;
                if (HitIsOnTaggedObject(hit, "Tree")) continue;
                position = hit.point + Vector3.up * 0.1f;
                return true;
            }
        }

        position = Vector3.zero;
        return false;
    }

    private void DestroyAll()
    {
        foreach (var a in activeAnimals)
            if (a != null) Destroy(a);
        activeAnimals.Clear();
    }

    private static bool HitIsOnTaggedObject(RaycastHit hit, string tag)
    {
        Transform t = hit.collider.transform;
        while (t != null)
        {
            if (t.CompareTag(tag)) return true;
            t = t.parent;
        }
        return false;
    }

    void OnDrawGizmosSelected()
    {
        if (player == null) return;
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.15f);
        Gizmos.DrawWireSphere(player.position, spawnMaxDistance);
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.3f);
        Gizmos.DrawWireSphere(player.position, spawnMinDistance);
    }
}
