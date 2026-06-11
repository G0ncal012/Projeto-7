using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Anexa modelos de ferramentas/armas (ex: machado, picareta) ao osso da mão direita do player
/// e mostra/esconde cada um consoante o item correspondente está equipado na hotbar.
/// </summary>
public class WeaponHold : MonoBehaviour
{
    [System.Serializable]
    public class ToolVisual
    {
        [Tooltip("Nome do item na hotbar que ativa este modelo (ex: Machado, Picareta).")]
        public string itemName = "Machado";

        [Tooltip("Prefab do modelo 3D da ferramenta (importado do Blender/FBX).")]
        public GameObject modelPrefab;

        [Header("Ajuste na mão")]
        [Tooltip("Dimensão-alvo (em metros) do lado MAIOR do item, já na mão. O item é " +
                 "auto-redimensionado para este tamanho, seja qual for a escala do osso/mesh. " +
                 "~0.5 = comprimento de uma ferramenta segurada à frente da câmara.")]
        public float targetWorldSize = 0.5f;

        [Tooltip("Deslocamento da posição, em METROS no espaço do mundo, a partir da mão.")]
        public Vector3 positionOffset;
        public Vector3 rotationOffset;

        [Tooltip("Multiplicador fino opcional por cima do targetWorldSize (1 = sem efeito).")]
        public Vector3 scale = Vector3.one;

        [System.NonSerialized] public GameObject instance;
        [System.NonSerialized] public Vector3 normalizedScale = Vector3.one;
        [System.NonSerialized] public float baseSize = 1f; // maior eixo do modelo medido com localScale=1
    }

    [SerializeField] private List<ToolVisual> tools = new List<ToolVisual>
    {
        new ToolVisual { itemName = "Machado", rotationOffset = new Vector3(0f, 180f, 0f) },
        new ToolVisual { itemName = "Picareta", rotationOffset = new Vector3(180f, 180f, 0f) },
    };

    private Transform rightHand;

    private void Start()
    {
        Animator animator = GetComponentInChildren<Animator>();
        if (animator == null)
        {
            Debug.LogWarning("[WeaponHold] Nenhum Animator encontrado no player.");
            return;
        }

        rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
        if (rightHand == null)
        {
            Debug.LogWarning("[WeaponHold] Osso RightHand não encontrado no Avatar.");
            return;
        }

        foreach (ToolVisual tool in tools)
        {
            if (tool.modelPrefab == null)
            {
                Debug.LogWarning($"[WeaponHold] modelPrefab não definido para '{tool.itemName}'.");
                continue;
            }

            tool.instance = Instantiate(tool.modelPrefab, rightHand);

            // Modelos importados de FBX/Blender podem trazer Câmaras/Luzes da cena de origem,
            // que ficariam ativas ao mostrar o modelo e estragariam o ecrã (ex: ecrã preto).
            foreach (Camera cam in tool.instance.GetComponentsInChildren<Camera>(true))
                cam.enabled = false;
            foreach (Light light in tool.instance.GetComponentsInChildren<Light>(true))
                light.enabled = false;
            foreach (AudioListener listener in tool.instance.GetComponentsInChildren<AudioListener>(true))
                listener.enabled = false;

            // Mede UMA vez o tamanho real do modelo com localScale=1 (em escala mundial, o que
            // já inclui a lossyScale do osso e o tamanho nativo do mesh). Guarda esse tamanho
            // para depois calcular a escala-alvo sem voltar a medir.
            tool.instance.transform.localScale = Vector3.one;
            tool.baseSize = MeasureLargestAxis(tool.instance);
            tool.normalizedScale = ComputeNormalizedScale(tool);

            ApplyTransform(tool);
            tool.instance.SetActive(false);
        }
    }

    /// <summary>Maior dimensão (mundo) do conjunto de renderers do objeto na escala atual.</summary>
    private static float MeasureLargestAxis(GameObject go)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return 1f;

        Bounds b = renderers[0].bounds;
        foreach (Renderer r in renderers) b.Encapsulate(r.bounds);

        return Mathf.Max(b.size.x, b.size.y, b.size.z);
    }

    /// <summary>Escala local que faz o maior eixo do modelo medir 'targetWorldSize' metros.</summary>
    private Vector3 ComputeNormalizedScale(ToolVisual tool)
    {
        if (tool.baseSize <= 0.0001f) return tool.scale;

        // baseSize foi medido com localScale=1, por isso este fator cancela tanto a
        // lossyScale do osso como o tamanho nativo do mesh.
        float factor = tool.targetWorldSize / tool.baseSize;
        return new Vector3(factor * tool.scale.x, factor * tool.scale.y, factor * tool.scale.z);
    }

    /// <summary>Aplica escala/posição/rotação. Posição em metros, compensando a escala do osso.</summary>
    private void ApplyTransform(ToolVisual tool)
    {
        if (tool.instance == null || rightHand == null) return;

        tool.instance.transform.localScale = tool.normalizedScale;
        tool.instance.transform.localRotation = Quaternion.Euler(tool.rotationOffset);

        Vector3 ls = rightHand.lossyScale;
        tool.instance.transform.localPosition = new Vector3(
            Mathf.Abs(ls.x) > 0.0001f ? tool.positionOffset.x / ls.x : tool.positionOffset.x,
            Mathf.Abs(ls.y) > 0.0001f ? tool.positionOffset.y / ls.y : tool.positionOffset.y,
            Mathf.Abs(ls.z) > 0.0001f ? tool.positionOffset.z / ls.z : tool.positionOffset.z);
    }

    private void Update()
    {
        if (InventorySystem.Instance == null) return;

        var stack = InventorySystem.Instance.hotbar[InventoryUI.SelectedHotbarSlot];
        string equippedItem = stack != null ? stack.itemName : null;

        foreach (ToolVisual tool in tools)
        {
            if (tool.instance == null) continue;

            bool visible = equippedItem == tool.itemName;
            if (tool.instance.activeSelf != visible)
                tool.instance.SetActive(visible);

            // Re-aplica enquanto visível para permitir afinação ao vivo no Inspector durante o Play.
            if (visible)
            {
                tool.normalizedScale = ComputeNormalizedScale(tool);
                ApplyTransform(tool);
            }
        }
    }
}
