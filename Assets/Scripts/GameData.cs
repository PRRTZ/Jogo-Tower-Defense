using System;
using System.Collections.Generic;
using UnityEngine;

// Configuração de cada nível de dificuldade (ajuste os números à vontade)
[Serializable]
public class DifficultyConfig
{
    public string name;
    public string description;

    [Header("Ondas")]
    public int waves;                     // quantidade de ondas (a última tem o chefe)
    public float prepTime;                // segundos de preparação antes de cada onda
    public int firstWaveSize;             // monstros na 1ª onda
    public int enemiesPerWave;            // monstros a mais a cada onda
    public float spawnInterval;           // segundos entre um monstro e o próximo

    [Header("Monstros")]
    public float enemyHealthMultiplier;   // vida base (1 = vida do prefab)
    public float healthGrowthPerWave;     // 0.18 = +18% de vida por onda (acumulativo)
    public float enemySpeedMultiplier;    // velocidade base
    public float speedGrowthPerWave;      // 0.03 = +3% de velocidade por onda
    public float maxSpeedMultiplier;      // limite da velocidade (para não virar foguete)

    [Header("Chefe (última onda)")]
    public float bossHealthMultiplier;    // vida do chefe = vida de um monstro da última onda x este valor
    public float bossEscortInterval;      // segundos entre os reforços chamados pelo chefe
    public int bossEscortCount;           // monstros por reforço
    public float bossEscortHealthFactor;  // vida dos reforços em relação a um monstro normal da onda (0.6 = 60%)
    public int bossMaxEscortsAlive;       // limite de reforços vivos ao mesmo tempo (evita enxurrada)

    [Header("Chefe furioso")]
    public float bossRageAt;              // fica furioso com esta fração da vida (0.35 = com 35%)
    public float bossRageSpeedMultiplier; // velocidade x este valor quando furioso
    public float bossRageEscortFactor;    // intervalo dos reforços x este valor quando furioso (1 = não muda)
    public float bossRageRoarTime;        // segundos parado rugindo ao ficar furioso (janela para as torres)

    [Header("Torres")]
    public float towerDamageMultiplier;   // dano de todas as torres (1 = valor do prefab)

    [Header("Economia")]
    public int startingMoney;             // moedas no começo da fase (0 = a 1ª onda é no corpo a corpo)
    public int killReward;                // moedas por monstro na 1ª onda
    public float rewardGrowthPerWave;     // 0.05 = +5% de moedas por monstro a cada onda
    public int waveBonusBase;             // bônus ao vencer a onda = base + porOnda x número da onda
    public int waveBonusPerWave;
    public float earlyCallBonusPerSecond; // moedas por segundo de preparação pulado com N

    [Header("Recompensas e pontos")]
    public float rewardMultiplier;        // multiplica as moedas por monstro
    public float scoreMultiplier;         // multiplica a pontuação (difícil vale mais)
}

// Ajuste por mapa (multiplica em cima da dificuldade). Mapas com menos TowerSpots
// precisam de monstros um pouco mais fracos e de mais moedas para compensar.
[Serializable]
public class MapBalance
{
    public string scene;                  // só para referência
    public float enemyHealthMultiplier;   // vida dos monstros (e reforços) neste mapa
    public float bossHealthMultiplier;    // vida do chefe neste mapa (em cima do valor acima)
    public float rewardMultiplier;        // moedas por monstro e bônus de onda
    public int startingMoneyBonus;        // moedas extras no começo da fase
}

// Dados que o menu passa para a cena do jogo
public static class GameSession
{
    public static int DifficultyIndex = 0;
    public static int MapIndex = 0;
    public static string PlayerName = "Jogador";

