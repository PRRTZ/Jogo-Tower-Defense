using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Forja: o player chega perto, aperta E e abre o menu de upgrades do próprio player.
///   [1] Dano  (+3 por nível no PlayerAttack)
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

    [Header("Interação")]
    [SerializeField] private float interactRadius = 2.5f;

    [Header("Dano")]
    [SerializeField] private float damagePerLevel = 3f;
    [SerializeField] private int[] damageCosts = { 50, 100, 200, 400 };   // um por nível

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
    TextMeshProUGUI damageLine, healthLine;

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
        if (player == null) { FindPlayer(); return; }

        Vector3 d = player.position - transform.position;
        d.y = 0f;
        playerNear = d.magnitude <= interactRadius;

        bool blocked = GameOverManager.IsGameOver || (health != null && health.IsDead);

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
        if (!Pay(damageCosts[damageLevel])) return;

        damageLevel++;
        attack.damage = baseDamage + damagePerLevel * damageLevel;
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

    static bool Pay(int cost)
    {
        var cm = CurrencyManager.Instance;
        return cm == null || cm.TrySpend(cost);
    }

    // ---------- tela ----------

    void BuildUI()
    {
        var canvas = UIFactory.CreateCanvas("UI_Forja", 20);
        var rt = UIFactory.CreatePanel(canvas.transform, "PainelForja", 560f, new Vector2(0.5f, 0.5f), new Vector2(0f, -40f));
        panel = rt.gameObject;

        var title = UIFactory.CreateText(rt, "FORJA", 36f);
        title.fontStyle = FontStyles.Bold;
        title.color = UIFactory.Gold;

        damageLine = UIFactory.CreateText(rt, "", 26f, TextAlignmentOptions.Left);
        healthLine = UIFactory.CreateText(rt, "", 26f, TextAlignmentOptions.Left);

        var hint = UIFactory.CreateText(rt, "[E] ou [Esc] fechar", 20f);
        hint.color = new Color(0.75f, 0.75f, 0.75f);
    }

    void Refresh()
    {
        if (panel == null || !panel.activeSelf) return;

        damageLine.text = Line("[1] Dano", damageLevel, damageCosts,
            attack != null ? attack.damage : 0f, damagePerLevel);
        healthLine.text = Line("[2] Vida", healthLevel, healthCosts,
            health != null ? health.maxHealth : 0f, healthPerLevel);
    }

    static string Line(string label, int level, int[] costs, float current, float perLevel)
    {
        string lv = "nv." + level + "/" + costs.Length;

        if (level >= costs.Length)
            return label + "   " + lv + "   " + current.ToString("0.#") + "   <color=#AAAAAA>máximo</color>";

        int cost = costs[level];
        var cm = CurrencyManager.Instance;
        bool canPay = cm == null || cm.CanAfford(cost);
        string costText = canPay ? "<color=#FFD640>$" + cost + "</color>" : "<color=#FF5555>$" + cost + "</color>";

        return label + "   " + lv + "   " + current.ToString("0.#")
            + " <color=#7CFC7C>» " + (current + perLevel).ToString("0.#") + "</color>   " + costText;
    }

    // aviso "[E] Forja" quando o player está perto (mesmo estilo do aviso das torres)
    void OnGUI()
    {
        if (!playerNear || IsOpen || GameOverManager.IsGameOver) return;

        var style = new GUIStyle(GUI.skin.label) { fontSize = 22, alignment = TextAnchor.MiddleCenter };
        style.normal.textColor = Color.white;
        GUI.Label(new Rect(0, Screen.height - 160, Screen.width, 40), "[E] Abrir forja", style);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.6f, 0.1f);
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}
