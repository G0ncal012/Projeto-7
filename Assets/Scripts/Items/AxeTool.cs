using UnityEngine;

public class AxeTool : MonoBehaviour
{
    [Header("Configurações")]
    [SerializeField] private float destroyRange = 5f;
    [SerializeField] private float hitCooldown = 0.35f;
    [SerializeField] private float damagePerHit = 34f;
    [SerializeField] private string buildablesTag = "Buildables";
    [SerializeField] private string treeTag = "Tree";
    [SerializeField] private string mobTag = "Mob";
    [SerializeField] private string animalTag = "Animal";
    [SerializeField] private LayerMask raycastMask = ~0;
    [SerializeField] private bool debugLogs = false;
    [SerializeField] private bool startActiveForTesting = false;
    [SerializeField] private KeyCode destroyKey = KeyCode.B;

    [Header("Highlight — opcional")]
    [SerializeField] private Color highlightColor = new Color(1f, 0.25f, 0.1f, 0.7f);

    private bool isActive = false;
    private float lastHitTime = -999f;
    private Camera cam;
    private float nextHeartbeatLogTime = 0f;
    private bool buildablesTagValid = true;
    private bool treeTagValid = false;
    private bool mobTagValid = false;
    private bool animalTagValid = false;
    private BuildingManager buildingManager;

    private GameObject lastHighlighted;
    private Material[] savedMaterials;
    private Renderer[] savedRenderers;

    private void Start()
    {
        cam = FindAnyObjectByType<Camera>();
        buildingManager = FindAnyObjectByType<BuildingManager>();

        buildablesTagValid = IsTagDefined(buildablesTag);
        treeTagValid = IsTagDefined(treeTag);
        mobTagValid = IsTagDefined(mobTag);
        animalTagValid = IsTagDefined(animalTag);
        if (debugLogs)
            Debug.Log($"[AxeTool] Tag valid? {buildablesTag}={buildablesTagValid}, {treeTag}={treeTagValid}");

        if (startActiveForTesting)
        {
            isActive = true;
            if (debugLogs) Debug.Log($"[AxeTool] startActiveForTesting -> Active={isActive} (on {name})");
        }
    }

    private void Update()
    {
        if (cam == null) cam = FindAnyObjectByType<Camera>();
        if (buildingManager == null) buildingManager = FindAnyObjectByType<BuildingManager>();

        if (startActiveForTesting && !isActive)
        {
            isActive = true;
            Debug.Log($"[AxeTool] startActiveForTesting keeps Active=True (on {name})");
        }

        if (!isActive)
        {
            ClearHighlight();
            return;
        }

        if (buildingManager != null && buildingManager.IsBuildModeActive())
        {
            ClearHighlight();
            return;
        }

        if ((debugLogs || startActiveForTesting) && Time.time >= nextHeartbeatLogTime)
        {
            nextHeartbeatLogTime = Time.time + 1f;
            Debug.Log($"[AxeTool] Heartbeat. focused={Application.isFocused} cam={(cam != null ? cam.name : "NULL")}");
        }

        GameObject target = GetTarget(false);
        UpdateHighlight(target);

        bool keyPressed = Input.GetKeyDown(destroyKey);
        bool mousePressed = Input.GetMouseButtonDown(0);
        bool destroyPressed = mousePressed || keyPressed;

        if (destroyPressed && Time.time >= lastHitTime + hitCooldown)
        {
            if (debugLogs)
            {
                Debug.Log($"[AxeTool] Destroy input. keyDown({destroyKey})={keyPressed} mouse0Down={mousePressed} anyKeyDown={Input.anyKeyDown} focused={Application.isFocused}");
                Debug.Log($"[AxeTool] cam={(cam != null ? cam.name : "NULL")} range={destroyRange} mask={raycastMask.value}");
            }

            target = GetTarget(debugLogs);

            if (debugLogs)
                Debug.Log($"[AxeTool] Target={(target != null ? target.name : "NULL")}");

            TryHit(target);
        }
    }

    public void SetAxeActive(bool active)
    {
        isActive = active;
        if (debugLogs) Debug.Log($"[AxeTool] Active={isActive} (on {name})");
        if (!active) ClearHighlight();
    }

