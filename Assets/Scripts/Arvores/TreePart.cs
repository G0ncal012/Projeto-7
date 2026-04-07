using UnityEngine;

// Coloca este script no Tronco e nas Folhas
// NÃO adds Rigidbody manualmente — este script trata disso
public class TreePart : MonoBehaviour
{
    [Tooltip("Velocidade a que a árvore tomba")]
    public float fallTorque = 1.5f;

    private Rigidbody rb;

    void Awake()
    {
        rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true; // não cai ao iniciar
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    // Chamado pelo TreeChopping — recebe a direção do corte
    public void Fall(Vector3 fallDirection)
    {
        rb.isKinematic = false;

        // Tomba na direção do corte, rodando no eixo lateral
        Vector3 torqueAxis = Vector3.Cross(Vector3.up, fallDirection).normalized;
        rb.AddTorque(torqueAxis * fallTorque, ForceMode.Impulse);
    }
}
