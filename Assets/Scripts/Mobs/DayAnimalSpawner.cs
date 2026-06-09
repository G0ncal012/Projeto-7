using System.Collections.Generic;
using UnityEngine;

public class DayAnimalSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    [Tooltip("Se vazio, carrega automaticamente tudo em Resources/Animais/")]
    [SerializeField] private GameObject[] animalPrefabs;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<DayAnimalSpawner>() != null) return;
        GameObject go = new GameObject("DayAnimalSpawner");
        DontDestroyOnLoad(go);
        go.AddComponent<DayAnimalSpawner>();
    }

    [Header("Limite")]
    [SerializeField] private int maxAnimals = 30;

    [Header("Spawn")]
    [SerializeField] private float spawnMinDistance = 15f;
    [SerializeField] private float spawnMaxDistance = 70f;
    [SerializeField] private float spawnCooldown = 2f;
    [Tooltip("Quantos animais tenta spawnar por ciclo")]
    [SerializeField] private int spawnBatchSize = 4;
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

        if (animalPrefabs == null || animalPrefabs.Length == 0)
        {
            animalPrefabs = Resources.LoadAll<GameObject>("Animais");
            if (animalPrefabs.Length == 0)
                Debug.LogWarning("[DayAnimalSpawner] Nenhum prefab encontrado em Resources/Animais/");
            else if (debugLogs)
                Debug.Log($"[DayAnimalSpawner] Carregados {animalPrefabs.Length} prefabs de Resources/Animais/");
        }
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
            int toSpawn = Mathf.Min(spawnBatchSize, maxAnimals - activeAnimals.Count);
            bool anySpawned = false;
            for (int i = 0; i < toSpawn; i++)
                if (TrySpawn()) anySpawned = true;
            if (anySpawned) lastSpawnTime = Time.time;
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
        for (int i = 0; i < 30; i++)
        {
            Vector2 rnd = Random.insideUnitCircle.normalized * Random.Range(spawnMinDistance, spawnMaxDistance);
            Vector3 candidate = player.position + new Vector3(rnd.x, 0f, rnd.y);
            candidate.y = player.position.y + 50f;

            if (!Physics.Raycast(candidate, Vector3.down, out RaycastHit hit, 200f, ~LayerMask.GetMask("Player")))
                continue;

            if (hit.point.y < minGroundHeight) continue;

            position = hit.point + Vector3.up * 0.1f;
            return true;
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
