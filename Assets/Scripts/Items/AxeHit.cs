using UnityEngine;

// Coloca este script no Player (ou num objeto filho "Machado")
// É este script que decide QUANDO a árvore é cortada — só ao clicar
public class AxeHit : MonoBehaviour
{
    [Tooltip("Distância máxima do corte")]
    public float hitRange = 3f;

    [Tooltip("Layer da árvore (opcional, para não acertar noutras coisas)")]
    public LayerMask hitLayers = ~0; // por defeito acerta em tudo

    private Camera playerCamera;

    void Start()
    {
        // Procura a câmara do jogador
        playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera == null)
            playerCamera = Camera.main;
    }

    void Update()
    {
        // Só corta ao clicar com o botão esquerdo do rato
        if (Input.GetMouseButtonDown(0))
        {
            TryHit();
        }
    }

    void TryHit()
    {
        if (playerCamera == null) return;

        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f));
        
        if (Physics.Raycast(ray, out RaycastHit hit, hitRange, hitLayers))
        {
            // Procura IHitable no objeto atingido ou nos seus pais
            IHitable hitable = hit.collider.GetComponentInParent<IHitable>();
            
            if (hitable != null)
            {
                hitable.Execute();
            }
        }
    }

    // Mostra o alcance do machado no editor
    void OnDrawGizmosSelected()
    {
        if (playerCamera == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * hitRange);
    }
}
