using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD das ondas, no topo da tela:
///   "ONDA 3 / 15"
///   "Inimigos restantes: 4"   ou   "Proxima onda em 8s - [N] Chamar agora (+15)"
///   "Pontos: 1234 - Normal"
/// Barra de vida do CHEFE (aparece só na última onda).
/// Avisos no centro: "ONDA 4!", "Onda concluida!", "O CHEFE CHEGOU!", "FURIA!", "CHEFE DERROTADO!".
/// Coloque no GameManager (cria o próprio Canvas).
/// </summary>
public class WaveHUD : MonoBehaviour
{
    [SerializeField] private Color titleColor = Color.white;
    [SerializeField] private Color bannerColor = new Color(1f, 0.84f, 0.25f, 1f);
    [SerializeField] private Color bossColor = new Color(0.9f, 0.2f, 0.2f, 1f);
    [SerializeField] private float bannerDuration = 1.8f;

    private TextMeshProUGUI waveText;
    private TextMeshProUGUI infoText;
    private TextMeshProUGUI scoreText;
    private TextMeshProUGUI bannerText;
    private Coroutine bannerRoutine;

    private GameObject bossBar;
    private TextMeshProUGUI bossNameText;
    private Image bossFill;
    private float bossShown = 1f;

    private void OnEnable()
    {
        EnemySpawner.OnWaveStarted += HandleWaveStarted;
        EnemySpawner.OnWaveCleared += HandleWaveCleared;
        EnemySpawner.OnBossSpawned += HandleBossSpawned;
        EnemySpawner.OnBossRage += HandleBossRage;
        EnemySpawner.OnBossDefeated += HandleBossDefeated;
    }

    private void OnDisable()
    {
        EnemySpawner.OnWaveStarted -= HandleWaveStarted;
        EnemySpawner.OnWaveCleared -= HandleWaveCleared;
        EnemySpawner.OnBossSpawned -= HandleBossSpawned;
        EnemySpawner.OnBossRage -= HandleBossRage;
        EnemySpawner.OnBossDefeated -= HandleBossDefeated;
    }

    private void Start()
    {
        BuildUI();
    }

    private void BuildUI()
    {
        var canvas = UIFactory.CreateCanvas("UI_Ondas", 6);

        waveText = UIFactory.CreateText(canvas.transform, "", 34f);
        waveText.enableAutoSizing = true;
        waveText.fontSizeMin = 24f;
        waveText.fontSizeMax = 34f;
        waveText.fontStyle = FontStyles.Bold;
        waveText.color = titleColor;
        Outline(waveText);
        Place(waveText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(1100f, 50f));

        infoText = UIFactory.CreateText(canvas.transform, "", 16f);
        infoText.enableAutoSizing = true;
        infoText.fontSizeMin = 12f;
        infoText.fontSizeMax = 16f;
        Outline(infoText);
        Place(infoText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -68f), new Vector2(1500f, 34f));