    private void TryHit(GameObject target)
    {
        if (target == null) return;

        lastHitTime = Time.time;
        ClearHighlight();

        // Tenta IHitable primeiro (árvores com TreeChopping)
        IHitable hitable = target.GetComponentInChildren<IHitable>();
        if (hitable == null) hitable = target.GetComponentInParent<IHitable>();

        if (hitable != null)
        {
            if (debugLogs) Debug.Log($"[AxeTool] IHitable encontrado em: {target.name}");
            hitable.TakeDamage(damagePerHit);
            return;
        }

        // Fallback: comportamento original para Buildables
        foreach (Connector c in target.GetComponentsInChildren<Connector>())
            c.UpdateConnectors(false);

        if (debugLogs) Debug.Log($"[AxeTool] Destruído (Buildable): {target.name}");
        Destroy(target);
    }

    private GameObject GetTarget(bool log)
    {
        if (cam == null)
        {
            if (log) Debug.Log("[AxeTool] GetTarget: cam is NULL.");
            return null;
        }

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        RaycastHit[] hits = Physics.RaycastAll(
            ray,
            destroyRange,
            raycastMask,
            QueryTriggerInteraction.Collide
        );

        if (hits == null || hits.Length == 0)
        {
            if (log) Debug.Log("[AxeTool] Raycast: no hits.");
            return null;
        }

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        if (log)
        {
            string msg = "[AxeTool] Hits (nearest->furthest):\n";
            for (int i = 0; i < hits.Length; i++)
            {
                var col = hits[i].collider;
                if (col == null) continue;
                msg += $"- d={hits[i].distance:F2} obj={col.name} tag={col.tag} layer={col.gameObject.layer} trig={col.isTrigger}\n";
            }
            Debug.Log(msg);
        }

        for (int i = 0; i < hits.Length; i++)
        {
            Transform t = hits[i].collider != null ? hits[i].collider.transform : null;
            if (t == null) continue;

            GameObject candidate = null;
            if (buildablesTagValid) candidate = FindTaggedInParents(t, buildablesTag);
            if (candidate == null && treeTagValid) candidate = FindTaggedInParents(t, treeTag);
            if (candidate == null && mobTagValid) candidate = FindTaggedInParents(t, mobTag);
            if (candidate == null && animalTagValid) candidate = FindTaggedInParents(t, animalTag);

            // Fallback: mobs/animais com IHitable — exclui RockBreaking (só picareta parte pedra)
            if (candidate == null)
            {
                IHitable hitable = hits[i].collider.GetComponentInParent<IHitable>();
                if (hitable == null) hitable = hits[i].collider.GetComponentInChildren<IHitable>();
                if (hitable != null && !(hitable is RockBreaking) && !(hitable is TreeChopping))
                    candidate = (hitable as MonoBehaviour)?.gameObject;
            }

            if (candidate != null) return candidate;
        }

        return null;
    }

    private static GameObject FindTaggedInParents(Transform start, string tag)
    {
        if (start == null || string.IsNullOrEmpty(tag)) return null;

        Transform t = start;
        while (t != null)
        {
            if (SafeCompareTag(t, tag)) return t.gameObject;
            t = t.parent;
        }

        Transform root = start.root;
        if (root != null && SafeCompareTag(root, tag)) return root.gameObject;
        return null;
    }

    private static bool SafeCompareTag(Component c, string tag)
    {
        if (c == null || string.IsNullOrEmpty(tag)) return false;
        try { return c.CompareTag(tag); }
        catch { return false; }
    }

    private static bool IsTagDefined(string tag)
    {
        if (string.IsNullOrEmpty(tag)) return false;
        try { GameObject.FindGameObjectWithTag(tag); return true; }
        catch { return false; }
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
        {
            for (int i = 0; i < savedRenderers.Length; i++)
            {
                if (savedRenderers[i] != null && savedMaterials[i] != null)
                    savedRenderers[i].material = savedMaterials[i];
            }
        }

        lastHighlighted = null;
        savedMaterials = null;
        savedRenderers = null;
    }

    private void OnDrawGizmosSelected()
    {
        if (cam == null) return;
        Gizmos.color = new Color(1f, 0.3f, 0.1f, 0.35f);
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Gizmos.DrawLine(ray.origin, ray.origin + ray.direction * destroyRange);
        Gizmos.DrawWireSphere(ray.origin + ray.direction * destroyRange, 0.15f);
    }
}
