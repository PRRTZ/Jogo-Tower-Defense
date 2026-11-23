using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Interface de construção das torres (substitui o texto provisório do TowerBuilder).
///   Perto de um local vazio:  barra com as torres [1]..[4], nome, papel e preço (a escolhida fica destacada)
///   Perto de uma torre:       card com nivel, status atuais > proximo nivel, upgrade e venda
/// Coloque no GameManager (cria o próprio Canvas). Os sons são opcionais: arraste do "400 Sounds Pack/UI".
/// Só reconstrói os textos quando algo muda (local, torre escolhida, moedas ou nível).
/// </summary>
public class TowerBuildUI : MonoBehaviour
{
    [Header("Tamanho")]
    [Tooltip("Aumenta ou diminui toda a interface das torres (1 = normal).")]
    [SerializeField, Range(0.6f, 2f)] private float uiScale = 1.2f;

    [Header("Cores")]
    [SerializeField] private Color cardColor = new Color(0.06f, 0.07f, 0.10f, 0.96f);
    [SerializeField] private Color selectedColor = new Color(1f, 0.84f, 0.25f, 1f);
    [Tooltip("Fundo dos cards que você ainda não pode pagar (os textos continuam nítidos).")]
    [SerializeField] private Color lockedCardColor = new Color(0.22f, 0.07f, 0.07f, 0.96f);

    [Header("Sons (opcional)")]
    [SerializeField] private AudioClip selectSound;     // ex.: select_1
    [SerializeField] private AudioClip buildSound;      // ex.: pop_2
    [SerializeField] private AudioClip upgradeSound;    // ex.: synth_confirmation
    [SerializeField] private AudioClip sellSound;       // ex.: click_double_off
    [SerializeField] private AudioClip errorSound;      // ex.: synth_error
    [SerializeField, Range(0f, 1f)] private float volume = 0.7f;

    private class Card
    {
        public Image background;
        public Outline border;
        public TextMeshProUGUI name;
        public TextMeshProUGUI role;
        public TextMeshProUGUI price;
    }

    private const string Green = "#7CFC7C";
    private const string Gold = "#FFD640";
    private const string Red = "#FF5555";

    private readonly List<Card> cards = new List<Card>();
    private GameObject buildBar;
    private TextMeshProUGUI buildHint;
    private GameObject towerCard;
    private TextMeshProUGUI towerTitle, towerStats, towerActions;
    private AudioSource audioSource;

    // para só atualizar quando algo muda
    private TowerSpot lastSpot;
    private Object lastTower;
    private int lastSelected = -1, lastMoney = -1, lastLevel = -1;
    private bool lastVisible;

    private void OnEnable()
    {
        TowerBuilder.OnSelectionChanged += PlaySelect;
        TowerBuilder.OnBuilt += PlayBuild;
        TowerBuilder.OnUpgraded += PlayUpgrade;
        TowerBuilder.OnSold += PlaySell;
        TowerBuilder.OnActionFailed += PlayError;
    }

    private void OnDisable()
    {
        TowerBuilder.OnSelectionChanged -= PlaySelect;
        TowerBuilder.OnBuilt -= PlayBuild;
        TowerBuilder.OnUpgraded -= PlayUpgrade;
        TowerBuilder.OnSold -= PlaySell;
        TowerBuilder.OnActionFailed -= PlayError;
    }

