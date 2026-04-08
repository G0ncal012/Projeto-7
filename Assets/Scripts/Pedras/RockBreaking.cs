using UnityEngine;

[RequireComponent(typeof(Health))]
public class RockBreaking : MonoBehaviour, IHitable
{
    [Tooltip("Prefab do drop de pedras")]
    [SerializeField] private GameObject dropPrefab;

    [Tooltip("Quantas pedras dropar")]
    [SerializeField] private int dropAmount = 3;

    [Tooltip("Tamanho dos drops")]
    [SerializeField] private float dropScale = 0.05f;

    private Health health;
    private bool broken = false;

    void Awake()
    {
        health = GetComponent<Health>();
        health.OnDeath += Break;
    }

    public void Setup(GameObject drop, int amount = 3, float scale = 0.05f)
    {
        dropPrefab = drop;
        dropAmount = amount;
        dropScale  = scale;
    }

    public void TakeDamage(float damage)
    {
        if (broken) return;

        health.TakeDamage(damage);
        Debug.Log($"[RockBreaking] Vida restante: {health.CurrentHP:F0}/{health.MaxHP:F0}");
    }

    private void Break()
    {
        broken = true;

        if (dropPrefab != null)
        {
            Transform container = GetOrCreateContainer("--- Pedras ---");

            for (int i = 0; i < dropAmount; i++)
            {
                Vector3 offset = new Vector3(
                    Random.Range(-1f, 1f),
                    0.3f,
                    Random.Range(-1f, 1f)
                );
                Quaternion rot = Quaternion.Euler(
                    Random.Range(0f, 30f),
                    Random.Range(0f, 360f),
                    Random.Range(0f, 30f)
                );
                GameObject drop = Instantiate(dropPrefab, transform.position + offset, rot, container);
                drop.transform.localScale = Vector3.one * dropScale;
            }
        }

        Destroy(gameObject);
    }

    private static Transform GetOrCreateContainer(string name)
    {
        GameObject existing = GameObject.Find(name);
        if (existing != null) return existing.transform;
        return new GameObject(name).transform;
    }
}
