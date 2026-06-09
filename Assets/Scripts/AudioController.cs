using UnityEngine;

/// <summary>
/// Toca a música de fundo em loop assim que o jogo arranca e guarda a preferência
/// de volume (PlayerPrefs) para os menus poderem ler/alterar.
/// Cria-se sozinho — não é preciso colocar nada na cena.
/// </summary>
public class AudioController : MonoBehaviour
{
    private const string VolumePrefKey = "MusicVolume";
    private const float DefaultVolume = 0.5f;

    public static AudioController Instance { get; private set; }

    private AudioSource source;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;

        GameObject root = new GameObject("AudioController");
        DontDestroyOnLoad(root);
        root.AddComponent<AudioController>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        source = gameObject.AddComponent<AudioSource>();
        source.clip = Resources.Load<AudioClip>("Audio/BackgroundMusic");
        source.loop = true;
        source.playOnAwake = false;
        source.spatialBlend = 0f; // música de fundo em 2D, não posicional
        source.volume = GetVolume();

        if (source.clip != null)
            source.Play();
        else
            Debug.LogWarning("[AudioController] Não encontrei 'Audio/BackgroundMusic' em Resources.");
    }

    /// <summary>Volume guardado (0–1), usado para inicializar sliders nos menus.</summary>
    public static float GetVolume() => PlayerPrefs.GetFloat(VolumePrefKey, DefaultVolume);

    /// <summary>Aplica e guarda um novo volume (0–1) para a música de fundo.</summary>
    public static void SetVolume(float value)
    {
        value = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(VolumePrefKey, value);

        if (Instance != null && Instance.source != null)
            Instance.source.volume = value;
    }
}
