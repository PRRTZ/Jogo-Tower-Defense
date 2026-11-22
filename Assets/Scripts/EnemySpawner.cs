using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Ondas em fases:
///   Preparação (sem inimigos, dá para construir) -> Onda (inimigos saindo) -> Combate (espera todos sumirem)
///   -> bônus de moedas e um pouco de vida para o player -> Preparação da próxima...
/// Depois da última onda (maxWave), dispara OnAllWavesCleared (vitória).
///
/// Teclas:  N = chamar a próxima onda antes da hora (ganha bônus pelo tempo que sobrou)
///          T = spawna um monstro (teste)    Y = spawna todos da lista, um de cada (teste)
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    public enum Phase { Preparing, Spawning, Fighting, Finished }

    public static EnemySpawner Instance { get; private set; }

    public static event Action<int> OnWaveStarted;              // número da onda
    public static event Action<int, int> OnWaveCleared;         // número da onda, bônus em moedas
    public static event Action OnAllWavesCleared;               // venceu a fase

    [Header("Inimigos")]
    public GameObject[] enemyPrefabs;        // do mais fraco ao mais forte, se for liberar aos poucos

    [Header("Local de spawn")]
    public Transform spawnPoint;             // se vazio, procura "EnemySpawnPoint" ou "SpawnPoint"

    [Header("Ondas")]
    public bool spawnOnStart = true;         // desligue para não ter ondas (só as teclas de teste)
    public float startDelay = 15f;           // preparação antes da 1ª onda
    public float wavesDelay = 15f;           // preparação entre uma onda e outra
    public float delayBetween = 0.8f;        // intervalo entre um monstro e outro
    public int maxWave = 10;                 // 0 = infinito
    public int firstWaveSize = 2;            // monstros na 1ª onda
    public int enemiesPerWave = 1;           // monstros a mais em cada onda

    [Header("Liberar monstros aos poucos (opcional)")]
    [Tooltip("0 = todos os monstros da lista desde a 1ª onda. Ex.: 2 = só os 2 primeiros da lista na 1ª onda.")]
    public int unlockedAtStart = 0;
    [Tooltip("Quantos monstros novos da lista são liberados a cada onda.")]
    public int unlockPerWave = 1;

    [Header("Monstros mais fortes a cada onda")]
    [Tooltip("0.15 = +15% de vida por onda.")]
    public float healthGrowthPerWave = 0.15f;
    [Tooltip("Moedas por monstro na 1ª onda.")]
    public int baseKillReward = 10;
    [Tooltip("0.10 = +10% de moedas por monstro a cada onda.")]
    public float rewardGrowthPerWave = 0.10f;

    [Header("Recompensas")]
    public int waveClearBonusBase = 20;      // bônus ao terminar a onda = base + porOnda * número da onda
    public int waveClearBonusPerWave = 10;
    public float earlyCallBonusPerSecond = 2f;  // bônus por segundo de preparação pulado com N
    public float healPlayerOnWaveClear = 20f;   // vida devolvida ao player no fim da onda (20 = 1 coração)

    // ---------- estado (a HUD lê isto) ----------
    public Phase CurrentPhase { get; private set; } = Phase.Preparing;
    public int CurrentWave { get; private set; }
    public int MaxWave => maxWave;
    public float PrepTimeLeft => CurrentPhase == Phase.Preparing ? Mathf.Max(0f, countdown) : 0f;
    public int EarlyCallBonus => Mathf.CeilToInt(PrepTimeLeft * earlyCallBonusPerSecond);
    public int AliveEnemies { get { CleanAlive(); return alive.Count; } }
    public bool HasMoreWaves => maxWave <= 0 || CurrentWave < maxWave;

    readonly HashSet<GameObject> alive = new HashSet<GameObject>();
    float countdown;
    int nextIndex;
    Health playerHealth;

    void Awake()
    {
        Instance = this;
    }

    void OnEnable()
    {
        EnemyHealth.OnEnemyDied += HandleEnemyDied;
        EnemyMovement.OnReachedBase += HandleEnemyReachedBase;
    }

    void OnDisable()
    {
        EnemyHealth.OnEnemyDied -= HandleEnemyDied;
        EnemyMovement.OnReachedBase -= HandleEnemyReachedBase;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        if (spawnPoint == null)
        {
            var sp = GameObject.Find("EnemySpawnPoint");
            if (sp == null) sp = GameObject.Find("SpawnPoint");
            if (sp != null) spawnPoint = sp.transform;
        }

        var pc = FindAnyObjectByType<PlayerController>();
        if (pc != null) playerHealth = pc.GetComponent<Health>();

        countdown = startDelay;
        CurrentPhase = Phase.Preparing;
    }

    void Update()
    {
        if (spawnOnStart)
        {
            switch (CurrentPhase)
            {
                case Phase.Preparing:
                    countdown -= Time.deltaTime;
                    if (countdown <= 0f) StartWave();
                    break;

                case Phase.Fighting:
                    if (AliveEnemies == 0) FinishWave();
                    break;
            }
        }

        var kb = Keyboard.current;
        if (kb == null) return;

        // N: chama a próxima onda agora e ganha bônus pelo tempo que sobrou
        if (kb.nKey.wasPressedThisFrame && spawnOnStart && CurrentPhase == Phase.Preparing)
        {
            int bonus = EarlyCallBonus;
            if (bonus > 0 && CurrencyManager.Instance != null) CurrencyManager.Instance.AddMoney(bonus);
            StartWave();
        }

        // T: um monstro / Y: todos da lista (teste)
        if (kb.tKey.wasPressedThisFrame) SpawnNext();
        if (kb.yKey.wasPressedThisFrame) StartCoroutine(SpawnAllRoutine());
    }

    // ---------- fases ----------

    void StartWave()
    {
        if (!HasMoreWaves) return;

        CurrentWave++;
        CurrentPhase = Phase.Spawning;
        OnWaveStarted?.Invoke(CurrentWave);
        StartCoroutine(SpawnWave());
    }

    IEnumerator SpawnWave()
    {
        int count = Mathf.Max(1, firstWaveSize + (CurrentWave - 1) * enemiesPerWave);
        for (int i = 0; i < count; i++)
        {
            SpawnNext();
            yield return new WaitForSeconds(delayBetween);
        }
        CurrentPhase = Phase.Fighting;
    }

    void FinishWave()
    {
        int bonus = waveClearBonusBase + waveClearBonusPerWave * CurrentWave;
        if (CurrencyManager.Instance != null) CurrencyManager.Instance.AddMoney(bonus);
        if (playerHealth != null && healPlayerOnWaveClear > 0f) playerHealth.Heal(healPlayerOnWaveClear);

        OnWaveCleared?.Invoke(CurrentWave, bonus);

        if (HasMoreWaves)
        {
            CurrentPhase = Phase.Preparing;
            countdown = wavesDelay;
        }
        else
        {
            CurrentPhase = Phase.Finished;
            OnAllWavesCleared?.Invoke();
        }
    }

    // ---------- spawn ----------

    IEnumerator SpawnAllRoutine()
    {
        for (int i = 0; i < enemyPrefabs.Length; i++)
        {
            SpawnOne(i);
            yield return new WaitForSeconds(delayBetween);
        }
    }

    // próximo monstro da lista, só entre os já liberados
    public void SpawnNext()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0) { SpawnOne(0); return; }

        int pool = enemyPrefabs.Length;
        if (unlockedAtStart > 0)
            pool = Mathf.Clamp(unlockedAtStart + Mathf.Max(0, CurrentWave - 1) * unlockPerWave, 1, enemyPrefabs.Length);

        SpawnOne(nextIndex++ % pool);
    }

    public void SpawnOne(int index)
    {
        if (spawnPoint == null || enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            Debug.LogWarning("Falta prefab ou SpawnPoint");
            return;
        }

        var prefab = enemyPrefabs[index % enemyPrefabs.Length];
        if (prefab == null) return;

        var enemy = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
        ApplyWaveStrength(enemy);
        alive.Add(enemy);
    }

    // vida e recompensa crescem com o número da onda
    void ApplyWaveStrength(GameObject enemy)
    {
        int steps = Mathf.Max(0, CurrentWave - 1);

        var health = enemy.GetComponent<EnemyHealth>();
        if (health != null)
            health.SetMaxHealth(health.maxHealth * Mathf.Pow(1f + healthGrowthPerWave, steps));

        var reward = enemy.GetComponent<EnemyReward>();
        if (reward == null) reward = enemy.AddComponent<EnemyReward>();
        reward.Reward = Mathf.RoundToInt(baseKillReward * (1f + rewardGrowthPerWave * steps));
    }

    // ---------- contagem de inimigos vivos ----------

    void HandleEnemyDied(EnemyHealth e)
    {
        if (e != null) alive.Remove(e.gameObject);
    }

    void HandleEnemyReachedBase(EnemyMovement e)
    {
        if (e != null) alive.Remove(e.gameObject);
    }

    void CleanAlive()
    {
        alive.RemoveWhere(g => g == null);
    }
}
