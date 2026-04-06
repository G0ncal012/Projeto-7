using UnityEngine;

/// <summary>
/// Adiciona este script ao Player.
/// Ativado/desativado pela HotbarUI com a tecla E.
/// Com o machado ativo:
///   - Aponta para uma parede/chão/árvore e clica botão esquerdo para destruir.
///   - O raycast sai do centro da câmera (funciona com cursor bloqueado).
///   - Objetos destruíveis precisam da tag "Buildables" (ou "Tree" para árvores — futuro).
/// </summary>
public class AxeTool : MonoBehaviour
{
    [Header("Configurações")]
    [SerializeField] private float destroyRange = 5f;
    [SerializeField] private float hitCooldown = 0.35f;
    [SerializeField] private string buildablesTag = "Buildables";
    [SerializeField] private string treeTag = "Tree"; // para o futuro
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
    private BuildingManager buildingManager;

    // Highlight state
    private GameObject lastHighlighted;
    private Material[] savedMaterials;
    private Renderer[] savedRenderers;

    // ─────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        cam = FindFirstObjectByType<Camera>();
        buildingManager = FindFirstObjectByType<BuildingManager>();

        // Valida tags para não gerar UnityException quando não existem no TagManager
        buildablesTagValid = IsTagDefined(buildablesTag);
        treeTagValid = IsTagDefined(treeTag);
        if (debugLogs)
        {
            Debug.Log($"[AxeTool] Tag valid? {buildablesTag}={buildablesTagValid}, {treeTag}={treeTagValid}");
        }

        if (startActiveForTesting)
        {
            isActive = true;
            if (debugLogs) Debug.Log($"[AxeTool] startActiveForTesting -> Active={isActive} (on {name})");
        }
    }

    private void Update()
    {
        if (cam == null) cam = FindFirstObjectByType<Camera>();
        if (buildingManager == null) buildingManager = FindFirstObjectByType<BuildingManager>();

        // Se estás a usar o modo de teste, garante que fica ativo mesmo que alguma UI o desligue.
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

        // Segurança: se estiveres em modo de construção, não destruímos nada mesmo que o machado esteja ativo por engano.
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

            // Recalcula com logs no clique
            target = GetTarget(debugLogs);

            if (debugLogs)
            {
                Debug.Log($"[AxeTool] Target={(target != null ? target.name : "NULL")}");
            }

            TryDestroy(target);
        }
    }

    // Chamado pela HotbarUI
    public void SetAxeActive(bool active)
    {
        isActive = active;
        if (debugLogs) Debug.Log($"[AxeTool] Active={isActive} (on {name})");
        if (!active) ClearHighlight();
    }

    // ── Alvo ─────────────────────────────────────────────────────────────────

    private GameObject GetTarget(bool log)
    {
        if (cam == null)
        {
            if (log) Debug.Log("[AxeTool] GetTarget: cam is NULL.");
            return null;
        }

        // Raycast do CENTRO do ecrã — funciona mesmo com cursor bloqueado
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        // RaycastAll para evitar "bater" primeiro em triggers/Connectors/etc.
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
        try
        {
            return c.CompareTag(tag);
        }
        catch (System.Exception)
        {
            // Tag não existe no projeto (ex: "Tree")
            return false;
        }
    }

    private static bool IsTagDefined(string tag)
    {
        if (string.IsNullOrEmpty(tag)) return false;
        try
        {
            // Esta chamada lança exceção se a tag não existir.
            GameObject.FindGameObjectWithTag(tag);
            return true;
        }
        catch
        {
            return false;
        }
    }

    // ── Destruir ─────────────────────────────────────────────────────────────

    private void TryDestroy(GameObject target)
    {
        if (target == null) return;

        lastHitTime = Time.time;

        // Atualiza conectores antes de destruir (para o sistema de building)
        foreach (Connector c in target.GetComponentsInChildren<Connector>())
            c.UpdateConnectors(false);

        Debug.Log($"[AxeTool] Destruído: {target.name}");
        ClearHighlight();
        Destroy(target);
    }

    // ── Highlight ────────────────────────────────────────────────────────────

    private void UpdateHighlight(GameObject target)
    {
        if (target == lastHighlighted) return;

        ClearHighlight();
        if (target == null) return;

        lastHighlighted = target;

        // Guarda os materiais originais e aplica cor de highlight
        savedRenderers = target.GetComponentsInChildren<Renderer>();
        savedMaterials = new Material[savedRenderers.Length];

        for (int i = 0; i < savedRenderers.Length; i++)
        {
            savedMaterials[i] = savedRenderers[i].material;

            // Cria cópia do material com cor alterada
            Material m = new Material(savedRenderers[i].material);
            m.color = Color.Lerp(m.color, highlightColor, 0.55f);
            savedRenderers[i].material = m;
        }
    }

    private void ClearHighlight()
    {
        if (lastHighlighted == null) return;

        // Restaura materiais originais se o objeto ainda existir
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

    // ── Gizmo no editor ──────────────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        if (cam == null) return;
        Gizmos.color = new Color(1f, 0.3f, 0.1f, 0.35f);
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Gizmos.DrawLine(ray.origin, ray.origin + ray.direction * destroyRange);
        Gizmos.DrawWireSphere(ray.origin + ray.direction * destroyRange, 0.15f);
    }
}