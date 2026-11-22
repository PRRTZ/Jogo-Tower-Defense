using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// HUD das ondas, no topo da tela:
///   "ONDA 3 / 10"
///   "Inimigos restantes: 4"   ou   "Próxima onda em 8s  ·  [N] Chamar agora (+15)"
/// E avisos no centro: "ONDA 4!" quando começa e "Onda concluída! +40 moedas" quando termina.
/// Coloque no GameManager (cria o próprio Canvas).
/// </summary>
public class WaveHUD : MonoBehaviour
{
    [SerializeField] private Color titleColor = Color.white;
    [SerializeField] private Color bannerColor = new Color(1f, 0.84f, 0.25f, 1f);
    [SerializeField] private float bannerDuration = 1.8f;

    private TextMeshProUGUI waveText;
    private TextMeshProUGUI infoText;
    private TextMeshProUGUI bannerText;
    private Coroutine bannerRoutine;

    private void OnEnable()
    {
        EnemySpawner.OnWaveStarted += HandleWaveStarted;
        EnemySpawner.OnWaveCleared += HandleWaveCleared;
    }

    private void OnDisable()
    {
        EnemySpawner.OnWaveStarted -= HandleWaveStarted;
        EnemySpawner.OnWaveCleared -= HandleWaveCleared;
    }

    private void Start()
    {
        BuildUI();
    }

    private void BuildUI()
    {
        var canvas = UIFactory.CreateCanvas("UI_Ondas", 6);

        waveText = UIFactory.CreateText(canvas.transform, "", 42f);
        waveText.fontStyle = FontStyles.Bold;
        waveText.color = titleColor;
        Outline(waveText);
        Place(waveText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(900f, 56f));

        infoText = UIFactory.CreateText(canvas.transform, "", 26f);
        Outline(infoText);
        Place(infoText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -72f), new Vector2(1100f, 40f));

        bannerText = UIFactory.CreateText(canvas.transform, "", 80f);
        bannerText.fontStyle = FontStyles.Bold;
        bannerText.color = bannerColor;
        Outline(bannerText);
        Place(bannerText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(1400f, 120f));
        bannerText.gameObject.SetActive(false);
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

        switch (spawner.CurrentPhase)
        {
            case EnemySpawner.Phase.Preparing:
                waveText.text = wave == 0 ? "PREPARE-SE" : "ONDA " + wave + max;
                int secs = Mathf.CeilToInt(spawner.PrepTimeLeft);
                int bonus = spawner.EarlyCallBonus;
                infoText.text = "Próxima onda em " + secs + "s"
                    + (bonus > 0 ? "   ·   [N] Chamar agora <color=#FFD640>(+" + bonus + ")</color>" : "   ·   [N] Chamar agora");
                break;

            case EnemySpawner.Phase.Spawning:
            case EnemySpawner.Phase.Fighting:
                waveText.text = "ONDA " + wave + max;
                infoText.text = "Inimigos restantes: " + spawner.AliveEnemies;
                break;

            case EnemySpawner.Phase.Finished:
                waveText.text = "ONDA " + wave + max;
                infoText.text = "Todas as ondas vencidas!";
                break;
        }
    }

    private void HandleWaveStarted(int wave)
    {
        ShowBanner("ONDA " + wave + "!");
    }

    private void HandleWaveCleared(int wave, int bonus)
    {
        ShowBanner("Onda concluída!  <size=60%>+" + bonus + " moedas</size>");
    }

    private void ShowBanner(string text)
    {
        if (bannerText == null) return;
        if (bannerRoutine != null) StopCoroutine(bannerRoutine);
        bannerRoutine = StartCoroutine(BannerRoutine(text));
    }

    private IEnumerator BannerRoutine(string text)
    {
        bannerText.text = text;
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
