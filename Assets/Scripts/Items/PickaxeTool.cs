using UnityEngine;

public class PickaxeTool : MonoBehaviour
{
    [Header("Configurações")]
    [SerializeField] private float range = 4f;
    [SerializeField] private float hitCooldown = 0.5f;
    [SerializeField] private float damagePerHit = 25f;
    [SerializeField] private LayerMask raycastMask = ~0;
    [SerializeField] private bool debugLogs = false;

    [Header("Highlight")]
    [SerializeField] private Color highlightColor = new Color(0.3f, 0.6f, 1f, 0.7f);

    private float lastHitTime = -999f;
    private Camera cam;
    private BuildingManager buildingManager;
    private float nextManagerSearch = 0f;

    private GameObject lastHighlighted;
    private Renderer[] savedRenderers;
    private Material[] savedMaterials;

    private void Start()
    {
        cam = Camera.main != null ? Camera.main : FindAnyObjectByType<Camera>();
        buildingManager = FindAnyObjectByType<BuildingManager>();
    }

    private void Update()
    {
        if (cam == null) cam = Camera.main != null ? Camera.main : FindAnyObjectByType<Camera>();
        // Procurar o BuildingManager é uma varredura à cena — não o faças todos os frames.
        if (buildingManager == null && Time.time >= nextManagerSearch)
        {
            nextManagerSearch = Time.time + 2f;
            buildingManager = FindAnyObjectByType<BuildingManager>();
        }

        if (buildingManager != null && buildingManager.IsBuildModeActive())
        {
            ClearHighlight();
            return;
        }

        if (!IsPickaxeEquipped())
        {
            ClearHighlight();
            return;
        }

        GameObject target = GetTarget();
        UpdateHighlight(target);

        if (Input.GetMouseButtonDown(0) && Time.time >= lastHitTime + hitCooldown)
        {
            if (debugLogs)
                Debug.Log($"[PickaxeTool] Click. Target={(target != null ? target.name : "NULL")}");

            if (target != null)
            {
                lastHitTime = Time.time;
                ClearHighlight();
                IHitable hitable = target.GetComponentInParent<IHitable>();
                if (debugLogs)
                    Debug.Log($"[PickaxeTool] IHitable={(hitable != null ? hitable.GetType().Name : "NULL")} em {target.name}, dano={damagePerHit}");
                hitable?.TakeDamage(damagePerHit);
            }
        }
    }

    public void SetPickaxeActive(bool active) { } // mantido por compatibilidade — activação gerida internamente

    private bool IsPickaxeEquipped()
    {
        if (InventorySystem.Instance == null) return false;
        var stack = InventorySystem.Instance.hotbar[InventoryUI.SelectedHotbarSlot];
        return stack != null && stack.itemName == "Picareta";
    }

    private GameObject GetTarget()
    {
        if (cam == null) return null;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit[] hits = Physics.RaycastAll(ray, range, raycastMask, QueryTriggerInteraction.Collide);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        if (debugLogs && Input.GetMouseButtonDown(0))
        {
            string msg = "[PickaxeTool] Hits (nearest->furthest):\n";
            foreach (var h in hits)
            {
                if (h.collider == null) continue;
                msg += $"- d={h.distance:F2} obj={h.collider.name} tag={h.collider.tag} root={h.collider.transform.root.name} rockBreaking={h.collider.GetComponentInParent<RockBreaking>() != null}\n";
            }
            Debug.Log(msg);
        }

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
