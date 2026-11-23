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

    [Header("Economia")]
    public int killReward;                // moedas por monstro na 1ª onda
    public float rewardGrowthPerWave;     // 0.05 = +5% de moedas por monstro a cada onda
    public int waveBonusBase;             // bônus ao vencer a onda = base + porOnda x número da onda
    public int waveBonusPerWave;
    public float earlyCallBonusPerSecond; // moedas por segundo de preparação pulado com N

    [Header("Recompensas e pontos")]
    public float rewardMultiplier;        // multiplica as moedas por monstro
    public float scoreMultiplier;         // multiplica a pontuação (difícil vale mais)
}

// Dados que o menu passa para a cena do jogo
public static class GameSession
{
    public static int DifficultyIndex = 0;
    public static int MapIndex = 0;
    public static string PlayerName = "Jogador";

    public static readonly DifficultyConfig[] Difficulties =
    {
        new DifficultyConfig {
            name = "Normal", description = "Ondas que crescem aos poucos e um chefe no final.",
            waves = 15, prepTime = 15f, firstWaveSize = 3, enemiesPerWave = 2, spawnInterval = 0.8f,
            enemyHealthMultiplier = 1f, healthGrowthPerWave = 0.18f,
            enemySpeedMultiplier = 1f, speedGrowthPerWave = 0.03f, maxSpeedMultiplier = 1.5f,
            bossHealthMultiplier = 15f, bossEscortInterval = 8f, bossEscortCount = 2,
            killReward = 6, rewardGrowthPerWave = 0.06f, waveBonusBase = 10, waveBonusPerWave = 5, earlyCallBonusPerSecond = 1f,
            rewardMultiplier = 1f, scoreMultiplier = 1f },

        new DifficultyConfig {
            name = "Difícil", description = "Mais ondas, monstros resistentes e rápidos, chefe furioso.",
            waves = 20, prepTime = 12f, firstWaveSize = 4, enemiesPerWave = 2, spawnInterval = 0.6f,
            enemyHealthMultiplier = 1.3f, healthGrowthPerWave = 0.17f,
            enemySpeedMultiplier = 1.15f, speedGrowthPerWave = 0.035f, maxSpeedMultiplier = 1.8f,
            bossHealthMultiplier = 20f, bossEscortInterval = 6f, bossEscortCount = 3,
            killReward = 5, rewardGrowthPerWave = 0.05f, waveBonusBase = 5, waveBonusPerWave = 4, earlyCallBonusPerSecond = 1f,
            rewardMultiplier = 1f, scoreMultiplier = 1.5f },
    };

    // Use no jogo: GameSession.Current.waves, .enemyHealthMultiplier, etc.
    public static DifficultyConfig Current
    {
        get { return Difficulties[Mathf.Clamp(DifficultyIndex, 0, Difficulties.Length - 1)]; }
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
