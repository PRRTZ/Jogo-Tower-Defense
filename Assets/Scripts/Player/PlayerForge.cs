using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Forja: o player chega perto, aperta E e abre o menu de upgrades do próprio player.
///   [1] Dano  (+50% do dano base por nível no PlayerAttack: 14 -> 21 -> 28 -> 35 -> 42)
///       cada nível só libera a partir de uma onda (Damage Unlock Wave), para não completar a forja logo no começo
///   [2] Vida  (+20 = 1 coração por nível no Health, e cura tudo na compra)
///   [E] / [Esc] fecha
/// O menu fecha sozinho se o player se afastar ou levar dano. O jogo NÃO pausa.
///
/// Coloque num objeto da cena (um modelo de bigorna, uma casa...), longe dos TowerSpots,
/// para o E da forja não se misturar com o E das torres.
/// </summary>
public class PlayerForge : MonoBehaviour
{
    public static bool IsOpen { get; private set; }
    /// <summary>Frame em que a forja fechou (o Esc que fecha a forja não abre a pausa).</summary>
    public static int ClosedFrame { get; private set; } = -1;

    [Header("Interação")]
    [SerializeField] private float interactRadius = 2.5f;

    [Header("Dano")]
    [Tooltip("Dano a mais por nível, em fração do dano base (0.5 = +50%).")]
    [SerializeField] private float damageBonusPerLevel = 0.5f;
    [SerializeField] private int[] damageCosts = { 40, 80, 130, 200 };    // um por nível
    [Tooltip("Onda a partir da qual cada nível de dano pode ser comprado (mesma ordem dos custos).")]
    [SerializeField] private int[] damageUnlockWave = { 3, 7, 10, 13 };

    [Header("Vida")]
    [SerializeField] private float healthPerLevel = 20f;                  // 20 = 1 coração
    [SerializeField] private int[] healthCosts = { 50, 100, 200, 400 };
    [SerializeField] private bool fullHealOnHealthUpgrade = true;

    // níveis comprados (compartilhados entre forjas, caso tenha mais de uma no mapa)
    static int damageLevel, healthLevel;
    static float baseDamage = -1f, baseMaxHealth = -1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { IsOpen = false; damageLevel = 0; healthLevel = 0; baseDamage = -1f; baseMaxHealth = -1f; }

    Transform player;
    PlayerAttack attack;
    Health health;
    bool playerNear;
    float lastHealth;

    GameObject panel;
    TextMeshProUGUI damageLine, healthLine, forgeHint;

    void Awake()
    {
        // nova fase: zera os upgrades (cada mapa começa do zero)
        ResetStatics();
    }

    void Start()
    {
        FindPlayer();
        BuildUI();
        SetOpen(false);
    }

    void FindPlayer()
    {
        var pc = FindAnyObjectByType<PlayerController>();
        if (pc == null) return;
        player = pc.transform;
        attack = pc.GetComponent<PlayerAttack>();
        health = pc.GetComponent<Health>();

        if (attack != null && baseDamage < 0f) baseDamage = attack.damage;
        if (health != null && baseMaxHealth < 0f) baseMaxHealth = health.maxHealth;
    }

