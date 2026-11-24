using UnityEngine;

// Som de ambiente em loop (vento, pássaros, vilarejo...). Um por cena: cada mapa pode ter o seu.
public class AmbientSound : MonoBehaviour
{
    public enum Channel { Effects, Music }

    [Header("Som de ambiente")]
    public AudioClip clip;                           // arraste o som aqui (precisa ser bom em loop)
    [Range(0f, 1f)] public float volume = 0.5f;
    public Channel followSlider = Channel.Effects;   // qual slider do menu de configurações controla este som
    public float fadeTime = 2f;                      // segundos de fade in / fade out

    [Header("Quando parar (com fade)")]
    public bool stopOnVictory = true;
    public bool stopOnPlayerDeath = false;

    AudioSource src;
    float fade;                                      // 0 = mudo, 1 = volume cheio
    bool stopping;

    void Awake()
    {
        src = gameObject.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = true;
        src.spatialBlend = 0f;                       // som 2D
        src.volume = 0f;
    }

    void Start()
    {
        if (clip == null) return;
        src.clip = clip;
        src.Play();
    }

    void OnEnable()
    {
        EnemySpawner.OnAllWavesCleared += OnVictory;
        Health.OnPlayerDied += OnPlayerDied;
    }

    void OnDisable()
    {
        EnemySpawner.OnAllWavesCleared -= OnVictory;
        Health.OnPlayerDied -= OnPlayerDied;
    }

    void OnVictory() { if (stopOnVictory) stopping = true; }
    void OnPlayerDied() { if (stopOnPlayerDeath) stopping = true; }

    void Update()
    {
        if (clip == null) return;

        // fade in no começo, fade out ao parar (tempo não escalado: funciona com o jogo pausado)
        float step = (fadeTime > 0f ? 1f / fadeTime : 100f) * Time.unscaledDeltaTime;
        fade = Mathf.MoveTowards(fade, stopping ? 0f : 1f, step);

        src.volume = volume * Master() * fade;

        if (stopping && fade <= 0.001f && src.isPlaying) src.Stop();
    }

    // acompanha o slider escolhido (Música ou Efeitos) do SoundManager
    float Master()
    {
        var sm = SoundManager.Instance;
        if (sm == null) return 1f;
        return followSlider == Channel.Music ? sm.musicVolume : sm.sfxVolume;
    }
}