using UnityEngine;

// Liga os sons aos eventos do jogo (sem alterar os scripts do EnemyMovement e do EnemySpawner)
public class EventSounds : MonoBehaviour
{
    void OnEnable()
    {
        EnemyMovement.OnReachedBase += OnEnemyReachedBase;
        EnemySpawner.OnWaveStarted += OnWaveStarted;
        EnemySpawner.OnAllWavesCleared += OnAllWavesCleared;
    }

    void OnDisable()
    {
        EnemyMovement.OnReachedBase -= OnEnemyReachedBase;
        EnemySpawner.OnWaveStarted -= OnWaveStarted;
        EnemySpawner.OnAllWavesCleared -= OnAllWavesCleared;
    }

    // o inimigo é destruído logo depois do evento, então o som é tocado na posição dele agora
    void OnEnemyReachedBase(EnemyMovement e)
    {
        if (e != null) SoundManager.PlayAt(Sfx.EnemyReachBase, e.transform.position);
    }

    void OnWaveStarted(int wave) { SoundManager.Play(Sfx.WaveStart); }

    void OnAllWavesCleared()
    {
        SoundManager.Play(Sfx.Victory);
        // opcional: SoundManager.StopMusic();   // para a música de fundo com fade na vitória
    }
}