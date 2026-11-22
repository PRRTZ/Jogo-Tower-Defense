using System;
using UnityEngine;

// Todos os sons do jogo. A ORDEM do enum define a ordem da lista no Inspector.
public enum Sfx
{
    PlayerAttack, PlayerHit, PlayerDeath, Heal,
    Footstep,
    PlaceTower, UpgradeTower, RemoveTower,
    EnemyHit, EnemyDeath, EnemyReachBase,
    Victory, Defeat, WaveStart, ButtonClick
}

[Serializable]
public class Sound
{
    public string name;                          // preenchido automaticamente (só leitura)
    public AudioClip[] clips;                    // vários clipes = sorteia um (menos repetitivo)
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0f, 0.3f)] public float pitchVariation = 0.05f;
}

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("Volumes")]
    [Range(0f, 1f)] public float sfxVolume = 1f;
    [Range(0f, 1f)] public float musicVolume = 0.4f;

    [Header("Música de fundo")]
    public AudioClip music;                      // arraste a música da fase aqui
    public bool playMusicOnStart = true;         // começa sozinha ao iniciar a cena
    public float musicFadeTime = 1.5f;           // segundos de fade in/out (0 = sem fade)

    [Header("Efeitos (um item por Sfx)")]
    public Sound[] sounds;

    [Header("Som 3D (inimigos/torres)")]
    public float minDistance = 6f;
    public float maxDistance = 45f;

    AudioSource[] pool;
    AudioSource musicSource;
    int next;
    bool musicStopping;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        pool = new AudioSource[16];
        for (int i = 0; i < pool.Length; i++)
        {
            var go = new GameObject("SfxSource_" + i);
            go.transform.SetParent(transform);
            var s = go.AddComponent<AudioSource>();
            s.playOnAwake = false;
            pool[i] = s;
        }

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.spatialBlend = 0f;
        musicSource.volume = 0f;                  // sobe com fade no Update
    }

    void Start()
    {
        if (playMusicOnStart && music != null) StartMusic(music);
    }

    // Mantém a lista do tamanho do enum, reaproveitando os itens PELO NOME
    // (assim, remover/adicionar sons no enum não embaralha os clipes já configurados)
    void OnValidate()
    {
        var values = (Sfx[])Enum.GetValues(typeof(Sfx));
        var novo = new Sound[values.Length];
        for (int i = 0; i < values.Length; i++)
        {
            string n = values[i].ToString();
            Sound found = null;
            if (sounds != null)
                foreach (var s in sounds)
                    if (s != null && s.name == n) { found = s; break; }
            novo[i] = found ?? new Sound();
            novo[i].name = n;
        }
        sounds = novo;
    }

    void Update()
    {
        if (musicSource == null) return;

        // fade in/out da música (usa tempo não escalado: funciona mesmo com o jogo pausado)
        float target = musicStopping ? 0f : musicVolume;
        float step = (musicFadeTime > 0f ? musicVolume / musicFadeTime : 1f) * Time.unscaledDeltaTime;
        if (step <= 0f) step = 1f;
        musicSource.volume = Mathf.MoveTowards(musicSource.volume, target, step);

        if (musicStopping && musicSource.volume <= 0.001f && musicSource.isPlaying)
            musicSource.Stop();
    }

    // ---------- Efeitos ----------
    public static void Play(Sfx s) { if (Instance != null) Instance.Emit(s, null); }
    public static void PlayAt(Sfx s, Vector3 pos) { if (Instance != null) Instance.Emit(s, pos); }

    void Emit(Sfx s, Vector3? pos)
    {
        int idx = (int)s;
        if (sounds == null || idx >= sounds.Length) return;
        Sound snd = sounds[idx];
        if (snd == null || snd.clips == null || snd.clips.Length == 0) return;

        AudioClip clip = snd.clips[UnityEngine.Random.Range(0, snd.clips.Length)];
        if (clip == null) return;

        AudioSource src = pool[next];
        next = (next + 1) % pool.Length;

        src.clip = clip;
        src.volume = snd.volume * sfxVolume;
        src.pitch = 1f + UnityEngine.Random.Range(-snd.pitchVariation, snd.pitchVariation);

        if (pos.HasValue)
        {
            src.transform.position = pos.Value;
            src.spatialBlend = 1f;                       // 3D
            src.rolloffMode = AudioRolloffMode.Linear;
            src.minDistance = minDistance;
            src.maxDistance = maxDistance;
        }
        else
        {
            src.spatialBlend = 0f;                       // 2D (jogador/UI)
        }
        src.Play();
    }

    // ---------- Música ----------
    void StartMusic(AudioClip clip)
    {
        if (musicSource == null || clip == null) return;
        musicStopping = false;
        musicSource.clip = clip;
        musicSource.volume = musicFadeTime > 0f ? 0f : musicVolume;
        musicSource.Play();
    }

    // Troca/começa uma música (ex.: PlayMusic(clipDoMenu))
    public static void PlayMusic(AudioClip clip) { if (Instance != null) Instance.StartMusic(clip); }

    // Para a música com fade (ex.: chame na derrota ou na vitória)
    public static void StopMusic() { if (Instance != null) Instance.musicStopping = true; }

    // Liga/desliga a música (ex.: botão de mute no menu)
    public static void SetMusicVolume(float v) { if (Instance != null) Instance.musicVolume = Mathf.Clamp01(v); }
    public static void SetSfxVolume(float v) { if (Instance != null) Instance.sfxVolume = Mathf.Clamp01(v); }
}