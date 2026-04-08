using UnityEngine;

public class TreeFallAnimator : MonoBehaviour
{
    private Vector3 fallDirection;
    private float fallSpeed;
    private float destroyAfter;
    private float totalRotation = 0f;
    private bool falling = false;

    // Wood drop
    private GameObject woodPrefab;
    private int woodAmount;
    private Vector3 woodSpawnPos;
    private float woodScale = 0.1f;

    public void StartFall(Vector3 direction, float speed, float destroyAfter)
    {
        this.fallDirection = direction;
        this.fallSpeed = speed;
        this.destroyAfter = destroyAfter;
        this.falling = true;

        if (destroyAfter > 0f)
            Destroy(gameObject, destroyAfter);
    }

    public void SetWoodDrop(GameObject prefab, int amount, Vector3 spawnPos, float scale = 0.1f)
    {
        woodPrefab = prefab;
        woodAmount = amount;
        woodSpawnPos = spawnPos;
        woodScale = scale;
    }

    void Update()
    {
        if (!falling) return;

        if (totalRotation >= 90f)
        {
            falling = false;
            SpawnWood();
            Destroy(this);
            return;
        }

        float t = totalRotation / 90f;
        float acceleration = Mathf.Pow(t, 2f) * 4f + 0.1f;
        float step = fallSpeed * acceleration * Time.deltaTime;
        step = Mathf.Min(step, 90f - totalRotation);

        Vector3 torqueAxis = Vector3.Cross(Vector3.up, fallDirection).normalized;
        transform.Rotate(torqueAxis, step, Space.World);
        totalRotation += step;
    }

    private void SpawnWood()
    {
        if (woodPrefab == null) return;

        for (int i = 0; i < woodAmount; i++)
        {
            Vector3 offset = new Vector3(
                Random.Range(-2f, 2f),
                0.3f,
                Random.Range(-2f, 2f)
            );
            // Deita a madeira no chão (X=90) com rotação aleatória
            Quaternion rotation = Quaternion.Euler(90f, Random.Range(0f, 360f), 0f);
            GameObject wood = Instantiate(woodPrefab, woodSpawnPos + offset, rotation);
            wood.transform.localScale = Vector3.one * woodScale;
        }
    }
}