        scoreText = UIFactory.CreateText(canvas.transform, "", 18f);
        scoreText.enableAutoSizing = true;
        scoreText.fontSizeMin = 14f;
        scoreText.fontSizeMax = 18f;
        scoreText.color = new Color(1f, 0.92f, 0.6f);
        Outline(scoreText);
        Place(scoreText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -102f), new Vector2(1200f, 32f));

        BuildBossBar(canvas.transform);

        bannerText = UIFactory.CreateText(canvas.transform, "", 48f);
        bannerText.enableAutoSizing = true;
        bannerText.fontSizeMin = 32f;
        bannerText.fontSizeMax = 48f;
        bannerText.fontStyle = FontStyles.Bold;
        bannerText.color = bannerColor;
        Outline(bannerText);
        Place(bannerText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(1400f, 120f));
        bannerText.gameObject.SetActive(false);
    }

    private void BuildBossBar(Transform parent)
    {
        bossBar = new GameObject("BarraChefe", typeof(RectTransform));
        var rt = (RectTransform)bossBar.transform;
        rt.SetParent(parent, false);
        Place(rt, new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(760f, 70f));

        bossNameText = UIFactory.CreateText(rt, "", 18f);
        bossNameText.enableAutoSizing = true;
        bossNameText.fontSizeMin = 14f;
        bossNameText.fontSizeMax = 18f;
        bossNameText.fontStyle = FontStyles.Bold;
        bossNameText.color = bossColor;
        Outline(bossNameText);
        Place(bossNameText.rectTransform, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(760f, 32f));

        // fundo escuro
        var bg = new GameObject("Fundo", typeof(RectTransform), typeof(Image));
        var bgRt = (RectTransform)bg.transform;
        bgRt.SetParent(rt, false);
        Place(bgRt, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(760f, 28f));
        bg.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);

        // preenchimento vermelho (encolhe da direita para a esquerda)
        var fill = new GameObject("Vida", typeof(RectTransform), typeof(Image));
        var fillRt = (RectTransform)fill.transform;
        fillRt.SetParent(bgRt, false);
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = new Vector2(4f, 4f);
        fillRt.offsetMax = new Vector2(-4f, -4f);
        fillRt.pivot = new Vector2(0f, 0.5f);
        bossFill = fill.GetComponent<Image>();
        bossFill.color = bossColor;

        bossBar.SetActive(false);
    }

    private static void Outline(TextMeshProUGUI t)
    {
        t.outlineWidth = 0.2f;
        t.outlineColor = Color.black;
    }

    private static void Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    private void Update()
    {
        var spawner = EnemySpawner.Instance;
        if (spawner == null || waveText == null) return;

        int wave = spawner.CurrentWave;
        string max = spawner.MaxWave > 0 ? " / " + spawner.MaxWave : "";
        string bossTag = spawner.IsBossWave ? "  <color=#FF5555>CHEFE</color>" : "";

        switch (spawner.CurrentPhase)
        {
            case EnemySpawner.Phase.Preparing:
                bool nextIsBoss = spawner.bossEnabled && spawner.MaxWave > 0 && wave + 1 == spawner.MaxWave;
                waveText.text = wave == 0 ? "PREPARE-SE" : "ONDA " + wave + max;
                int secs = Mathf.CeilToInt(spawner.PrepTimeLeft);
                int bonus = spawner.EarlyCallBonus;
                infoText.text = (nextIsBoss ? "<color=#FF5555>O CHEFE vem na proxima onda!</color>   " : "")
                    + "Proxima onda em " + secs + "s"
                    + (bonus > 0 ? "   -   [N] Chamar agora <color=#FFD640>(+" + bonus + ")</color>" : "   -   [N] Chamar agora");
                break;

            case EnemySpawner.Phase.Spawning:
            case EnemySpawner.Phase.Fighting:
                waveText.text = "ONDA " + wave + max + bossTag;
                infoText.text = "Inimigos restantes: " + spawner.AliveEnemies;
                break;

            case EnemySpawner.Phase.Finished:
                waveText.text = "ONDA " + wave + max;
                infoText.text = "Todas as ondas vencidas!";
                break;
        }

        int score = ScoreManager.Instance != null ? ScoreManager.Instance.Score : 0;
        scoreText.text = "Pontos: " + score + "   -   " + UIFactory.RemoveDiacritics(GameSession.Current.name);

        UpdateBossBar(spawner);
    }

    private void UpdateBossBar(EnemySpawner spawner)
    {
        var boss = spawner.Boss;
        if (boss == null || boss.IsDead)
        {
            if (bossBar.activeSelf) bossBar.SetActive(false);
            return;
        }

        if (!bossBar.activeSelf) { bossBar.SetActive(true); bossShown = 1f; }

        float target = boss.maxHealth > 0f ? Mathf.Clamp01(boss.CurrentHealth / boss.maxHealth) : 0f;
        bossShown = Mathf.Lerp(bossShown, target, 8f * Time.deltaTime);   // desce suave
        bossFill.rectTransform.localScale = new Vector3(bossShown, 1f, 1f);

        var controller = boss.GetComponent<BossController>();
        bool raged = controller != null && controller.Raged;
        bossNameText.text = UIFactory.RemoveDiacritics(spawner.BossDisplayName.ToUpperInvariant())
            + (raged ? "  <color=#FFD640>FURIA!</color>" : "");
    }

    private void HandleWaveStarted(int wave)
    {
        var spawner = EnemySpawner.Instance;
        bool boss = spawner != null && spawner.IsBossWave;
        ShowBanner(boss ? "ONDA FINAL!" : "ONDA " + wave + "!", boss ? bossColor : bannerColor);
    }

    private void HandleWaveCleared(int wave, int bonus)
    {
        ShowBanner(bonus > 0 ? "Onda concluida!  <size=60%>+" + bonus + " moedas</size>" : "Onda concluida!", bannerColor);
    }

    private void HandleBossSpawned(EnemyHealth boss) => ShowBanner("O CHEFE CHEGOU!", bossColor);
    private void HandleBossRage() => ShowBanner("O CHEFE ESTA FURIOSO!", bossColor);
    private void HandleBossDefeated() => ShowBanner("CHEFE DERROTADO!", bannerColor);

    private void ShowBanner(string text, Color color)
    {
        if (bannerText == null) return;
        if (bannerRoutine != null) StopCoroutine(bannerRoutine);
        bannerRoutine = StartCoroutine(BannerRoutine(text, color));
    }

    private IEnumerator BannerRoutine(string text, Color color)
    {
        bannerText.text = UIFactory.RemoveDiacritics(text);
        bannerText.color = color;
        bannerText.gameObject.SetActive(true);
        var rt = bannerText.rectTransform;

        for (float t = 0f; t < bannerDuration; t += Time.unscaledDeltaTime)
        {
            float k = t / bannerDuration;
            // entra crescendo rápido e some no final
            float scale = k < 0.15f ? Mathf.Lerp(0.6f, 1f, k / 0.15f) : 1f;
            float alpha = k < 0.7f ? 1f : Mathf.Lerp(1f, 0f, (k - 0.7f) / 0.3f);
            rt.localScale = Vector3.one * scale;
            bannerText.alpha = alpha;
            yield return null;
        }

        bannerText.gameObject.SetActive(false);
        bannerRoutine = null;
    }
}
