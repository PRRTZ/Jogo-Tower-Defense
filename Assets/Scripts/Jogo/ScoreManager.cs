using System;
using UnityEngine;

/// <summary>
/// Pontuação da partida (vai para o Ranking do menu no fim, na vitória ou no game over).
///   + pontos por monstro derrotado (valem mais nas ondas avançadas)
///   + bônus a cada onda vencida
///   + bônus grande ao derrotar o chefe
///   + na vitória: bônus de vitória e pontos por coração que sobrou
/// Tudo é multiplicado pelo "scoreMultiplier" da dificuldade (Difícil vale mais).
/// É criado sozinho pelo GameOverManager: não precisa colocar na cena.
/// </summary>
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }
    public static event Action<int> OnScoreChanged;

    [Header("Pontos")]
    public int pointsPerKill = 10;          // x número da onda
    public int pointsPerWave = 100;         // x número da onda
    public int pointsForBoss = 2000;
    public int pointsForVictory = 1000;
    public int pointsPerHeartLeft = 50;     // por coração (20 de vida) que sobrou na vitória

    public int Score { get; private set; }
    public int Kills { get; private set; }

    bool finished;

    void Awake()
    {
        Instance = this;
    }

    void OnEnable()
    {
        EnemyHealth.OnEnemyDied += HandleKill;
        EnemySpawner.OnWaveCleared += HandleWaveCleared;
        EnemySpawner.OnBossDefeated += HandleBossDefeated;
    }

    void OnDisable()
    {
        EnemyHealth.OnEnemyDied -= HandleKill;
        EnemySpawner.OnWaveCleared -= HandleWaveCleared;
        EnemySpawner.OnBossDefeated -= HandleBossDefeated;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    int CurrentWave => EnemySpawner.Instance != null ? Mathf.Max(1, EnemySpawner.Instance.CurrentWave) : 1;

    void HandleKill(EnemyHealth e)
    {
        Kills++;
        Add(pointsPerKill * CurrentWave);
    }

    void HandleWaveCleared(int wave, int bonus) => Add(pointsPerWave * wave);

    void HandleBossDefeated() => Add(pointsForBoss);

    void Add(int raw)
    {
        if (finished || raw <= 0) return;
        Score += Mathf.RoundToInt(raw * GameSession.Current.scoreMultiplier);
        OnScoreChanged?.Invoke(Score);
    }

    /// <summary>Fecha a pontuação no fim da partida e devolve o total.</summary>
    public int FinishMatch(bool victory)
    {
        if (!finished && victory)
        {
            Add(pointsForVictory);
            var pc = FindAnyObjectByType<PlayerController>();
            var health = pc != null ? pc.GetComponent<Health>() : null;
            if (health != null) Add(Mathf.FloorToInt(health.CurrentHealth / 20f) * pointsPerHeartLeft);
        }
        finished = true;
        return Score;
    }
}
