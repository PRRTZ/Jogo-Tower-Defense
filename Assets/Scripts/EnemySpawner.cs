using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Ondas em fases:
///   Preparação (sem inimigos, dá para construir) -> Onda (inimigos saindo) -> Combate (espera todos sumirem)
///   -> bônus de moedas e um pouco de vida para o player -> Preparação da próxima...
/// A cada onda os monstros ficam com mais vida e mais rápidos.
/// A ÚLTIMA onda tem um CHEFE: muita vida, chama reforços e fica furioso (ruge parado e depois anda um pouco mais rápido).
/// Os números também são ajustados por mapa (GameSession.Maps em GameData.cs).
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
    public float bossHealthMultiplier = 6f;
    [Tooltip("Velocidade do chefe em relação aos outros monstros (0.6 = 40% mais lento).")]
    public float bossSpeedFactor = 0.6f;
    public float bossEscortInterval = 10f;   // segundos entre os reforços
    public int bossEscortCount = 2;          // monstros por reforço
    [Tooltip("Vida dos reforços em relação a um monstro normal da onda (0.5 = metade).")]
    public float bossEscortHealthFactor = 0.5f;
    [Tooltip("Limite de reforços vivos ao mesmo tempo.")]
    public int bossMaxEscortsAlive = 6;
    [Range(0.1f, 0.9f)] public float bossRageAt = 0.35f;  // fica furioso com esta fração da vida
    public float bossRageSpeedMultiplier = 1.2f;
    [Tooltip("Intervalo dos reforços x este valor quando furioso (1 = não muda).")]
    public float bossRageEscortFactor = 1f;
    [Tooltip("Segundos parado rugindo ao ficar furioso.")]
    public float bossRageRoarTime = 2f;
    public int bossReward = 300;             // moedas ao derrotar o chefe

    [Header("Aura do chefe")]
    [Tooltip("Efeito preso no chefe (ex.: Hovl Studio > Character auras > Star aura). Vazio = sem aura.")]
    public GameObject bossAuraPrefab;
    [Tooltip("Largura da aura em relação à largura do chefe (1.3 = 30% maior que o modelo).")]
    public float bossAuraSize = 1.3f;
    [Tooltip("Largura da aura no tamanho original do prefab, em metros (Star aura ≈ 2).")]
    public float bossAuraPrefabWidth = 2f;

    [Header("Torres")]
    [Tooltip("Dano de todas as torres construídas (1 = valor do prefab). Com 'Use Difficulty', vem da dificuldade.")]
    public float towerDamageMultiplier = 1f;

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
    /// <summary>Quanto a vida dos monstros já cresceu nesta onda (1 na 1ª onda). O golpe do player usa isto.</summary>
    public float WaveHealthGrowth => Mathf.Pow(1f + healthGrowthPerWave, Mathf.Max(0, CurrentWave - 1));
    public string BossDisplayName { get; private set; } = "";

    readonly HashSet<GameObject> alive = new HashSet<GameObject>();
    readonly HashSet<GameObject> escorts = new HashSet<GameObject>();
    int startingMoneyBonus;
    float countdown;
    int nextIndex;
    Health playerHealth;

    void Awake()
    {
        Instance = this;
        if (useDifficulty)
        {
            ApplyDifficulty(GameSession.Current);
            ApplyMap(FindMapBalance());
        }
    }

    // acha o ajuste pelo nome da cena (funciona mesmo dando Play direto no mapa); senão usa o MapIndex do menu
    static MapBalance FindMapBalance()
    {
        string scene = SceneManager.GetActiveScene().name;
        foreach (var m in GameSession.Maps)
            if (m != null && m.scene == scene) return m;
        return GameSession.CurrentMap;
    }

    void ApplyMap(MapBalance m)
    {
        if (m == null) return;
        healthMultiplier *= m.enemyHealthMultiplier;
        bossHealthMultiplier *= m.bossHealthMultiplier;
        rewardMultiplier *= m.rewardMultiplier;
        waveClearBonusBase = Mathf.RoundToInt(waveClearBonusBase * m.rewardMultiplier);
        waveClearBonusPerWave = Mathf.RoundToInt(waveClearBonusPerWave * m.rewardMultiplier);
        startingMoneyBonus += m.startingMoneyBonus;
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
        towerDamageMultiplier = d.towerDamageMultiplier > 0f ? d.towerDamageMultiplier : 1f;
        bossEscortHealthFactor = d.bossEscortHealthFactor;
        bossMaxEscortsAlive = d.bossMaxEscortsAlive;
        bossRageAt = d.bossRageAt;
        bossRageSpeedMultiplier = d.bossRageSpeedMultiplier;
        bossRageEscortFactor = d.bossRageEscortFactor;
        bossRageRoarTime = d.bossRageRoarTime;
        rewardMultiplier = d.rewardMultiplier;
        baseKillReward = d.killReward;
        rewardGrowthPerWave = d.rewardGrowthPerWave;
        waveClearBonusBase = d.waveBonusBase;
        waveClearBonusPerWave = d.waveBonusPerWave;
        earlyCallBonusPerSecond = d.earlyCallBonusPerSecond;
        startingMoneyBonus += d.startingMoney;
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

        if (startingMoneyBonus > 0 && CurrencyManager.Instance != null)
            CurrencyManager.Instance.AddMoney(startingMoneyBonus);

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

        if (PauseSettingsMenu.IsOpen) return;

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
        SoundManager.Play(Sfx.WaveStart);
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

        SpawnOne(index, 1f);
    }

    GameObject SpawnOne(int index, float healthFactor)
    {
        if (spawnPoint == null || enemyPrefabs == null || enemyPrefabs.Length == 0) return null;
        var prefab = enemyPrefabs[index % enemyPrefabs.Length];
        if (prefab == null) return null;

        var enemy = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
        ApplyWaveStrength(enemy, healthFactor, 1f);
        alive.Add(enemy);
        return enemy;
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
            escorts.RemoveWhere(g => g == null);
            if (bossMaxEscortsAlive > 0 && escorts.Count >= bossMaxEscortsAlive) yield break;  // já tem reforço demais

            var e = SpawnEscortOne();
            if (e != null) escorts.Add(e);
            yield return new WaitForSeconds(delayBetween);
        }
    }

    // reforço: próximo monstro liberado, com menos vida que um monstro normal da onda
    GameObject SpawnEscortOne()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0) return null;
        int pool = enemyPrefabs.Length;
        if (unlockedAtStart > 0)
            pool = Mathf.Clamp(unlockedAtStart + Mathf.Max(0, CurrentWave - 1) * unlockPerWave, 1, enemyPrefabs.Length);
        return SpawnOne(nextIndex++ % pool, bossEscortHealthFactor);
    }

    void SpawnBoss()
    {
        GameObject prefab = bossPrefab;
        if (prefab == null && enemyPrefabs != null && enemyPrefabs.Length > 0) prefab = enemyPrefabs[enemyPrefabs.Length - 1];
        if (prefab == null || spawnPoint == null) return;

        var boss = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
        boss.name = "BOSS_" + prefab.name;
        boss.transform.localScale *= bossScale;

        // a vida do chefe parte da vida de um monstro COMUM (não da vida salva no prefab do chefe),
        // assim trocar o prefab do chefe não muda o balanceamento
        var bossHealth = boss.GetComponent<EnemyHealth>();
        if (bossHealth != null) bossHealth.maxHealth = RegularEnemyBaseHealth();
        ApplyWaveStrength(boss, bossHealthMultiplier, bossSpeedFactor);

        var reward = boss.GetComponent<EnemyReward>();
        if (reward == null) reward = boss.AddComponent<EnemyReward>();
        reward.Reward = Mathf.RoundToInt(bossReward * rewardMultiplier);

        AttachBossAura(boss);

        var controller = boss.AddComponent<BossController>();
        // furioso nunca fica mais rápido que um monstro normal da onda
        float rageSpeed = bossSpeedFactor > 0f ? Mathf.Min(bossRageSpeedMultiplier, 1f / bossSpeedFactor) : bossRageSpeedMultiplier;
        controller.Setup(this, bossEscortInterval, bossEscortCount, bossRageAt, rageSpeed, bossRageEscortFactor, bossRageRoarTime);

        Boss = boss.GetComponent<EnemyHealth>();
        BossDisplayName = string.IsNullOrEmpty(bossName) ? prefab.name.Replace("_", " ") : bossName;
        alive.Add(boss);
        OnBossSpawned?.Invoke(Boss);
    }

    // prende a aura no chefe, do tamanho do modelo (mede a largura real pelos renderers)
    void AttachBossAura(GameObject boss)
    {
        if (bossAuraPrefab == null || boss == null) return;

        float width = 0f;
        bool found = false;
        Bounds b = new Bounds(boss.transform.position, Vector3.zero);
        foreach (var r in boss.GetComponentsInChildren<Renderer>())
        {
            if (r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer) continue;
            if (r.GetComponentInParent<Canvas>() != null) continue;           // barra de vida etc.
            if (!found) { b = r.bounds; found = true; } else b.Encapsulate(r.bounds);
        }
        width = found ? Mathf.Max(b.size.x, b.size.z) : bossScale;
        if (width < 0.1f) width = bossScale;

        var aura = Instantiate(bossAuraPrefab, boss.transform.position, Quaternion.identity);
        aura.name = "Aura";
        float s = width * bossAuraSize / Mathf.Max(0.1f, bossAuraPrefabWidth);
        aura.transform.localScale = bossAuraPrefab.transform.localScale * s;

        // partículas crescem com a escala do objeto e acompanham o chefe andando
        foreach (var ps in aura.GetComponentsInChildren<ParticleSystem>())
        {
            var main = ps.main;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        }

        aura.transform.SetParent(boss.transform, true);   // mantém o tamanho calculado
    }

    // vida de prefab de um monstro comum (o 1º da lista); 50 se não achar
    float RegularEnemyBaseHealth()
    {
        if (enemyPrefabs != null)
            foreach (var p in enemyPrefabs)
            {
                if (p == null) continue;
                var h = p.GetComponent<EnemyHealth>();
                if (h != null && h.maxHealth > 1f) return h.maxHealth;
            }
        return 50f;
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