    void Update()
    {
        if (PauseSettingsMenu.IsOpen) return;
        if (player == null) { FindPlayer(); return; }

        Vector3 d = player.position - transform.position;
        d.y = 0f;
        playerNear = d.magnitude <= interactRadius;

        bool blocked = GameOverManager.IsGameOver || (health != null && health.IsDead);
        bool showHint = playerNear && !panel.activeSelf && !blocked;
        if (forgeHint.gameObject.activeSelf != showHint) forgeHint.gameObject.SetActive(showHint);

        if (panel.activeSelf)
        {
            // fecha sozinho: longe demais, levou dano ou o jogo acabou
            bool tookDamage = health != null && health.CurrentHealth < lastHealth;
            if (!playerNear || tookDamage || blocked) { SetOpen(false); return; }
            if (health != null) lastHealth = health.CurrentHealth;
        }

        var kb = Keyboard.current;
        if (kb == null || blocked) return;

        if (!panel.activeSelf)
        {
            if (playerNear && kb.eKey.wasPressedThisFrame) SetOpen(true);
            return;
        }

        if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) BuyDamage();
        if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) BuyHealth();
        if (kb.eKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame) SetOpen(false);
    }

    void SetOpen(bool open)
    {
        if (panel == null) return;
        if (IsOpen && !open) ClosedFrame = Time.frameCount;
        panel.SetActive(open);
        IsOpen = open;
        if (!GameOverManager.IsGameOver) TowerBuilder.InputBlocked = open;   // teclas 1, 2 e E não vão para as torres
        if (open)
        {
            if (health != null) lastHealth = health.CurrentHealth;
            Refresh();
        }
    }

    // ---------- compras ----------

    void BuyDamage()
    {
        if (attack == null || damageLevel >= damageCosts.Length) return;
        if (!DamageUnlocked(damageLevel)) return;
        if (!Pay(damageCosts[damageLevel])) return;

        damageLevel++;
        attack.damage = baseDamage * (1f + damageBonusPerLevel * damageLevel);
        Refresh();
    }

    void BuyHealth()
    {
        if (health == null || healthLevel >= healthCosts.Length) return;
        if (!Pay(healthCosts[healthLevel])) return;

        healthLevel++;
        health.maxHealth = baseMaxHealth + healthPerLevel * healthLevel;
        if (fullHealOnHealthUpgrade) health.Heal(health.maxHealth);
        lastHealth = health.CurrentHealth;
        Refresh();
    }

    // nível "level" (0 = primeiro) já pode ser comprado? libera na preparação antes da onda indicada
    bool DamageUnlocked(int level)
    {
        if (damageUnlockWave == null || level >= damageUnlockWave.Length) return true;
        int unlock = damageUnlockWave[level];
        if (unlock <= 1) return true;
        var sp = EnemySpawner.Instance;
        if (sp == null) return true;
        int nextWave = sp.CurrentPhase == EnemySpawner.Phase.Preparing ? sp.CurrentWave + 1 : sp.CurrentWave;
        return nextWave >= unlock;
    }

    int DamageUnlockWave(int level) =>
        damageUnlockWave != null && level < damageUnlockWave.Length ? damageUnlockWave[level] : 0;

    static bool Pay(int cost)
    {
        var cm = CurrencyManager.Instance;
        return cm == null || cm.TrySpend(cost);
    }

    // ---------- tela ----------

    void BuildUI()
    {
        var canvas = UIFactory.CreateCanvas("UI_Forja", 20);
        forgeHint = UIFactory.CreateText(canvas.transform, "[E] Abrir forja", 16f);
        forgeHint.outlineWidth = 0.2f;
        forgeHint.outlineColor = Color.black;
        var hintRt = forgeHint.rectTransform;
        hintRt.anchorMin = hintRt.anchorMax = hintRt.pivot = new Vector2(0.5f, 0f);
        hintRt.anchoredPosition = new Vector2(0f, 160f);
        hintRt.sizeDelta = new Vector2(1000f, 40f);
        forgeHint.gameObject.SetActive(false);

        var rt = UIFactory.CreatePanel(canvas.transform, "PainelForja", 560f, new Vector2(0.5f, 0.5f), new Vector2(0f, -40f));
        panel = rt.gameObject;

        var title = UIFactory.CreateText(rt, "FORJA", 26f);
        title.fontStyle = FontStyles.Bold;
        title.color = UIFactory.Gold;

        damageLine = UIFactory.CreateText(rt, "", 16f, TextAlignmentOptions.Left);
        healthLine = UIFactory.CreateText(rt, "", 16f, TextAlignmentOptions.Left);

        var hint = UIFactory.CreateText(rt, "[E] ou [Esc] fechar", 14f);
        hint.color = new Color(0.75f, 0.75f, 0.75f);
    }

    void Refresh()
    {
        if (panel == null || !panel.activeSelf) return;

        // mostra o dano REAL do golpe agora (com o crescimento da onda atual)
        float dmgNow = attack != null ? attack.CurrentDamage : 0f;
        float dmgStep = attack != null && attack.damage > 0f
            ? dmgNow * (baseDamage * damageBonusPerLevel) / attack.damage : 0f;
        damageLine.text = Line("[1] Dano", damageLevel, damageCosts, dmgNow, dmgStep);
        if (damageLevel < damageCosts.Length && !DamageUnlocked(damageLevel))
            damageLine.text = "[1] Dano   nv." + damageLevel + "/" + damageCosts.Length + "   " + dmgNow.ToString("0.#")
                + "   <color=#AAAAAA>libera na onda " + DamageUnlockWave(damageLevel) + "</color>";
        healthLine.text = Line("[2] Vida", healthLevel, healthCosts,
            health != null ? health.maxHealth : 0f, healthPerLevel);
    }

    static string Line(string label, int level, int[] costs, float current, float perLevel)
    {
        string lv = "nv." + level + "/" + costs.Length;

        if (level >= costs.Length)
            return label + "   " + lv + "   " + current.ToString("0.#") + "   <color=#AAAAAA>maximo</color>";

        int cost = costs[level];
        var cm = CurrencyManager.Instance;
        bool canPay = cm == null || cm.CanAfford(cost);
        string costText = canPay ? "<color=#FFD640>$" + cost + "</color>" : "<color=#FF5555>$" + cost + "</color>";

        return label + "   " + lv + "   " + current.ToString("0.#")
            + " <color=#7CFC7C>> " + (current + perLevel).ToString("0.#") + "</color>   " + costText;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.6f, 0.1f);
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}
