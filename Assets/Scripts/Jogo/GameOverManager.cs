using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// Fim da fase:
///   - GAME OVER quando um inimigo chega à base (EnemyMovement.OnReachedBase) ou o player morre (Health.OnPlayerDied)
///   - VITÓRIA quando todas as ondas são vencidas (EnemySpawner.OnAllWavesCleared)
/// Pausa o jogo e mostra a tela com "Jogar de novo" (R) e "Sair".
/// Coloque no GameManager de cada mapa (cria a própria tela).
/// </summary>
public class GameOverManager : MonoBehaviour
{
    public static GameOverManager Instance { get; private set; }
    public static bool IsGameOver { get; private set; }

    [Header("Textos")]
    [SerializeField] private string reachedBaseText = "Um inimigo chegou à sua base!";
    [SerializeField] private string playerDiedText = "Você foi derrotado!";
    [SerializeField] private string victoryText = "Você defendeu a vila!";

    [Header("Tempo")]
    [Tooltip("Espera antes da tela quando o player morre, para dar tempo de ver a animação.")]
    [SerializeField] private float playerDeathDelay = 1.5f;

    private GameObject screen;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI reasonText;
    private TextMeshProUGUI waveText;

    private void Awake()
    {
        Instance = this;

        // começa cada fase com tudo "normal" (importante ao recarregar a cena)
        IsGameOver = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        TowerBuilder.InputBlocked = false;
    }

    private void OnEnable()
    {
        EnemyMovement.OnReachedBase += HandleReachedBase;
        Health.OnPlayerDied += HandlePlayerDied;
        EnemySpawner.OnAllWavesCleared += HandleVictory;
    }

    private void OnDisable()
    {
        EnemyMovement.OnReachedBase -= HandleReachedBase;
        Health.OnPlayerDied -= HandlePlayerDied;
        EnemySpawner.OnAllWavesCleared -= HandleVictory;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        EnsureEventSystem();
        BuildUI();
        screen.SetActive(false);
    }

    private void Update()
    {
        if (!IsGameOver) return;

        var kb = Keyboard.current;
        if (kb != null && kb.rKey.wasPressedThisFrame) Restart();
    }

    // ---------- gatilhos ----------

    private void HandleReachedBase(EnemyMovement enemy) => Lose(reachedBaseText);

    private void HandlePlayerDied()
    {
        if (IsGameOver) return;
        StartCoroutine(LoseAfterDelay());
    }

    private IEnumerator LoseAfterDelay()
    {
        yield return new WaitForSecondsRealtime(playerDeathDelay);
        Lose(playerDiedText);
    }

    private void HandleVictory() => Show(true, victoryText);

    public void Lose(string reason) => Show(false, reason);

    private void Show(bool victory, string reason)
    {
        if (IsGameOver) return;
        IsGameOver = true;

        Time.timeScale = 0f;              // congela inimigos, torres e ondas
        AudioListener.pause = true;
        TowerBuilder.InputBlocked = true;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        titleText.text = victory ? "VITÓRIA!" : "GAME OVER";
        titleText.color = victory ? new Color(1f, 0.84f, 0.25f) : new Color(1f, 0.3f, 0.3f);
        reasonText.text = reason;

        var spawner = EnemySpawner.Instance;
        if (spawner != null)
        {
            string max = spawner.MaxWave > 0 ? " / " + spawner.MaxWave : "";
            waveText.text = "Onda alcançada: " + spawner.CurrentWave + max;
        }
        else waveText.text = "";

        screen.SetActive(true);
    }

    // ---------- botões ----------

    public void Restart()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;

        var scene = SceneManager.GetActiveScene();
        if (SceneUtility.GetBuildIndexByScenePath(scene.path) >= 0)
        {
            SceneManager.LoadScene(scene.buildIndex);
            return;
        }

#if UNITY_EDITOR
        // a cena não está na lista de Build: no editor ainda dá para recarregar
        EditorSceneManager.LoadSceneInPlayMode(scene.path, new LoadSceneParameters(LoadSceneMode.Single));
#else
        Debug.LogError("GameOverManager: adicione a cena em File > Build Profiles > Scene List.");
#endif
    }

    public void Quit()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ---------- tela ----------

    private void BuildUI()
    {
        var canvas = UIFactory.CreateCanvas("UI_FimDeJogo", 100);

        // fundo escuro na tela toda (também bloqueia cliques no jogo)
        var bg = new GameObject("Fundo", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        var bgRt = (RectTransform)bg.transform;
        bgRt.SetParent(canvas.transform, false);
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;
        bg.GetComponent<UnityEngine.UI.Image>().color = new Color(0f, 0f, 0f, 0.75f);
        screen = bg;

        var panel = UIFactory.CreatePanel(bg.transform, "Painel", 560f, new Vector2(0.5f, 0.5f), Vector2.zero);
        panel.GetComponent<UnityEngine.UI.VerticalLayoutGroup>().spacing = 14f;

        titleText = UIFactory.CreateText(panel, "GAME OVER", 72f);
        titleText.fontStyle = FontStyles.Bold;

        reasonText = UIFactory.CreateText(panel, "", 28f);
        waveText = UIFactory.CreateText(panel, "", 24f);
        waveText.color = new Color(0.8f, 0.8f, 0.8f);

        UIFactory.CreateButton(panel, "Jogar de novo  [R]", Restart, UIFactory.ButtonColor, out _, 56f);
        UIFactory.CreateButton(panel, "Sair", Quit, UIFactory.ButtonNeutral, out _, 46f);
    }

    // os mapas não têm EventSystem; sem ele os botões não recebem clique
    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null || FindAnyObjectByType<EventSystem>() != null) return;
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }
}
