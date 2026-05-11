using UnityEngine;

public class PickaxeTool : MonoBehaviour
{
    [Header("Configurações")]
    [SerializeField] private float range = 4f;
    [SerializeField] private float hitCooldown = 0.5f;
    [SerializeField] private float damagePerHit = 25f;
    [SerializeField] private LayerMask raycastMask = ~0;

    [Header("Highlight")]
    [SerializeField] private Color highlightColor = new Color(0.3f, 0.6f, 1f, 0.7f);

    private bool isActive = false;
    private float lastHitTime = -999f;
    private Camera cam;

    private GameObject lastHighlighted;
    private Renderer[] savedRenderers;
    private Material[] savedMaterials;

    private void Start()
    {
        cam = FindAnyObjectByType<Camera>();
    }

    private void Update()
    {
        if (cam == null) cam = FindAnyObjectByType<Camera>();
        if (!isActive) { ClearHighlight(); return; }

        GameObject target = GetTarget();
        UpdateHighlight(target);

        if (Input.GetMouseButtonDown(0) && Time.time >= lastHitTime + hitCooldown)
        {
            if (target != null)
            {
                lastHitTime = Time.time;
                ClearHighlight();
                IHitable hitable = target.GetComponentInParent<IHitable>();
                hitable?.TakeDamage(damagePerHit);
            }
        }
    }

    public void SetPickaxeActive(bool active)
    {
        isActive = active;
        if (!active) ClearHighlight();
    }

    private GameObject GetTarget()
    {
        if (cam == null) return null;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit[] hits = Physics.RaycastAll(ray, range, raycastMask, QueryTriggerInteraction.Collide);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var hit in hits)
        {
            if (hit.collider == null) continue;
            Transform t = hit.collider.transform;
            while (t != null)
            {
                if (t.GetComponent<RockBreaking>() != null) return t.gameObject;
                t = t.parent;
            }
        }
        return null;
    }

    private void UpdateHighlight(GameObject target)
    {
        if (target == lastHighlighted) return;
        ClearHighlight();
        if (target == null) return;

        lastHighlighted = target;
        savedRenderers = target.GetComponentsInChildren<Renderer>();
        savedMaterials = new Material[savedRenderers.Length];

        for (int i = 0; i < savedRenderers.Length; i++)
        {
            savedMaterials[i] = savedRenderers[i].material;
            Material m = new Material(savedRenderers[i].material);
            m.color = Color.Lerp(m.color, highlightColor, 0.55f);
            savedRenderers[i].material = m;
        }
    }

    private void ClearHighlight()
    {
        if (lastHighlighted == null) return;
        if (savedRenderers != null)
            for (int i = 0; i < savedRenderers.Length; i++)
                if (savedRenderers[i] != null && savedMaterials[i] != null)
                    savedRenderers[i].material = savedMaterials[i];

        lastHighlighted = null;
        savedRenderers = null;
        savedMaterials = null;
    }
}
