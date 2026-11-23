using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Ondas em fases:
///   Preparação (sem inimigos, dá para construir) -> Onda (inimigos saindo) -> Combate (espera todos sumirem)
///   -> bônus de moedas e um pouco de vida para o player -> Preparação da próxima...
/// A cada onda os monstros ficam com mais vida e mais rápidos.
/// A ÚLTIMA onda tem um CHEFE: muita vida, chama reforços e fica furioso (mais rápido) com metade da vida.
/// Vencendo a última onda, dispara OnAllWavesCleared (vitória).
///
/// Com "Use Difficulty" ligado, os números vêm da dificuldade escolhida no menu (GameData.cs).
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
    public static event Action<EnemyHealth> OnBossSpawned;      // o chefe apareceu
    public static event Action OnBossRage;                      // o chefe ficou furioso
    public static event Action OnBossDefeated;                  // o chefe morreu

    [Header("Inimigos")]
    public GameObject[] enemyPrefabs;        // do mais fraco ao mais forte, se for liberar aos poucos

    [Header("Local de spawn")]
    public Transform spawnPoint;             // se vazio, procura "EnemySpawnPoint" ou "SpawnPoint"

    [Header("Dificuldade")]
    [Tooltip("Ligado: os números das ondas, monstros e chefe vêm da dificuldade escolhida no menu (GameData.cs). " +
             "Desligado: usa os valores deste Inspector (bom para testar).")]
    public bool useDifficulty = true;

    [Header("Ondas")]
    public bool spawnOnStart = true;         // desligue para não ter ondas (só as teclas de teste)
    public float startDelay = 15f;           // preparação antes da 1ª onda
    public float wavesDelay = 15f;           // preparação entre uma onda e outra
    public float delayBetween = 0.8f;        // intervalo entre um monstro e outro
    public int maxWave = 15;                 // 0 = infinito (sem chefe)
    public int firstWaveSize = 3;            // monstros na 1ª onda
    public int enemiesPerWave = 2;           // monstros a mais em cada onda

    [Header("Liberar monstros aos poucos (opcional)")]
    [Tooltip("0 = todos os monstros da lista desde a 1ª onda. Ex.: 2 = só os 2 primeiros da lista na 1ª onda.")]
    public int unlockedAtStart = 0;
    [Tooltip("Quantos monstros novos da lista são liberados a cada onda.")]
    public int unlockPerWave = 1;

    [Header("Monstros mais fortes a cada onda")]
    public float healthMultiplier = 1f;      // vida base (1 = vida do prefab)
    [Tooltip("0.18 = +18% de vida por onda (acumulativo).")]
    public float healthGrowthPerWave = 0.18f;
    public float speedMultiplier = 1f;       // velocidade base
    [Tooltip("0.03 = +3% de velocidade por onda.")]
    public float speedGrowthPerWave = 0.03f;
    public float maxSpeedMultiplier = 1.5f;  // limite da velocidade
    [Tooltip("Moedas por monstro na 1ª onda.")]
    public int baseKillReward = 10;
    [Tooltip("0.10 = +10% de moedas por monstro a cada onda.")]
    public float rewardGrowthPerWave = 0.10f;
    public float rewardMultiplier = 1f;

    [Header("Chefe (última onda)")]
    public bool bossEnabled = true;
    [Tooltip("Vazio = usa o último monstro da lista Enemy Prefabs.")]
    public GameObject bossPrefab;
    public string bossName = "";             // vazio = nome do prefab
    public float bossScale = 2.2f;           // tamanho do chefe
    [Tooltip("Vida do chefe = vida de um monstro da última onda x este valor.")]
    public float bossHealthMultiplier = 15f;
    [Tooltip("Velocidade do chefe em relação aos outros monstros (0.6 = 40% mais lento).")]
    public float bossSpeedFactor = 0.6f;
    public float bossEscortInterval = 8f;    // segundos entre os reforços
    public int bossEscortCount = 2;          // monstros por reforço
    [Range(0.1f, 0.9f)] public float bossRageAt = 0.5f;   // fica furioso com esta fração da vida
    public float bossRageSpeedMultiplier = 1.6f;
    public int bossReward = 300;             // moedas ao derrotar o chefe

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
    public bool IsBossWave => bossEnabled && maxWave > 0 && CurrentWave == maxWave;
    public EnemyHealth Boss { get; private set; }
    public string BossDisplayName { get; private set; } = "";

    readonly HashSet<GameObject> alive = new HashSet<GameObject>();
    float countdown;
    int nextIndex;
    Health playerHealth;

    void Awake()
    {
        Instance = this;
        if (useDifficulty) ApplyDifficulty(GameSession.Current);
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

    void ApplyDifficulty(DifficultyConfig d)
    {
        if (d == null) return;
        maxWave = d.waves;
        startDelay = d.prepTime;
        wavesDelay = d.prepTime;
        firstWaveSize = d.firstWaveSize;
        enemiesPerWave = d.enemiesPerWave;
        delayBetween = d.spawnInterval;
        healthMultiplier = d.enemyHealthMultiplier;
        healthGrowthPerWave = d.healthGrowthPerWave;
        speedMultiplier = d.enemySpeedMultiplier;
        speedGrowthPerWave = d.speedGrowthPerWave;
        maxSpeedMultiplier = d.maxSpeedMultiplier;
        bossHealthMultiplier = d.bossHealthMultiplier;
        bossEscortInterval = d.bossEscortInterval;
        bossEscortCount = d.bossEscortCount;
        rewardMultiplier = d.rewardMultiplier;
        baseKillReward = d.killReward;
        rewardGrowthPerWave = d.rewardGrowthPerWave;
        waveClearBonusBase = d.waveBonusBase;
        waveClearBonusPerWave = d.waveBonusPerWave;
        earlyCallBonusPerSecond = d.earlyCallBonusPerSecond;
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
        bool boss = IsBossWave;
        if (boss) count = Mathf.Max(1, count / 2);   // na onda do chefe, metade dos monstros vem antes dele

        for (int i = 0; i < count; i++)
        {
            SpawnNext();
            yield return new WaitForSeconds(delayBetween);
        }

        if (boss)
        {
            yield return new WaitForSeconds(2f);
            SpawnBoss();
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
        ApplyWaveStrength(enemy, 1f, 1f);
        alive.Add(enemy);
    }

    /// <summary>Reforços chamados pelo chefe: saem do ponto de spawn com a força da onda atual.</summary>
    public void SpawnEscorts(int count)
    {
        StartCoroutine(SpawnEscortsRoutine(count));
    }

    IEnumerator SpawnEscortsRoutine(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (Boss == null) yield break;          // chefe morreu: para de chamar
            SpawnNext();
            yield return new WaitForSeconds(delayBetween);
        }
    }

    void SpawnBoss()
    {
        GameObject prefab = bossPrefab;
        if (prefab == null && enemyPrefabs != null && enemyPrefabs.Length > 0) prefab = enemyPrefabs[enemyPrefabs.Length - 1];
        if (prefab == null || spawnPoint == null) return;

        var boss = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
        boss.name = "BOSS_" + prefab.name;
        boss.transform.localScale *= bossScale;
        ApplyWaveStrength(boss, bossHealthMultiplier, bossSpeedFactor);

        var reward = boss.GetComponent<EnemyReward>();
        if (reward == null) reward = boss.AddComponent<EnemyReward>();
        reward.Reward = Mathf.RoundToInt(bossReward * rewardMultiplier);

        var controller = boss.AddComponent<BossController>();
        controller.Setup(this, bossEscortInterval, bossEscortCount, bossRageAt, bossRageSpeedMultiplier);

        Boss = boss.GetComponent<EnemyHealth>();
        BossDisplayName = string.IsNullOrEmpty(bossName) ? prefab.name.Replace("_", " ") : bossName;
        alive.Add(boss);
        OnBossSpawned?.Invoke(Boss);
    }

    /// <summary>Chamado pelo BossController quando o chefe fica furioso.</summary>
    public void NotifyBossRage() => OnBossRage?.Invoke();

    // vida, velocidade e recompensa crescem com o número da onda
    void ApplyWaveStrength(GameObject enemy, float extraHealth, float extraSpeed)
    {
        int steps = Mathf.Max(0, CurrentWave - 1);

        var health = enemy.GetComponent<EnemyHealth>();
        if (health != null)
        {
            float hp = health.maxHealth * healthMultiplier * Mathf.Pow(1f + healthGrowthPerWave, steps) * extraHealth;
            health.SetMaxHealth(hp);
        }

        float speedMult = Mathf.Min(maxSpeedMultiplier, speedMultiplier * (1f + speedGrowthPerWave * steps)) * extraSpeed;
        var move = enemy.GetComponent<EnemyMovement>();
        if (move != null) move.speed *= speedMult;
        var anim = enemy.GetComponentInChildren<Animator>();
        if (anim != null) anim.speed = Mathf.Clamp(speedMult, 0.6f, 1.6f);   // a animação acompanha

        var reward = enemy.GetComponent<EnemyReward>();
        if (reward == null) reward = enemy.AddComponent<EnemyReward>();
        reward.Reward = Mathf.RoundToInt(baseKillReward * (1f + rewardGrowthPerWave * steps) * rewardMultiplier);
    }

    // ---------- contagem de inimigos vivos ----------

    void HandleEnemyDied(EnemyHealth e)
    {
        if (e == null) return;
        alive.Remove(e.gameObject);

        if (Boss != null && e == Boss)
        {
            Boss = null;
            OnBossDefeated?.Invoke();
        }
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
