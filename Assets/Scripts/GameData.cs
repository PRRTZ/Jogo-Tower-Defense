using System;
using System.Collections.Generic;
using UnityEngine;

// Configuração de cada nível de dificuldade (ajuste os números à vontade)
[Serializable]
public class DifficultyConfig
{
    public string name;
    public string description;
    public int totalEnemies;            // inimigos a eliminar (o mais fácil precisa de >= 5)
    public int waves;                   // quantidade de ondas
    public float enemyHealthMultiplier; // multiplica a vida dos inimigos
    public float enemySpeedMultiplier;  // multiplica a velocidade dos inimigos
    public float spawnInterval;         // segundos entre um inimigo e o próximo
    public float missionTimeSeconds;    // tempo máximo da missão
    public int baseMaxHealth;           // vida da base
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
            name = "Fácil", description = "Para relaxar e conhecer as torres.",
            totalEnemies = 10, waves = 2, enemyHealthMultiplier = 1f, enemySpeedMultiplier = 1f,
            spawnInterval = 3f, missionTimeSeconds = 300f, baseMaxHealth = 10 },

        new DifficultyConfig {
            name = "Médio", description = "Mais inimigos, mais resistentes e mais rápidos.",
            totalEnemies = 20, waves = 3, enemyHealthMultiplier = 1.5f, enemySpeedMultiplier = 1.15f,
            spawnInterval = 2.2f, missionTimeSeconds = 420f, baseMaxHealth = 8 },

        new DifficultyConfig {
            name = "Difícil", description = "Uma enxurrada de bichinhos. Boa sorte!",
            totalEnemies = 35, waves = 5, enemyHealthMultiplier = 2f, enemySpeedMultiplier = 1.3f,
            spawnInterval = 1.5f, missionTimeSeconds = 480f, baseMaxHealth = 5 },
    };

    // Use no jogo: GameSession.Current.totalEnemies, .missionTimeSeconds, etc.
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

    // Sugestão de pontuação: inimigos derrotados + vida da base + tempo que sobrou
    public static int ComputeScore(int enemiesKilled, int baseHealthLeft, float secondsLeft)
    {
        float multiplier = 1f + GameSession.DifficultyIndex * 0.5f;   // difícil vale mais
        return Mathf.RoundToInt((enemiesKilled * 100 + baseHealthLeft * 50 + Mathf.Max(0f, secondsLeft) * 2f) * multiplier);
    }
}