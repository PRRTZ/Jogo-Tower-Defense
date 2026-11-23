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
/// Pausa o jogo, fecha a pontuação, salva no Ranking do menu e mostra a tela com
/// "Jogar de novo" (R) e "Menu principal" (M).
/// Coloque no GameManager de cada mapa (cria a própria tela e o ScoreManager).
/// </summary>
public class GameOverManager : MonoBehaviour
{
    public static GameOverManager Instance { get; private set; }
    public static bool IsGameOver { get; private set; }

    [Header("Textos")]
    [SerializeField] private string reachedBaseText = "Um inimigo chegou a sua base!";
    [SerializeField] private string playerDiedText = "Voce foi derrotado!";
    [SerializeField] private string victoryText = "Voce defendeu a vila!";

    [Header("Menu")]
    [Tooltip("Nome da cena do menu principal (precisa estar no Build Settings).")]
    [SerializeField] private string menuScene = "MainMenu";

    [Header("Música do fim de jogo (opcional)")]
    [Tooltip("Toca quando a tela de VITÓRIA abre (em loop, mesmo com o jogo pausado). Vazio = continua a música da fase.")]
    [SerializeField] private AudioClip victoryMusic;
    [Tooltip("Toca quando a tela de GAME OVER abre (em loop, mesmo com o jogo pausado). Vazio = continua a música da fase.")]
    [SerializeField] private AudioClip defeatMusic;
    [SerializeField, Range(0f, 1f)] private float endMusicVolume = 0.5f;

    [Header("Tempo")]
    [Tooltip("Espera antes da tela quando o player morre, para dar tempo de ver a animação.")]
    [SerializeField] private float playerDeathDelay = 1.5f;

    private GameObject screen;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI reasonText;
    private TextMeshProUGUI waveText;
    private TextMeshProUGUI scoreText;
    private TextMeshProUGUI rankText;

    private void Awake()
    {
        Instance = this;

        // começa cada fase com tudo "normal" (importante ao recarregar a cena)
        IsGameOver = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        TowerBuilder.InputBlocked = false;

        // pontuação da partida
        if (FindAnyObjectByType<ScoreManager>() == null) gameObject.AddComponent<ScoreManager>();
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
        if (kb != null && kb.mKey.wasPressedThisFrame) GoToMenu();
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

        Time.timeScale = 0f;              // congela inimigos, torres e ondas (os sons de vitória/derrota continuam)
        TowerBuilder.InputBlocked = true;
        if (!victory) SoundManager.Play(Sfx.Defeat);   // o som de vitória já toca pelo EventSounds
        PlayEndMusic(victory ? victoryMusic : defeatMusic);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        titleText.text = victory ? "VITORIA!" : "GAME OVER";
        titleText.color = victory ? new Color(1f, 0.84f, 0.25f) : new Color(1f, 0.3f, 0.3f);
        reasonText.text = UIFactory.RemoveDiacritics(reason);

        var spawner = EnemySpawner.Instance;
        if (spawner != null)
        {
            string max = spawner.MaxWave > 0 ? " / " + spawner.MaxWave : "";
            waveText.text = "Onda alcancada: " + spawner.CurrentWave + max;
        }
        else waveText.text = "";

        // pontuação e ranking (Top 5 por dificuldade, mostrado no menu)
        int score = ScoreManager.Instance != null ? ScoreManager.Instance.FinishMatch(victory) : 0;
        int position = Ranking.AddAndGetPosition(GameSession.DifficultyIndex, GameSession.PlayerName, score);
        scoreText.text = "Pontos: " + score + "   <size=75%>("
            + UIFactory.RemoveDiacritics(GameSession.Current.name) + ")</size>";
        rankText.text = position == 1 ? "<color=#FFD640>NOVO RECORDE!</color>"
                      : position > 0 ? "Voce ficou na posicao " + position + " do ranking!"
                      : "";

        screen.SetActive(true);
    }

    // ---------- música ----------

    private AudioSource endMusicSource;

    private void PlayEndMusic(AudioClip clip)
    {
        if (clip == null) return;                      // sem música própria: segue a música da fase

        if (SoundManager.Instance != null)
        {
            SoundManager.PlayMusic(clip);              // troca a música de fundo (com fade, funciona pausado)
            return;
        }

        // cena sem SoundManager: toca por conta própria
        if (endMusicSource == null)
        {
            endMusicSource = gameObject.AddComponent<AudioSource>();
            endMusicSource.playOnAwake = false;
            endMusicSource.loop = true;
            endMusicSource.spatialBlend = 0f;
            endMusicSource.ignoreListenerPause = true;
        }
        endMusicSource.clip = clip;
        endMusicSource.volume = endMusicVolume;
        endMusicSource.Play();
    }

    // ---------- botões ----------

    public void Restart()
    {
        Time.timeScale = 1f;

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

    public void GoToMenu()
    {
        if (!Application.CanStreamedLevelBeLoaded(menuScene))
        {
            Quit();     // sem menu no Build Settings: só fecha
            return;
        }
        Time.timeScale = 1f;
        SceneManager.LoadScene(menuScene);
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

        titleText = UIFactory.CreateText(panel, "GAME OVER", 40f);
        titleText.fontStyle = FontStyles.Bold;

        reasonText = UIFactory.CreateText(panel, "", 18f);
        reasonText.enableAutoSizing = true;
        reasonText.fontSizeMin = 14f;
        reasonText.fontSizeMax = 18f;
        waveText = UIFactory.CreateText(panel, "", 16f);
        waveText.color = new Color(0.8f, 0.8f, 0.8f);

        scoreText = UIFactory.CreateText(panel, "", 22f);
        scoreText.fontStyle = FontStyles.Bold;
        scoreText.color = UIFactory.Gold;
        rankText = UIFactory.CreateText(panel, "", 16f);

        UIFactory.CreateButton(panel, "Jogar de novo  [R]", Restart, UIFactory.ButtonColor, out _, 56f);
        bool hasMenu = Application.CanStreamedLevelBeLoaded(menuScene);
        UIFactory.CreateButton(panel, hasMenu ? "Menu principal  [M]" : "Sair", hasMenu ? (UnityEngine.Events.UnityAction)GoToMenu : Quit,
                               UIFactory.ButtonNeutral, out _, 46f);
    }

    // os mapas não têm EventSystem; sem ele os botões não recebem clique
    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null || FindAnyObjectByType<EventSystem>() != null) return;
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }
}
