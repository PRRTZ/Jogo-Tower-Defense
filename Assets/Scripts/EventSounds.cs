using UnityEngine;

// Liga os sons aos eventos do jogo (sem alterar EnemyMovement e EnemySpawner)
public class EventSounds : MonoBehaviour
{
    [Header("Ondas (arraste um som para cada)")]
    public AudioClip waveStartClip;                 // toca quando a onda COMEÇA
    public AudioClip waveClearedClip;               // toca quando a onda TERMINA (todos os inimigos eliminados)
    [Range(0f, 1f)] public float waveVolume = 1f;
    public bool skipClearedOnLastWave = true;       // na última onda toca só a vitória, não os dois juntos

    AudioSource src;

    void Awake()
    {
        src = gameObject.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = false;
        src.spatialBlend = 0f;                      // som 2D
    }

    void OnEnable()
    {
        EnemyMovement.OnReachedBase += OnEnemyReachedBase;
        EnemySpawner.OnWaveStarted += OnWaveStarted;
        EnemySpawner.OnWaveCleared += OnWaveCleared;
        EnemySpawner.OnAllWavesCleared += OnAllWavesCleared;
    }

    void OnDisable()
    {
        EnemyMovement.OnReachedBase -= OnEnemyReachedBase;
        EnemySpawner.OnWaveStarted -= OnWaveStarted;
        EnemySpawner.OnWaveCleared -= OnWaveCleared;
        EnemySpawner.OnAllWavesCleared -= OnAllWavesCleared;
    }

    // o inimigo é destruído logo depois do evento, então o som é tocado na posição dele agora
    void OnEnemyReachedBase(EnemyMovement e)
    {
        if (e != null) SoundManager.PlayAt(Sfx.EnemyReachBase, e.transform.position);
    }

    void OnWaveStarted(int wave)
    {
        if (waveStartClip != null) PlayClip(waveStartClip);
        else SoundManager.Play(Sfx.WaveStart);      // sem clipe aqui: usa o som WaveStart do SoundManager
    }

    void OnWaveCleared(int wave, int bonus)
    {
        if (waveClearedClip == null) return;

        // na última onda o jogo também dispara a vitória: evita tocar dois sons ao mesmo tempo
        if (skipClearedOnLastWave && EnemySpawner.Instance != null && !EnemySpawner.Instance.HasMoreWaves) return;

        PlayClip(waveClearedClip);
    }

    void OnAllWavesCleared()
    {
        SoundManager.Play(Sfx.Victory);
        // opcional: SoundManager.StopMusic();   // para a música de fundo com fade na vitória
    }

    void PlayClip(AudioClip clip)
    {
        float sfx = SoundManager.Instance != null ? SoundManager.Instance.sfxVolume : 1f;   // respeita o slider de efeitos
        src.PlayOneShot(clip, waveVolume * sfx);
    }
}