using UnityEngine;

/// <summary>
/// Controla o ciclo de dia e noite.
/// Coloca este script num objeto vazio na cena e arrasta a luz direcional (sol) para sunLight.
/// 
/// timeOfDay: 0.0 = meia-noite, 0.25 = amanhecer, 0.5 = meio-dia, 0.75 = pôr-do-sol
/// </summary>
public class DayNightCycle : MonoBehaviour
{
    [Header("Referências")]
    [Tooltip("Arrasta aqui a Directional Light (sol) da cena")]
    [SerializeField] private Light sunLight;

    [Header("Tempo")]
    [Tooltip("Duração de um dia completo em segundos reais")]
    [SerializeField] private float dayDurationSeconds = 120f;

    [Tooltip("Hora inicial (0=meia-noite, 0.25=amanhecer, 0.5=meio-dia, 0.75=pôr-do-sol)")]
    [Range(0f, 1f)]
    [SerializeField] private float startTime = 0.35f;

    [Header("Limites da Noite")]
    [Tooltip("A noite começa a esta hora (ex: 0.75 = pôr-do-sol)")]
    [Range(0f, 1f)]
    [SerializeField] private float nightStartTime = 0.75f;

    [Tooltip("A noite termina a esta hora (ex: 0.25 = amanhecer)")]
    [Range(0f, 1f)]
    [SerializeField] private float nightEndTime = 0.25f;

    [Header("Cores Ambiente")]
    [SerializeField] private Color dayAmbientColor = new Color(0.8f, 0.8f, 1f);
    [SerializeField] private Color nightAmbientColor = new Color(0.05f, 0.05f, 0.15f);
    [SerializeField] private Color daySunColor = new Color(1f, 0.95f, 0.8f);
    [SerializeField] private Color nightSunColor = new Color(0.1f, 0.1f, 0.3f);

    [Header("Intensidade da Luz")]
    [SerializeField] private float dayLightIntensity = 1f;
    [SerializeField] private float nightLightIntensity = 0f;

    // ── Estado público ─────────────────────────────────────────────────────────
    /// <summary>Hora atual do dia (0-1). Acede a partir de outros scripts.</summary>
    public static float TimeOfDay { get; private set; }

    /// <summary>True quando é noite.</summary>
    public static bool IsNight { get; private set; }

    // Singleton simples para acesso fácil
    public static DayNightCycle Instance { get; private set; }

    void Awake()
    {
        Instance = this;
        TimeOfDay = startTime;
    }

    void Update()
    {
        // Avança o tempo
        TimeOfDay += Time.deltaTime / dayDurationSeconds;
        if (TimeOfDay >= 1f) TimeOfDay -= 1f;

        UpdateIsNight();
        UpdateSun();
        UpdateAmbientLight();
    }

    private void UpdateIsNight()
    {
        // Noite = entre nightStartTime e o final do dia OU entre 0 e nightEndTime
        if (nightStartTime > nightEndTime)
            IsNight = TimeOfDay >= nightStartTime || TimeOfDay < nightEndTime;
        else
            IsNight = TimeOfDay >= nightStartTime && TimeOfDay < nightEndTime;
    }

    private void UpdateSun()
    {
        if (sunLight == null) return;

        // Rotação: ao meio-dia (0.5) o sol está em cima (xRot=90), à meia-noite está em baixo (xRot=270)
        float xRot = (TimeOfDay * 360f) - 90f;
        sunLight.transform.rotation = Quaternion.Euler(xRot, 170f, 0f);

        // Interpola cor e intensidade conforme a hora
        float t = GetDayBlend();
        sunLight.color = Color.Lerp(nightSunColor, daySunColor, t);
        sunLight.intensity = Mathf.Lerp(nightLightIntensity, dayLightIntensity, t);
    }

    private void UpdateAmbientLight()
    {
        float t = GetDayBlend();
        RenderSettings.ambientLight = Color.Lerp(nightAmbientColor, dayAmbientColor, t);
    }

    /// <summary>Retorna 1 ao meio-dia e 0 a meia-noite, com transições suaves.</summary>
    private float GetDayBlend()
    {
        // Mapeia timeOfDay num valor 0-1: 0 = noite, 1 = pleno dia
        // Usa uma curva sinusoidal centrada no meio-dia (0.5)
        float angle = TimeOfDay * Mathf.PI * 2f; // 0 a 2π
        float sinValue = Mathf.Sin(angle - Mathf.PI * 0.5f); // -1 a 1, pico no meio-dia
        return Mathf.Clamp01((sinValue + 1f) * 0.5f);
    }

    // ── Gizmos ──────────────────────────────────────────────────────────────────
    void OnValidate()
    {
        TimeOfDay = startTime;
        UpdateIsNight();
        if (sunLight != null) UpdateSun();
    }
}
