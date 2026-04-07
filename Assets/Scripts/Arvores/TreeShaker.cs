using UnityEngine;

// Adicionado automaticamente pelo TreeChopping — não precisas de colocar manualmente
public class TreeShaker : MonoBehaviour
{
    private Vector3 originalPos;
    private float shakeTime = 0f;
    private float shakeDuration = 0.25f;
    private float shakeMagnitude = 0.05f;
    private bool shaking = false;

    public void Shake()
    {
        if (!shaking)
            originalPos = transform.localPosition;
        shakeTime = shakeDuration;
        shaking = true;
    }

    void Update()
    {
        if (!shaking) return;

        shakeTime -= Time.deltaTime;
        if (shakeTime <= 0f)
        {
            transform.localPosition = originalPos;
            shaking = false;
            return;
        }

        transform.localPosition = originalPos + Random.insideUnitSphere * shakeMagnitude;
    }
}