    // Vida dos monstros (prefab = 50):
    //   Normal  onda 1 = 65, última (15) = 65 x 1.11^14  ≈ 280
    //   Difícil onda 1 = 75, última (20) = 75 x 1.073^19 ≈ 286
    // Golpe do player: 14 fixo; forja +50%/nível (máx. 42). Golpes num monstro comum do Map_01, sem forja / forja máx.:
    //   Normal  onda 1: 5/2   onda 7: 9/3   onda 15: 21/7
    //   Difícil onda 1: 6/2   onda 7: 9/3   onda 20: 21/7
    // Vida do chefe = monstro COMUM da última onda x bossHealthMultiplier
    // (não usa a vida salva no prefab do chefe):
    //   Normal  ≈ 280 x 3.9 ≈ 1090       Difícil ≈ 286 x 5.7 ≈ 1630
    //   Normal calibrado na geometria do Map_01 (torres a 60%): com 7 torres e upgrades desiguais,
    //   as torres dão ~1550 de dano no chefe até a última curva -> ele morre por volta de 70% do caminho até ela.
    //   Map_02: x 0.8 (monstros) x 0.9 (chefe) -> Normal ≈ 790, Difícil ≈ 1170
    // Torres com 60% do dano do prefab (Turret nv1: 6 -> 3,6 por tiro, ~14 de dano/s):
    // 2 Turrets nv1 já não seguram a onda 6 sozinhas.
    // Começa com 0 moedas e não há bônus ao vencer a onda: o dinheiro vem só de matar monstros.
    // Normal: ~98 moedas no fim da onda 3 -> a 1ª torre (90) sai aí.
    public static readonly DifficultyConfig[] Difficulties =
    {
        new DifficultyConfig {
            name = "Normal", description = "Ondas que crescem aos poucos e um chefe no final.",
            waves = 15, prepTime = 15f, firstWaveSize = 3, enemiesPerWave = 2, spawnInterval = 0.9f,
            enemyHealthMultiplier = 1.3f, healthGrowthPerWave = 0.11f,
            enemySpeedMultiplier = 1f, speedGrowthPerWave = 0.015f, maxSpeedMultiplier = 1.25f,
            bossHealthMultiplier = 3.9f, bossEscortInterval = 10f, bossEscortCount = 2,
            bossEscortHealthFactor = 0.5f, bossMaxEscortsAlive = 6,
            bossRageAt = 0.35f, bossRageSpeedMultiplier = 1.2f, bossRageEscortFactor = 1f, bossRageRoarTime = 2f,
            towerDamageMultiplier = 0.6f,
            startingMoney = 0,
            killReward = 6, rewardGrowthPerWave = 0.07f, waveBonusBase = 0, waveBonusPerWave = 0, earlyCallBonusPerSecond = 1f,
            rewardMultiplier = 1f, scoreMultiplier = 1f },

        new DifficultyConfig {
            name = "Difícil", description = "Mais ondas, monstros resistentes e rápidos, chefe furioso.",
            waves = 20, prepTime = 12f, firstWaveSize = 4, enemiesPerWave = 2, spawnInterval = 0.7f,
            enemyHealthMultiplier = 1.5f, healthGrowthPerWave = 0.073f,
            enemySpeedMultiplier = 1.05f, speedGrowthPerWave = 0.02f, maxSpeedMultiplier = 1.4f,
            bossHealthMultiplier = 5.7f, bossEscortInterval = 8f, bossEscortCount = 2,
            bossEscortHealthFactor = 0.6f, bossMaxEscortsAlive = 8,
            bossRageAt = 0.4f, bossRageSpeedMultiplier = 1.3f, bossRageEscortFactor = 0.85f, bossRageRoarTime = 1.5f,
            towerDamageMultiplier = 0.6f,
            startingMoney = 0,
            killReward = 6, rewardGrowthPerWave = 0.06f, waveBonusBase = 0, waveBonusPerWave = 0, earlyCallBonusPerSecond = 1f,
            rewardMultiplier = 1f, scoreMultiplier = 1.5f },
    };

    // Mesma ordem do "mapScenes" do MainMenu. Map_01 tem 11 TowerSpots, Map_02 só 7.
    public static readonly MapBalance[] Maps =
    {
        new MapBalance { scene = "Map_01", enemyHealthMultiplier = 1f,    bossHealthMultiplier = 1f,    rewardMultiplier = 1f,    startingMoneyBonus = 0 },
        new MapBalance { scene = "Map_02", enemyHealthMultiplier = 0.8f,  bossHealthMultiplier = 0.9f,  rewardMultiplier = 1.2f,  startingMoneyBonus = 0 },
    };

    // Use no jogo: GameSession.Current.waves, .enemyHealthMultiplier, etc.
    public static DifficultyConfig Current
    {
        get { return Difficulties[Mathf.Clamp(DifficultyIndex, 0, Difficulties.Length - 1)]; }
    }

    // Ajuste do mapa atual (mapa sem entrada na lista = sem ajuste)
    public static MapBalance CurrentMap
    {
        get
        {
            if (MapIndex >= 0 && MapIndex < Maps.Length) return Maps[MapIndex];
            return new MapBalance { scene = "", enemyHealthMultiplier = 1f, bossHealthMultiplier = 1f, rewardMultiplier = 1f, startingMoneyBonus = 0 };
        }
    }
}

// Ranking simples salvo no computador (PlayerPrefs), top 5 por dificuldade
public static class Ranking
{
    public const int MaxEntries = 5;

    [Serializable] public class Entry { public string name; public int score; }
    [Serializable] public class Data { public List<Entry> entries = new List<Entry>(); }

    static string Key(int difficulty) { return "ranking_diff_" + difficulty; }

    public static List<Entry> Get(int difficulty)
    {
        string json = PlayerPrefs.GetString(Key(difficulty), "");
        if (string.IsNullOrEmpty(json)) return new List<Entry>();
        try
        {
            Data data = JsonUtility.FromJson<Data>(json);
            return (data != null && data.entries != null) ? data.entries : new List<Entry>();
        }
        catch { return new List<Entry>(); }
    }

    // Chame ao vencer: Ranking.Add(GameSession.DifficultyIndex, GameSession.PlayerName, score);
    public static void Add(int difficulty, string playerName, int score)
    {
        List<Entry> list = Get(difficulty);
        list.Add(new Entry
        {
            name = string.IsNullOrWhiteSpace(playerName) ? "Jogador" : playerName.Trim(),
            score = score
        });
        list.Sort((a, b) => b.score.CompareTo(a.score));
        if (list.Count > MaxEntries) list.RemoveRange(MaxEntries, list.Count - MaxEntries);

        PlayerPrefs.SetString(Key(difficulty), JsonUtility.ToJson(new Data { entries = list }));
        PlayerPrefs.Save();
    }

    public static void Clear()
    {
        for (int i = 0; i < GameSession.Difficulties.Length; i++) PlayerPrefs.DeleteKey(Key(i));
        PlayerPrefs.Save();
    }

    // Salva e diz em que posição a pontuação entrou (1 = primeiro lugar, 0 = não entrou no top)
    public static int AddAndGetPosition(int difficulty, string playerName, int score)
    {
        Add(difficulty, playerName, score);
        List<Entry> list = Get(difficulty);
        string n = string.IsNullOrWhiteSpace(playerName) ? "Jogador" : playerName.Trim();
        for (int i = 0; i < list.Count; i++)
            if (list[i].score == score && list[i].name == n) return i + 1;
        return 0;
    }
}
