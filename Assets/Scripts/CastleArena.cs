using UnityEngine;
using UnityEngine.SceneManagement;

public class CastleArena : MonoBehaviour
{
    // Raio do trigger de entrada — player tem de estar a 10u do centro do castelo
    private const float EntryRadius = 10f;
    private const float WallHeight = 25f;
    private const int WallSegments = 32;

    private GameObject barrierVisual;
    private BoxCollider[] wallColliders;
    private SphereCollider entryTrigger;
    private bool arenaReady = false;
    private bool playerInArena = false;
    private float wallRadius = 25f; // calculado dinamicamente em Build()

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<CastleArena>() != null) return;
        GameObject go = new GameObject("CastleArena");
        DontDestroyOnLoad(go);
        go.AddComponent<CastleArena>();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        arenaReady = false;
        playerInArena = false;

        if (barrierVisual != null) Destroy(barrierVisual);
        barrierVisual = null;

        if (wallColliders != null)
        {
            foreach (var wc in wallColliders)
                if (wc != null) Destroy(wc.gameObject);
            wallColliders = null;
        }

        if (entryTrigger != null) Destroy(entryTrigger);
        entryTrigger = null;
    }

    private void Update()
    {
        if (arenaReady) return;

        GameObject castle = GameObject.Find("Castle");
        if (castle == null) return;

        Build(castle);
        arenaReady = true;
    }

    private void Build(GameObject castle)
    {
        // Mede os bounds reais do modelo do castelo
        Renderer[] renderers = castle.GetComponentsInChildren<Renderer>();
        Vector3 center = castle.transform.position;

        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            foreach (var r in renderers)
                bounds.Encapsulate(r.bounds);

            // Centrar no centro geométrico horizontal do modelo (não no pivot)
            center = new Vector3(bounds.center.x, castle.transform.position.y, bounds.center.z);

            // Raio = maior semi-extensão horizontal + margem generosa
            wallRadius = Mathf.Max(bounds.extents.x, bounds.extents.z) + 3f;
            wallRadius = Mathf.Clamp(wallRadius, 12f, 60f);
        }

        transform.position = center;

        // Trigger de entrada — só ativa quando player está DENTRO das paredes
        entryTrigger = gameObject.AddComponent<SphereCollider>();
        entryTrigger.isTrigger = true;
        entryTrigger.radius = EntryRadius;

        // Visual — cilindro amarelo escuro transparente
        barrierVisual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        barrierVisual.name = "CastleBarrierVisual";
        Destroy(barrierVisual.GetComponent<Collider>());
        barrierVisual.transform.SetParent(transform, false);
        barrierVisual.transform.localPosition = new Vector3(0f, WallHeight * 0.5f - 1f, 0f);
        barrierVisual.transform.localScale = new Vector3(wallRadius * 2f, WallHeight * 0.5f, wallRadius * 2f);

        Renderer rend = barrierVisual.GetComponent<Renderer>();
        rend.material = CreateTransparentMaterial(new Color(0.55f, 0.45f, 0f, 0.5f));
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rend.receiveShadows = false;
        barrierVisual.SetActive(false);

        // Paredes físicas em anel — raio calculado dos bounds reais
        float segAngle = 360f / WallSegments;
        float wallLength = 2f * Mathf.PI * wallRadius / WallSegments * 1.2f;

        wallColliders = new BoxCollider[WallSegments];
        for (int i = 0; i < WallSegments; i++)
        {
            float angleDeg = i * segAngle;
            float angleRad = angleDeg * Mathf.Deg2Rad;

            GameObject wall = new GameObject($"BarrierWall_{i}");
            wall.transform.SetParent(transform, false);
            wall.transform.localPosition = new Vector3(
                Mathf.Sin(angleRad) * wallRadius,
                WallHeight * 0.5f,
                Mathf.Cos(angleRad) * wallRadius
            );
            wall.transform.localRotation = Quaternion.Euler(0f, angleDeg, 0f);

            BoxCollider bc = wall.AddComponent<BoxCollider>();
            bc.size = new Vector3(wallLength, WallHeight, 0.6f);
            bc.enabled = false;

            wallColliders[i] = bc;
        }

        Debug.Log($"[CastleArena] Barreira criada com raio={wallRadius:F1} (bounds do castelo)");
    }

    private static Material CreateTransparentMaterial(Color color)
    {
        string[] candidates = {
            "Sprites/Default",
            "Universal Render Pipeline/Particles/Unlit",
            "Particles/Standard Unlit",
            "Unlit/Transparent",
            "Standard",
        };

        Shader shader = null;
        foreach (var name in candidates)
        {
            shader = Shader.Find(name);
            if (shader != null) break;
        }

        if (shader == null)
        {
            Debug.LogWarning("[CastleArena] Nenhum shader transparente encontrado.");
            return new Material(Shader.Find("Unlit/Color")) { color = color };
        }

        Material mat = new Material(shader);
        mat.color = color;

        if (mat.HasProperty("_Cull"))
            mat.SetInt("_Cull", 0);

        if (shader.name == "Standard")
        {
            mat.SetFloat("_Mode", 3f);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.renderQueue = 3000;
        }

        return mat;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || playerInArena) return;
        playerInArena = true;

        if (barrierVisual != null)
            barrierVisual.SetActive(true);

        if (wallColliders != null)
            foreach (var bc in wallColliders)
                if (bc != null) bc.enabled = true;

        SpawnBoss();
        BossAI.OnBossDied += OnBossDefeated;

        Debug.Log("[CastleArena] Player entrou na arena — boss invocado.");
    }

    private void SpawnBoss()
    {
        if (BossAI.ActiveBoss != null) return;

        GameObject[] prefabs = Resources.LoadAll<GameObject>("Mobs");
        if (prefabs == null || prefabs.Length == 0)
        {
            Debug.LogWarning("[CastleArena] Nenhum prefab em Resources/Mobs/ para o boss.");
            return;
        }

        // Spawna no centro da arena, ligeiramente acima do chão
        Vector3 spawnPos = transform.position;
        if (Physics.Raycast(transform.position + Vector3.up * 20f, Vector3.down, out RaycastHit hit, 40f))
            spawnPos.y = hit.point.y + 0.1f;
        else
            spawnPos.y = transform.position.y + 0.1f;

        GameObject bossObj = Instantiate(prefabs[0], spawnPos,
            Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
        bossObj.name = "SkeletonKing";
        bossObj.transform.localScale *= 2f;

        // Escalar 2x duplica os collideres em world-space — o player fica dentro deles
        // e Physics.RaycastAll não deteta collideres quando o raio começa dentro deles.
        // Corrigir: dividir as dimensões locais por 2 para manter o tamanho world-space original.
        foreach (CapsuleCollider cc in bossObj.GetComponentsInChildren<CapsuleCollider>())
        {
            cc.height /= 2f;
            cc.radius /= 2f;
            cc.center /= 2f;
        }
        foreach (SphereCollider sc in bossObj.GetComponentsInChildren<SphereCollider>())
        {
            sc.radius /= 2f;
            sc.center /= 2f;
        }
        foreach (BoxCollider bc in bossObj.GetComponentsInChildren<BoxCollider>())
        {
            bc.size /= 2f;
            bc.center /= 2f;
        }
        // Garante que existe pelo menos um collider
        if (bossObj.GetComponentInChildren<Collider>() == null)
        {
            CapsuleCollider col = bossObj.AddComponent<CapsuleCollider>();
            col.height = 1f;
            col.radius = 0.25f;
            col.center = new Vector3(0f, 0.5f, 0f);
        }

        // Remove MobAI e adiciona BossAI
        MobAI mob = bossObj.GetComponent<MobAI>();
        if (mob != null) Destroy(mob);

        bossObj.AddComponent<BossAI>();
    }

    /// <summary>Remove a barreira, destrói o boss ativo e repõe a arena. Chamado pelo PlayerRespawn ao renascer.</summary>
    public void ResetArena()
    {
        BossAI.OnBossDied -= OnBossDefeated;

        if (BossAI.ActiveBoss != null)
            Destroy(BossAI.ActiveBoss.gameObject);

        if (barrierVisual != null) barrierVisual.SetActive(false);

        if (wallColliders != null)
            foreach (var bc in wallColliders)
                if (bc != null) bc.enabled = false;

        playerInArena = false;
        Debug.Log("[CastleArena] Arena reposta após renascimento.");
    }

    private void OnBossDefeated()
    {
        BossAI.OnBossDied -= OnBossDefeated;

        // Remove barreiras para o player sair
        if (barrierVisual != null) barrierVisual.SetActive(false);
        if (wallColliders != null)
            foreach (var bc in wallColliders)
                if (bc != null) bc.enabled = false;

        playerInArena = false;
        Debug.Log("[CastleArena] Boss derrotado — barreiras removidas.");
    }
}
