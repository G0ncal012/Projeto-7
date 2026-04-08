using UnityEngine;

[RequireComponent(typeof(Health))]
public class TreeChopping : MonoBehaviour, IHitable
{
    [Tooltip("Arrasta aqui o TroncoFolhas")]
    [SerializeField] private GameObject troncoFolhas;

    [Tooltip("Arrasta aqui o Bottom (raiz que fica no chão)")]
    [SerializeField] private GameObject bottom;

    [Tooltip("Prefab do item de madeira que cai no chão")]
    [SerializeField] private GameObject woodItemPrefab;

    [Tooltip("Quantos itens de madeira dropar")]
    [SerializeField] private int woodAmount = 3;

    [Tooltip("Tamanho da madeira ao spawnar")]
    [SerializeField] private float woodScale = 0.1f;

    [Tooltip("Velocidade de tombamento")]
    [SerializeField] private float fallSpeed = 40f;

    [Tooltip("Segundos até desaparecer após cair (0 = nunca)")]
    [SerializeField] private float destroyAfter = 30f;

    private Health health;
    private bool hasFallen = false;

    void Awake()
    {
        health = GetComponent<Health>();
        health.OnDeath += Fall;
    }

    public void TakeDamage(float damage)
    {
        if (hasFallen) return;

        health.TakeDamage(damage);
        Debug.Log($"[TreeChopping] Vida restante: {health.CurrentHP:F0}/{health.MaxHP:F0}");

        TreeShaker shaker = GetComponent<TreeShaker>();
        if (shaker == null) shaker = gameObject.AddComponent<TreeShaker>();
        shaker.Shake();
    }

    private void Fall()
    {
        hasFallen = true;

        Vector3 spawnPosition = transform.position;

        Vector3 fallDirection = transform.forward;
        Camera cam = Camera.main;
        if (cam == null) cam = FindAnyObjectByType<Camera>();
        if (cam != null)
        {
            Vector3 toTree = transform.position - cam.transform.position;
            toTree.y = 0f;
            if (toTree.sqrMagnitude > 0.01f)
                fallDirection = toTree.normalized;
        }

        // Separa o Bottom antes da animação
        if (bottom != null)
            bottom.transform.SetParent(null, true);

        // Anima o TroncoFolhas
        if (troncoFolhas != null)
        {
            troncoFolhas.transform.SetParent(null, true);
            TreeFallAnimator animator = troncoFolhas.AddComponent<TreeFallAnimator>();
            animator.StartFall(fallDirection, fallSpeed, destroyAfter);

            if (woodItemPrefab != null)
                animator.SetWoodDrop(woodItemPrefab, woodAmount, spawnPosition, woodScale);
        }

        this.enabled = false;
    }
}