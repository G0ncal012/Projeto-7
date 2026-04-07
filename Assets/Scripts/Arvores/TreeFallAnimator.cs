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

    public void StartFall(Vector3 direction, float speed, float destroyAfter)
    {
        this.fallDirection = direction;
        this.fallSpeed = speed;
        this.destroyAfter = destroyAfter;
        this.falling = true;

        if (destroyAfter > 0f)
            Destroy(gameObject, destroyAfter);
    }

    public void SetWoodDrop(GameObject prefab, int amount, Vector3 spawnPos)
    {
        woodPrefab = prefab;
        woodAmount = amount;
        woodSpawnPos = spawnPos;
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
                Random.Range(-1.5f, 1.5f),
                0.5f,
                Random.Range(-1.5f, 1.5f)
            );
            Instantiate(woodPrefab, woodSpawnPos + offset, Quaternion.identity);
        }
    }
}
