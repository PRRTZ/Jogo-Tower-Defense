using System;
using UnityEngine;

/// <summary>Preço de um tipo de torre (configurado no TowerBuilder, na mesma ordem dos prefabs).</summary>
[Serializable]
public class TowerPrice
{
    [Min(0)] public int buildCost = 100;

    [Tooltip("Custo de cada upgrade: o 1º vai do nível 1 ao 2, o 2º do 2 ao 3...")]
    public int[] upgradeCosts = { 80, 160 };
}

/// <summary>Quanto cada nível de upgrade melhora a torre (igual para todas, configurado no TowerBuilder).</summary>
[Serializable]
public class TowerUpgradeSettings
{
    [Tooltip("Dano multiplicado a cada nível (1.35 = +35%).")]
    [Min(1f)] public float damageMultiplier = 1.35f;

    [Tooltip("Intervalo entre tiros multiplicado a cada nível (0.88 = 12% mais rápido).")]
    [Range(0.1f, 1f)] public float cooldownMultiplier = 0.88f;

    [Tooltip("Alcance somado a cada nível.")]
    [Min(0f)] public float rangeBonus = 0.5f;

    [Tooltip("Quanto a torre cresce a cada nível (0.08 = 8%). 0 = não muda o tamanho.")]
    [Min(0f)] public float scalePerLevel = 0.08f;
}

/// <summary>
/// Nível da torre. É adicionado SOZINHO pelo TowerBuilder ao construir (não precisa colocar no prefab).
/// Guarda os valores originais do componente Tower e aplica os multiplicadores de cada nível,
/// então cada torre mantém o seu estilo (Catapult continua lenta e forte, Turret rápida...).
/// </summary>
[DisallowMultipleComponent]
public class TowerLevel : MonoBehaviour
{
    public int Level { get; private set; } = 1;          // começa no nível 1
    public int MaxLevel => 1 + (price?.upgradeCosts?.Length ?? 0);
    public bool IsMaxLevel => Level >= MaxLevel;
    public int TotalInvested { get; private set; }

    public int NextUpgradeCost => IsMaxLevel ? 0 : price.upgradeCosts[Level - 1];
    public int SellValue => Mathf.FloorToInt(TotalInvested * sellRefund);

    TowerPrice price;
    TowerUpgradeSettings settings;
    float sellRefund;

    Tower tower;
    float baseDamage, baseCooldown, baseRange;
    Vector3 baseScale;

    /// <summary>Chamado pelo TowerBuilder logo depois de construir.</summary>
    public void Init(TowerPrice towerPrice, TowerUpgradeSettings upgradeSettings, float refund)
    {
        price = towerPrice ?? new TowerPrice { buildCost = 0, upgradeCosts = new int[0] };
        settings = upgradeSettings ?? new TowerUpgradeSettings();
        sellRefund = Mathf.Clamp01(refund);

        tower = GetComponent<Tower>();
        if (tower != null)
        {
            // multiplicador global de dano das torres (vem da dificuldade, pelo EnemySpawner)
            float mult = EnemySpawner.Instance != null ? EnemySpawner.Instance.towerDamageMultiplier : 1f;
            baseDamage = tower.damage * mult;
            baseCooldown = tower.cooldown;
            baseRange = tower.range;
        }
        baseScale = transform.localScale;

        Level = 1;
        TotalInvested = price.buildCost;
        Apply();
    }

    /// <summary>Sobe um nível pagando com moedas. Retorna true se conseguiu.</summary>
    public bool TryUpgrade()
    {
        if (IsMaxLevel) return false;

        int cost = NextUpgradeCost;
        var cm = CurrencyManager.Instance;
        if (cm != null && !cm.TrySpend(cost)) return false;

        Level++;
        TotalInvested += cost;
        Apply();
        return true;
    }

    void Apply()
    {
        int steps = Level - 1;

        if (tower != null)
        {
            tower.damage = baseDamage * Mathf.Pow(settings.damageMultiplier, steps);
            tower.cooldown = baseCooldown * Mathf.Pow(settings.cooldownMultiplier, steps);
            tower.range = baseRange + settings.rangeBonus * steps;
        }

        transform.localScale = baseScale * (1f + settings.scalePerLevel * steps);
    }
}
