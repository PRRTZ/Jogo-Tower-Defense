using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Menu de pausa: abre pelo Esc ou pelo ícone de configurações do prefab UI.
/// Continuar (Esc), Recomeçar a fase, Voltar ao menu principal e Sair do jogo. O jogo fica parado enquanto aberto.
/// </summary>
public class PauseSettingsMenu : MonoBehaviour
{
    [SerializeField] private TMP_FontAsset hudFont;

    public static bool IsOpen { get; private set; }
    /// <summary>Frame em que a pausa fechou (o clique em "Continuar" não vira ataque).</summary>
    public static int ClosedFrame { get; private set; } = -1;

    private GameObject screen;
    private float previousTimeScale;
    private bool previousAudioPaused;
    private bool previousTowerInputBlocked;
    private bool previousCursorVisible;
    private CursorLockMode previousCursorLockState;

    private void Awake()
    {
        if (hudFont == null)
        {
            Debug.LogError("PauseSettingsMenu: configure a fonte da HUD no prefab UI.", this);
            return;
        }

        UIFactory.DefaultFont = hudFont;
    }

    private void Start()
    {
        EnsureEventSystem();

        var icon = transform.Find("ConfigUI/ConfigIcon");
        if (icon == null)
        {
            Debug.LogError("PauseSettingsMenu: não encontrou ConfigUI/ConfigIcon no prefab UI.", this);
            enabled = false;
            return;
        }

        var image = icon.GetComponent<Image>();
        if (image == null)
        {
            Debug.LogError("PauseSettingsMenu: ConfigIcon não tem um componente Image.", this);
            enabled = false;
            return;
        }

        var button = icon.GetComponent<Button>();
        if (button == null) button = icon.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        button.onClick.AddListener(() =>
        {
            SoundManager.Play(Sfx.ButtonClick);
            OpenSettings();
        });

        BuildUI();
    }

    private void OnDestroy()
    {
        if (IsOpen) RestoreGameState();
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null || !kb.escapeKey.wasPressedThisFrame || screen == null) return;

        if (IsOpen) { Resume(); return; }

        // Esc abre a pausa, menos quando está fechando a forja ou o jogo já acabou
        if (GameOverManager.IsGameOver) return;
        if (PlayerForge.IsOpen || PlayerForge.ClosedFrame == Time.frameCount) return;
        SoundManager.Play(Sfx.ButtonClick);
        OpenSettings();
    }

    private void OpenSettings()
    {
        if (IsOpen || GameOverManager.IsGameOver) return;

        previousTimeScale = Time.timeScale;
        previousAudioPaused = AudioListener.pause;
        previousTowerInputBlocked = TowerBuilder.InputBlocked;
        previousCursorVisible = Cursor.visible;
        previousCursorLockState = Cursor.lockState;

        IsOpen = true;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        TowerBuilder.InputBlocked = true;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        screen.SetActive(true);
    }

    private void Resume()
    {
        screen.SetActive(false);
        RestoreGameState();
    }

    private void RestartLevel()
    {
        Resume();

        if (GameOverManager.Instance == null)
        {
            Debug.LogError("PauseSettingsMenu: não encontrou GameOverManager para reiniciar a fase.", this);
            return;
        }

        GameOverManager.Instance.Restart();
    }

    private void ReturnToMenu()
    {
        Resume();

        if (GameOverManager.Instance == null)
        {
            Debug.LogError("PauseSettingsMenu: não encontrou GameOverManager para voltar ao menu.", this);
            return;
        }

        GameOverManager.Instance.GoToMenu();
    }

    private void QuitGame()
    {
        Resume();

        if (GameOverManager.Instance != null) { GameOverManager.Instance.Quit(); return; }
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void RestoreGameState()
    {
        IsOpen = false;
        ClosedFrame = Time.frameCount;
        Time.timeScale = previousTimeScale;
        AudioListener.pause = previousAudioPaused;
        TowerBuilder.InputBlocked = GameOverManager.IsGameOver || previousTowerInputBlocked;
        Cursor.visible = previousCursorVisible;
        Cursor.lockState = previousCursorLockState;
    }

    private void BuildUI()
    {
        var canvas = UIFactory.CreateCanvas("UI_Configuracoes", 110);

        var bg = new GameObject("Fundo", typeof(RectTransform), typeof(Image));
        var bgRt = (RectTransform)bg.transform;
        bgRt.SetParent(canvas.transform, false);
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;
        bg.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);
        screen = bg;

        var panel = UIFactory.CreatePanel(bg.transform, "Painel", 600f, new Vector2(0.5f, 0.5f), Vector2.zero);
        panel.GetComponent<VerticalLayoutGroup>().spacing = 14f;

        var title = UIFactory.CreateText(panel, "JOGO PAUSADO", 28f);
        title.fontStyle = FontStyles.Bold;
        title.color = UIFactory.Gold;

        UIFactory.CreateButton(panel, "Continuar  [Esc]", Resume, UIFactory.ButtonColor, out _, 54f);
        UIFactory.CreateButton(panel, "Recomecar fase", RestartLevel, UIFactory.ButtonNeutral, out _, 54f);
        UIFactory.CreateButton(panel, "Voltar ao menu principal", ReturnToMenu, UIFactory.ButtonNeutral, out _, 54f);
        UIFactory.CreateButton(panel, "Sair do jogo", QuitGame, UIFactory.ButtonAltColor, out _, 54f);

        screen.SetActive(false);
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null || FindAnyObjectByType<EventSystem>() != null) return;
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }
}
