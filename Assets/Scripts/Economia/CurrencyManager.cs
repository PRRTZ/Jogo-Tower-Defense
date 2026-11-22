using System;
using UnityEngine;

/// <summary>
/// Guarda as moedas do jogador. Coloque no GameManager de cada mapa.
/// Dá moedas sozinho quando um inimigo morre (escuta EnemyHealth.OnEnemyDied).
/// </summary>
public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }

    [Header("Moedas")]
    [Tooltip("Moedas no começo da fase. 0 = o player precisa lutar para comprar a primeira torre.")]
    [SerializeField] private int startingMoney = 0;

    [Tooltip("Moedas por inimigo morto quando ele não tem o componente EnemyReward.")]
    [SerializeField] private int defaultKillReward = 10;

    public int Money { get; private set; }

    /// <summary>Disparado sempre que as moedas mudam.</summary>
    public event Action<int> OnMoneyChanged;

    /// <summary>Disparado quando alguém tenta gastar sem saldo.</summary>
    public event Action OnPurchaseFailed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        Money = startingMoney;
    }

    private void OnEnable()  => EnemyHealth.OnEnemyDied += HandleEnemyDied;
    private void OnDisable() => EnemyHealth.OnEnemyDied -= HandleEnemyDied;

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        OnMoneyChanged?.Invoke(Money);
    }

    private void HandleEnemyDied(EnemyHealth enemy)
    {
        int reward = defaultKillReward;
        if (enemy != null && enemy.TryGetComponent(out EnemyReward custom))
            reward = custom.Reward;

        AddMoney(reward);
    }

    public bool CanAfford(int amount) => Money >= amount;

    public void AddMoney(int amount)
    {
        if (amount <= 0) return;
        Money += amount;
        OnMoneyChanged?.Invoke(Money);
    }

    /// <summary>Tenta gastar. Sem saldo, não gasta nada e retorna false.</summary>
    public bool TrySpend(int amount)
    {
        if (amount < 0) return false;

        if (Money < amount)
        {
            OnPurchaseFailed?.Invoke();
            return false;
        }

        Money -= amount;
        OnMoneyChanged?.Invoke(Money);
        return true;
    }
}
