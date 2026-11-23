using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class TowerBuilder : MonoBehaviour
{
    public static TowerBuilder Instance { get; private set; }

    // avisos para a interface (sons, animações)
    public static event Action OnSelectionChanged;
    public static event Action OnBuilt;
    public static event Action OnUpgraded;
    public static event Action OnSold;
    public static event Action OnActionFailed;            // faltou moeda ou já está no nível máximo

    [Header("Torres disponíveis (arraste os prefabs Tower_*)")]
    public GameObject[] towerPrefabs;

    [Header("Preços (mesma ordem dos prefabs; preenchido sozinho pelo nome)")]
    public TowerPrice[] prices;

    [Header("Upgrade e venda")]
    public TowerUpgradeSettings upgrade = new TowerUpgradeSettings();
    [Range(0f, 1f)] public float sellRefund = 0.5f;      // 0.5 = devolve 50% do investido

    [Header("Interação")]
    public float interactRadius = 2.5f;     // distância do player ao local para poder usar E

    /// <summary>Outros menus (forja, game over) ligam isto para o TowerBuilder ignorar as teclas.</summary>
    public static bool InputBlocked;

    // Versão do balanceamento: quando muda, os preços e upgrades salvos nas cenas
    // voltam para os valores padrão novos (uma única vez). Depois dá para ajustar no Inspector normalmente.
    const int CurrentBalance = 2;
    [SerializeField, HideInInspector] int balanceVersion;

    // ---------- lido pela interface (TowerBuildUI) ----------
    public TowerSpot NearestSpot => nearest;
    public int SelectedIndex => selected;
    public TowerPrice GetPrice(int index) => PriceOf(index);

    int selected;                            // torre escolhida (teclas 1..9)
    TowerSpot[] spots;
    Transform player;
    Health playerHealth;
    Animator playerAnimator;
    TowerSpot nearest;

    void Awake()
    {
        Instance = this;
        InputBlocked = false;
        ApplyBalanceUpdate();
        EnsurePrices();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void OnValidate()
    {
        ApplyBalanceUpdate();
        EnsurePrices();
    }

    void ApplyBalanceUpdate()
    {
        if (balanceVersion >= CurrentBalance) return;
        ResetPricesAndUpgrades();
        balanceVersion = CurrentBalance;
    }

    /// <summary>Volta preços e upgrades para os valores padrão (também no menu ⋮ do componente).</summary>
    [ContextMenu("Usar preços e upgrades padrão")]
    public void ResetPricesAndUpgrades()
    {
        upgrade = new TowerUpgradeSettings();
        if (towerPrefabs == null) return;
        prices = new TowerPrice[towerPrefabs.Length];
        for (int i = 0; i < prices.Length; i++) prices[i] = DefaultPrice(towerPrefabs[i]);
    }

    void Start()
    {
        spots = FindObjectsByType<TowerSpot>(FindObjectsSortMode.None);
        FindPlayer();
    }

    void FindPlayer()
    {
        var pc = FindAnyObjectByType<PlayerController>();
        if (pc == null) return;
        player = pc.transform;
        playerHealth = pc.GetComponent<Health>();
        playerAnimator = pc.GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (player == null) { FindPlayer(); return; }
        if (playerHealth != null && playerHealth.IsDead) { nearest = null; return; }

        nearest = FindNearestSpot();

        if (InputBlocked) return;
        var kb = Keyboard.current;
        if (kb == null) return;

        // teclas 1..9 escolhem qual torre construir
        for (int i = 0; i < towerPrefabs.Length && i < 9; i++)
        {
            if (kb[(Key)((int)Key.Digit1 + i)].wasPressedThisFrame && selected != i)
            {
                selected = i;
                OnSelectionChanged?.Invoke();
            }
        }

        if (nearest == null) return;

        // E: constrói (pagando) ou vende (devolvendo parte do valor)
        if (kb.eKey.wasPressedThisFrame)
        {
            if (nearest.IsEmpty) Build(nearest);
            else Sell(nearest);
        }

        // U: upgrade da torre construída
        if (kb.uKey.wasPressedThisFrame && !nearest.IsEmpty)
            Upgrade(nearest);
    }

    TowerSpot FindNearestSpot()
    {
        TowerSpot best = null;
        float bestDist = interactRadius;
        foreach (var s in spots)
        {
            if (s == null) continue;
            Vector3 d = s.transform.position - player.position;
            d.y = 0f;
            float dist = d.magnitude;
            if (dist <= bestDist) { best = s; bestDist = dist; }
        }
        return best;
    }

    void Build(TowerSpot spot)
    {
        if (towerPrefabs == null || towerPrefabs.Length == 0 || towerPrefabs[selected] == null) return;

        TowerPrice price = PriceOf(selected);
        var cm = CurrencyManager.Instance;
        if (cm != null && !cm.TrySpend(price.buildCost))   // sem moedas: não constrói
        {
            OnActionFailed?.Invoke();
            return;
        }

        spot.tower = Instantiate(towerPrefabs[selected], spot.BuildPosition, spot.transform.rotation);

        var level = spot.tower.GetComponent<TowerLevel>();
        if (level == null) level = spot.tower.AddComponent<TowerLevel>();
        level.Init(price, upgrade, sellRefund);

        PlayInteractAnimation();
        SoundManager.Play(Sfx.PlaceTower);
        OnBuilt?.Invoke();
    }

    void Sell(TowerSpot spot)
    {
        var level = spot.tower.GetComponent<TowerLevel>();
        if (level != null && CurrencyManager.Instance != null)
            CurrencyManager.Instance.AddMoney(level.SellValue);

        Destroy(spot.tower);
        spot.tower = null;
        PlayInteractAnimation();
        SoundManager.Play(Sfx.RemoveTower);
        OnSold?.Invoke();
    }

    void Upgrade(TowerSpot spot)
    {
        var level = spot.tower.GetComponent<TowerLevel>();
        if (level != null && level.TryUpgrade())
        {
            PlayInteractAnimation();
            SoundManager.Play(Sfx.UpgradeTower);
            OnUpgraded?.Invoke();
        }
        else OnActionFailed?.Invoke();
    }

    void PlayInteractAnimation()
    {
        if (playerAnimator != null) playerAnimator.SetTrigger("Interact");
    }

    // ---------- preços ----------

    TowerPrice PriceOf(int index)
    {
        EnsurePrices();
        return prices[index] ?? DefaultPrice(towerPrefabs[index]);
    }

    // deixa a lista de preços do mesmo tamanho da lista de torres, com valores padrão pelo nome
    void EnsurePrices()
    {
        if (towerPrefabs == null) return;
        if (prices == null) prices = new TowerPrice[0];

        if (prices.Length != towerPrefabs.Length)
        {
            var resized = new TowerPrice[towerPrefabs.Length];
            for (int i = 0; i < resized.Length; i++)
                resized[i] = i < prices.Length && prices[i] != null ? prices[i] : DefaultPrice(towerPrefabs[i]);
            prices = resized;
        }
    }

    static TowerPrice DefaultPrice(GameObject prefab)
    {
        string n = prefab != null ? prefab.name.ToLowerInvariant() : "";
        if (n.Contains("turret"))   return new TowerPrice { buildCost = 90,  upgradeCosts = new[] { 120, 240 } };
        if (n.Contains("ballista")) return new TowerPrice { buildCost = 120, upgradeCosts = new[] { 160, 320 } };
        if (n.Contains("cannon"))   return new TowerPrice { buildCost = 170, upgradeCosts = new[] { 220, 440 } };
        if (n.Contains("catapult")) return new TowerPrice { buildCost = 220, upgradeCosts = new[] { 280, 560 } };
        return new TowerPrice { buildCost = 120, upgradeCosts = new[] { 160, 320 } };
    }

    public static string DisplayName(GameObject prefab)
    {
        return prefab == null ? "?" : prefab.name.Replace("(Clone)", "").Replace("Tower_", "").Trim();
    }
}
