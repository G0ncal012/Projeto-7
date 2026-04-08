using UnityEngine;

public class RockBreaking : MonoBehaviour, IHitable
{
    [Tooltip("Quantos golpes para partir")]
    [SerializeField] private int hitsToBreak = 4;

    [Tooltip("Prefab do drop de pedras")]
    [SerializeField] private GameObject dropPrefab;

    [Tooltip("Quantas pedras dropar")]
    [SerializeField] private int dropAmount = 3;

    [Tooltip("Tamanho dos drops")]
    [SerializeField] private float dropScale = 0.05f;

    private int currentHits = 0;
    private bool broken = false;

    public void Setup(GameObject drop, int amount = 3, float scale = 0.05f)
    {
        dropPrefab = drop;
        dropAmount = amount;
        dropScale  = scale;
    }

    public void Execute()
    {
        if (broken) return;

        currentHits++;
        Debug.Log($"[RockBreaking] Golpe {currentHits}/{hitsToBreak}");

        if (currentHits >= hitsToBreak)
            Break();
    }

    private void Break()
    {
        broken = true;

        if (dropPrefab != null)
        {
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
                GameObject drop = Instantiate(dropPrefab, transform.position + offset, rot);
                drop.transform.localScale = Vector3.one * dropScale;
            }
        }

        Destroy(gameObject);
    }
}