    private void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        BuildUI();
        buildBar.SetActive(false);
        towerCard.SetActive(false);
    }

    // ---------- montagem da tela ----------

    private void BuildUI()
    {
        var canvas = UIFactory.CreateCanvas("UI_Torres", 8);
        var builder = TowerBuilder.Instance;

        // barra de construção (embaixo, no centro)
        buildBar = new GameObject("BarraConstrucao", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        var barRt = (RectTransform)buildBar.transform;
        barRt.SetParent(canvas.transform, false);
        barRt.anchorMin = barRt.anchorMax = barRt.pivot = new Vector2(0.5f, 0f);
        barRt.anchoredPosition = new Vector2(0f, 30f);
        barRt.localScale = Vector3.one * uiScale;
        var barLayout = buildBar.GetComponent<VerticalLayoutGroup>();
        barLayout.spacing = 12f;
        barLayout.childAlignment = TextAnchor.LowerCenter;
        barLayout.childControlWidth = barLayout.childControlHeight = true;
        barLayout.childForceExpandWidth = barLayout.childForceExpandHeight = false;
        var barFit = buildBar.GetComponent<ContentSizeFitter>();
        barFit.horizontalFit = barFit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var row = new GameObject("Cards", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        row.transform.SetParent(buildBar.transform, false);
        var rowLayout = row.GetComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 12f;
        rowLayout.childControlWidth = rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = rowLayout.childForceExpandHeight = false;

        if (builder != null && builder.towerPrefabs != null)
            for (int i = 0; i < builder.towerPrefabs.Length && i < 9; i++)
                cards.Add(CreateCard(row.transform, i, builder.towerPrefabs[i]));

        buildHint = UIFactory.CreateText(buildBar.transform, "", 24f);
        buildHint.fontStyle = FontStyles.Bold;
        buildHint.enableAutoSizing = true;
        buildHint.fontSizeMin = 16f;
        buildHint.fontSizeMax = 24f;
        buildHint.rectTransform.sizeDelta = new Vector2(1400f, 40f);
        Outline(buildHint);

        // card da torre construída
        var cardRt = UIFactory.CreatePanel(canvas.transform, "CardTorre", 560f, new Vector2(0.5f, 0f), new Vector2(0f, 30f));
        cardRt.GetComponent<Image>().color = cardColor;
        cardRt.localScale = Vector3.one * uiScale;
        var cardLayout = cardRt.GetComponent<VerticalLayoutGroup>();
        cardLayout.padding = new RectOffset(16, 16, 12, 14);
        cardLayout.spacing = 10f;
        towerCard = cardRt.gameObject;
        towerTitle = UIFactory.CreateText(cardRt, "", 26f);
        towerTitle.fontStyle = FontStyles.Bold;
        towerTitle.enableAutoSizing = true;
        towerTitle.fontSizeMin = 18f;
        towerTitle.fontSizeMax = 26f;
        Outline(towerTitle);
        towerStats = UIFactory.CreateText(cardRt, "", 16f, TextAlignmentOptions.Left);
        towerStats.enableAutoSizing = true;
        towerStats.fontSizeMin = 12f;
        towerStats.fontSizeMax = 16f;
        towerStats.lineSpacing = 2f;
        Outline(towerStats);
        towerActions = UIFactory.CreateText(cardRt, "", 16f);
        towerActions.fontStyle = FontStyles.Bold;
        towerActions.enableAutoSizing = true;
        towerActions.fontSizeMin = 12f;
        towerActions.fontSizeMax = 16f;
        Outline(towerActions);
    }

    private Card CreateCard(Transform parent, int index, GameObject prefab)
    {
        var go = new GameObject("Card_" + (index + 1), typeof(RectTransform), typeof(Image), typeof(Outline),
                                typeof(VerticalLayoutGroup), typeof(LayoutElement));
        go.transform.SetParent(parent, false);

        var bg = go.GetComponent<Image>();
        bg.color = cardColor;

        var border = go.GetComponent<Outline>();
        border.effectColor = selectedColor;
        border.effectDistance = new Vector2(5f, -5f);
        border.enabled = false;

        var layout = go.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 8, 10);
        layout.spacing = 4f;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        var le = go.GetComponent<LayoutElement>();
        le.preferredWidth = 240f;

        var key = UIFactory.CreateText(go.transform, "[" + (index + 1) + "]", 16f, TextAlignmentOptions.Left);
        key.color = selectedColor;
        key.fontStyle = FontStyles.Bold;
        Outline(key);

        var name = UIFactory.CreateText(go.transform, TowerBuilder.DisplayName(prefab), 22f);
        name.fontStyle = FontStyles.Bold;
        name.enableAutoSizing = true;
        name.fontSizeMin = 14f;
        name.fontSizeMax = 22f;
        Outline(name);

        var role = UIFactory.CreateText(go.transform, RoleOf(prefab), 16f);
        role.enableAutoSizing = true;
        role.fontSizeMin = 12f;
        role.fontSizeMax = 16f;
        role.color = new Color(0.9f, 0.92f, 1f);
        Outline(role);

        // preço numa faixa escura própria
        var badge = new GameObject("Preco", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        badge.transform.SetParent(go.transform, false);
        badge.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);
        badge.GetComponent<LayoutElement>().preferredHeight = 38f;

        var price = UIFactory.CreateText(badge.transform, "", 22f);
        price.fontStyle = FontStyles.Bold;
        price.enableAutoSizing = true;
        price.fontSizeMin = 16f;
        price.fontSizeMax = 22f;
        Outline(price);
        var prt = price.rectTransform;
        prt.anchorMin = Vector2.zero;
        prt.anchorMax = Vector2.one;
        prt.offsetMin = prt.offsetMax = Vector2.zero;

        return new Card { background = bg, border = border, name = name, role = role, price = price };
    }

    // descrição curta a partir dos valores do componente Tower do prefab
    private static string RoleOf(GameObject prefab)
    {
        var t = prefab != null ? prefab.GetComponent<Tower>() : null;
        if (t == null) return "";
        if (t.arcHeight > 0f) return "area - em arco";
        if (t.splashRadius > 0f) return "dano em area";
        if (t.cooldown <= 0.5f) return "tiro rapido";
        if (t.range >= 9f) return "longo alcance";
        return "tiro preciso";
    }

    private static void Outline(TextMeshProUGUI t)
    {
        t.outlineWidth = 0.25f;
        t.outlineColor = Color.black;
    }

    // ---------- atualização ----------

    private void Update()
    {
        var builder = TowerBuilder.Instance;
        if (builder == null || buildBar == null) return;

        bool blocked = TowerBuilder.InputBlocked || GameOverManager.IsGameOver;
        TowerSpot spot = blocked ? null : builder.NearestSpot;
        GameObject tower = spot != null ? spot.tower : null;
        TowerLevel level = tower != null ? tower.GetComponent<TowerLevel>() : null;

        int money = CurrencyManager.Instance != null ? CurrencyManager.Instance.Money : int.MaxValue;
        int lv = level != null ? level.Level : -1;
        bool visible = spot != null;

        // nada mudou: não refaz os textos
        if (visible == lastVisible && spot == lastSpot && tower == lastTower &&
            builder.SelectedIndex == lastSelected && money == lastMoney && lv == lastLevel)
            return;

        lastVisible = visible; lastSpot = spot; lastTower = tower;
        lastSelected = builder.SelectedIndex; lastMoney = money; lastLevel = lv;

        buildBar.SetActive(visible && tower == null);
        towerCard.SetActive(visible && tower != null);

        if (visible && tower == null) RefreshBuildBar(builder, money);
        else if (visible) RefreshTowerCard(builder, tower, level, money);
    }

    private void RefreshBuildBar(TowerBuilder builder, int money)
    {
        for (int i = 0; i < cards.Count; i++)
        {
            int cost = builder.GetPrice(i).buildCost;
            bool canPay = money >= cost;
            bool isSelected = i == builder.SelectedIndex;

            cards[i].border.enabled = isSelected;
            cards[i].background.color = canPay ? cardColor : lockedCardColor;
            cards[i].price.text = "<color=" + (canPay ? Gold : "#FF6B6B") + ">$ " + cost + "</color>";
        }

        int sel = builder.SelectedIndex;
        if (sel < 0 || sel >= builder.towerPrefabs.Length) { buildHint.text = ""; return; }

        string name = UIFactory.RemoveDiacritics(TowerBuilder.DisplayName(builder.towerPrefabs[sel]));
        int selCost = builder.GetPrice(sel).buildCost;
        buildHint.text = money >= selCost
            ? "[E] Construir " + name
            : "<color=" + Red + ">Faltam $" + (selCost - money) + " para " + name + "</color>";
    }

    private void RefreshTowerCard(TowerBuilder builder, GameObject towerObject, TowerLevel level, int money)
    {
        string name = UIFactory.RemoveDiacritics(TowerBuilder.DisplayName(towerObject));
        var tower = towerObject.GetComponent<Tower>();

        if (level == null || tower == null)
        {
            towerTitle.text = name;
            towerStats.text = "";
            towerActions.text = "[E] Remover";
            return;
        }

        towerTitle.text = name + "   <size=70%><color=" + Gold + ">Nv. " + level.Level + "/" + level.MaxLevel + "</color></size>";

        var up = builder.upgrade;
        bool max = level.IsMaxLevel;
        float shots = 1f / Mathf.Max(0.01f, tower.cooldown);

        string stats =
            Stat("Dano", tower.damage, tower.damage * up.damageMultiplier, max, "0") + "\n" +
            Stat("Tiros/s", shots, 1f / Mathf.Max(0.01f, tower.cooldown * up.cooldownMultiplier), max, "0.0") + "\n" +
            Stat("Alcance", tower.range, tower.range + up.rangeBonus, max, "0.#");
        if (tower.splashRadius > 0f) stats += "\nArea: " + tower.splashRadius.ToString("0.#");
        towerStats.text = stats;

        string upgradeText;
        if (max) upgradeText = "<color=#AAAAAA>Nivel maximo</color>";
        else
        {
            int cost = level.NextUpgradeCost;
            upgradeText = "[U] Upgrade <color=" + (money >= cost ? Gold : Red) + ">$" + cost + "</color>";
        }
        towerActions.text = upgradeText + "      [E] Vender <color=" + Gold + ">+$" + level.SellValue + "</color>";
    }

    private static string Stat(string label, float current, float next, bool max, string format)
    {
        string line = label + ":  " + current.ToString(format);
        if (!max) line += "  <color=" + Green + ">> " + next.ToString(format) + "</color>";
        return line;
    }

    // ---------- sons ----------

    private void Play(AudioClip clip)
    {
        if (clip != null && audioSource != null) audioSource.PlayOneShot(clip, volume);
    }

    private void PlaySelect()  => Play(selectSound);
    private void PlayBuild()   => Play(buildSound);
    private void PlayUpgrade() => Play(upgradeSound);
    private void PlaySell()    => Play(sellSound);
    private void PlayError()   => Play(errorSound);
}
