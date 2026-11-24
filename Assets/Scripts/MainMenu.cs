using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Menu principal montado por código: adicione este script a um objeto vazio numa cena nova.
// Todas as imagens são OPCIONAIS: o que ficar vazio usa o visual gerado por código.
public class MainMenu : MonoBehaviour
{
    [Header("Textos")]
    public string gameTitle = "Tower Defense";
    public string subtitle = "Defenda a base dos bichinhos de pelúcia!";
    [Tooltip("Desmarque para o título ficar parado (sem balançar).")]
    public bool animateTitle = true;
    [Tooltip("Tamanho da fonte do título.")]
    public float titleFontSize = 76f;
    [Tooltip("Tamanho da fonte do subtítulo.")]
    public float subtitleFontSize = 22f;

    [Header("Título colorido")]
    [Tooltip("Ligado: cada letra do título com uma cor, trocando sozinhas. Desligado: usa Theme Title Color.")]
    public bool colorfulTitle = false;
    [Tooltip("Cores usadas nas letras, em ordem. Adicione ou remova cores à vontade.")]
    public Color[] titleColors =
    {
        new Color(1f, 0.30f, 0.30f),   // vermelho
        new Color(1f, 0.62f, 0.20f),   // laranja
        new Color(1f, 0.90f, 0.25f),   // amarelo
        new Color(0.40f, 0.85f, 0.35f),// verde
        new Color(0.30f, 0.65f, 1f),   // azul
        new Color(0.70f, 0.45f, 1f),   // roxo
    };
    [Tooltip("Quantas vezes por segundo as cores andam pelas letras (0 = paradas).")]
    [Range(0f, 20f)] public float titleColorSpeed = 4f;
    [Tooltip("Ligado: as cores se misturam suavemente. Desligado: trocam de uma vez (efeito pisca-pisca).")]
    public bool smoothTitleColors = true;
    [Header("Tema")]
    [Tooltip("Ligado: visual de madeira e céu, no estilo do jogo (ignora as cores pastel abaixo). Desligado: visual original.")]
    public bool useGameTheme = true;

    [Header("Mapas (nome da cena no Build Settings)")]
    public string[] mapScenes = { "Map_01", "Map_02" };
    public string[] mapNames = { "Mapa 1 - Castelo", "Mapa 2 - Vilarejo" };

    [Header("Imagens (opcional - arraste aqui)")]
    public Sprite backgroundSprite;                  // fundo da tela (cobre tudo, sem distorcer)
    public Sprite logoSprite;                        // logo: substitui o texto do título
    public Vector2 logoSize = new Vector2(900, 300);
    public Sprite buttonSprite;                      // botões (use Border no Sprite Editor para esticar bem)
    public Sprite buttonSelectedSprite;              // botão marcado (dificuldade / mapa)
    public Sprite panelSprite;                       // cartões do Ranking
    public Sprite inputSprite;                       // campo do nome
    public Sprite[] floatingSprites;                 // imagens que flutuam no fundo (vazio = bolhas)
    public int floatingCount = 14;
    public Texture2D cursorTexture;                  // cursor do mouse (Texture Type = Cursor)
    public Vector2 cursorHotspot = Vector2.zero;

    [Header("Cores e fonte")]
    public TMP_FontAsset font;                                       // opcional: uma fonte fofa
    public Color topColor = new Color(1f, 0.84f, 0.91f);             // degradê do fundo (se não houver imagem)
    public Color bottomColor = new Color(0.73f, 0.84f, 1f);
    public Color textColor = new Color(0.27f, 0.16f, 0.35f);         // textos gerais
    public Color buttonTextColor = new Color(0.27f, 0.16f, 0.35f);   // texto dos botões
    public Color titleOutlineColor = new Color(0.27f, 0.16f, 0.35f); // contorno do título
    public bool tintCustomButtons = false;                           // marque para tingir seu Button Sprite com as cores pastel

    [Header("Áudio (opcional)")]
    public AudioClip music;
    public AudioClip clickSound;
    public AudioClip hoverSound;
    [Range(0f, 1f)] public float volume = 0.5f;

    // paleta pastel (visual original)
    static readonly Color Pink = new Color(1f, 0.56f, 0.69f);
    static readonly Color Mint = new Color(0.58f, 0.88f, 0.74f);
    static readonly Color Lilac = new Color(0.73f, 0.63f, 0.94f);
    static readonly Color Sun = new Color(1f, 0.84f, 0.4f);
    static readonly Color Sky = new Color(0.55f, 0.8f, 1f);

    [Header("Cores do tema do jogo (com Use Game Theme ligado)")]
    public Color skyColor = new Color(0.33f, 0.58f, 0.90f);          // fundo: em cima
    public Color grassColor = new Color(0.62f, 0.84f, 0.52f);        // fundo: embaixo
    public Color themeTitleColor = new Color(1f, 0.78f, 0.28f);      // título
    public Color themeOutlineColor = new Color(0.26f, 0.15f, 0.08f); // contorno do título e dos textos
    public Color themeTextColor = new Color(1f, 0.97f, 0.88f);       // textos sobre o céu (subtítulo, "Escolha a dificuldade"...)
    public Color themeButtonTextColor = new Color(0.26f, 0.15f, 0.08f); // texto dos botões
    public Color themeCardTextColor = new Color(0.26f, 0.15f, 0.08f);   // texto dos cartões do Ranking e do campo do nome
    public Color playButtonColor = new Color(0.45f, 0.72f, 0.33f);   // Jogar e Começar
    public Color rankingButtonColor = new Color(1f, 0.78f, 0.28f);   // Ranking e opção marcada
    public Color quitButtonColor = new Color(0.84f, 0.38f, 0.32f);   // Sair e Limpar ranking
    public Color backButtonColor = new Color(0.74f, 0.52f, 0.32f);   // Voltar e opção não marcada
    public Color cardColor = new Color(0.98f, 0.91f, 0.75f);         // cartões do Ranking e campo do nome

    // nomes usados no resto do código
    Color Leaf => playButtonColor;
    Color Gold => rankingButtonColor;
    Color Berry => quitButtonColor;
    Color Wood => backButtonColor;
    Color Parchment => cardColor;
    Color Cream => themeTextColor;
    Color DarkWood => themeOutlineColor;

    // escolhe a cor do tema do jogo ou a pastel original
    Color C(Color game, Color pastel) { return useGameTheme ? game : pastel; }
    Color CardText() { return useGameTheme ? themeCardTextColor : textColor; }

    Canvas canvas;
    Sprite roundSprite, circleSprite;
    GameObject mainPanel, playPanel, rankingPanel, currentPanel;
    TMP_Text titleText, difficultyDesc, statusText;
    RectTransform titleRect;
    TMP_InputField nameInput;
    Transform rankingRow;
    readonly List<Image> diffButtons = new List<Image>();
    readonly List<Image> mapButtons = new List<Image>();
    int selDiff, selMap;
    float statusUntil;
    AudioSource audioSrc;

    class Floater { public RectTransform rt; public Vector2 basePos; public float speed, phase, amp; public bool sway; }
    readonly List<Floater> floaters = new List<Floater>();

    // ---------------------------------------------------------------- início
    void Start()
    {
        Time.timeScale = 1f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        if (cursorTexture != null) Cursor.SetCursor(cursorTexture, cursorHotspot, CursorMode.Auto);

        EnsureEventSystem();
        if (useGameTheme)
        {
            topColor = skyColor;                              // céu
            bottomColor = grassColor;                         // horizonte de grama
            textColor = Cream;
            buttonTextColor = themeButtonTextColor;
            titleOutlineColor = DarkWood;
        }
        roundSprite = useGameTheme ? MakeWoodSprite() : MakeRoundedSprite(64, 24);
        circleSprite = MakeRoundedSprite(128, 64);
        FixMapScenes();

        selDiff = Mathf.Clamp(GameSession.DifficultyIndex, 0, GameSession.Difficulties.Length - 1);
        selMap = Mathf.Clamp(GameSession.MapIndex, 0, Mathf.Max(0, mapScenes.Length - 1));

        SetupAudio();
        MenuButtonFx.Hovered += PlayHover;

        BuildCanvas();
        BuildBackground();
        BuildMainPanel();
        BuildPlayPanel();
        BuildRankingPanel();
        Show(mainPanel);
    }

    void OnDestroy()
    {
        MenuButtonFx.Hovered -= PlayHover;
        if (cursorTexture != null) Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);   // devolve o cursor normal
    }

    void Update()
    {
        float t = Time.unscaledTime;

        foreach (var f in floaters)
        {
            f.rt.anchoredPosition = f.basePos + new Vector2(
                Mathf.Sin(t * f.speed + f.phase) * f.amp,
                Mathf.Cos(t * f.speed * 0.8f + f.phase) * f.amp);
            if (f.sway) f.rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * f.speed + f.phase) * 8f);
        }

        if (titleRect != null && animateTitle)
        {
            titleRect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 1.6f) * 2f);
            float s = 1f + Mathf.Sin(t * 2.2f) * 0.025f;
            titleRect.localScale = new Vector3(s, s, 1f);
        }

        if (colorfulTitle) ApplyTitleColors(t);

        if (statusText != null && statusText.text != "" && t > statusUntil) statusText.text = "";

        // ESC volta ao menu principal
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame
            && currentPanel != mainPanel && currentPanel != null)
            Show(mainPanel);
    }

    // pinta cada letra do título com uma cor da lista e vai deslocando as cores com o tempo
    void ApplyTitleColors(float t)
    {
        if (titleText == null || titleColors == null || titleColors.Length == 0) return;

        titleText.color = Color.white;          // a cor do texto multiplica a das letras: branco = cores puras
        titleText.ForceMeshUpdate();
        TMP_TextInfo info = titleText.textInfo;
        int n = titleColors.Length;
        float pos = t * titleColorSpeed;
        int letter = 0;

        for (int i = 0; i < info.characterCount; i++)
        {
            TMP_CharacterInfo ch = info.characterInfo[i];
            if (!ch.isVisible) continue;        // espaços não contam

            float k = pos + letter++;
            int a = ((int)Mathf.Floor(k) % n + n) % n;
            int b = (a + 1) % n;
            Color c = smoothTitleColors ? Color.Lerp(titleColors[a], titleColors[b], k - Mathf.Floor(k)) : titleColors[a];

            Color32 c32 = c;
            Color32[] cols = info.meshInfo[ch.materialReferenceIndex].colors32;
            int v = ch.vertexIndex;
            cols[v] = c32; cols[v + 1] = c32; cols[v + 2] = c32; cols[v + 3] = c32;
        }

        titleText.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }

    // ---------------------------------------------------------------- painéis
    void BuildMainPanel()
    {
        mainPanel = MakePanel("MainPanel");
        Transform p = mainPanel.transform;

        if (logoSprite != null)
        {
            var go = new GameObject("Logo", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            go.transform.SetParent(p, false);
            var im = go.GetComponent<Image>();
            im.sprite = logoSprite;
            im.preserveAspect = true;
            im.raycastTarget = false;
            var le = go.GetComponent<LayoutElement>();
            le.preferredWidth = logoSize.x;
            le.preferredHeight = logoSize.y;
            titleRect = (RectTransform)go.transform;
        }
        else
        {
            titleText = Label(p, gameTitle, titleFontSize, 1500, Mathf.Max(120f, titleFontSize * 1.6f), TextAlignmentOptions.Center, FontStyles.Bold);
            titleText.color = useGameTheme ? themeTitleColor : Color.white;
            titleText.outlineWidth = useGameTheme ? 0.28f : 0.22f;
            titleText.outlineColor = titleOutlineColor;
            titleRect = titleText.rectTransform;
        }

        Label(p, subtitle, subtitleFontSize, 1400, Mathf.Max(46f, subtitleFontSize * 2f));
        Spacer(p, 18);

        MakeButton(p, "Jogar", C(Leaf, Pink), 480, 82, 28, () => Show(playPanel));
        MakeButton(p, "Ranking", C(Gold, Sun), 480, 82, 28, () => Show(rankingPanel));
        MakeButton(p, "Sair", C(Berry, Lilac), 480, 82, 28, QuitGame);
    }

    void BuildPlayPanel()
    {
        playPanel = MakePanel("PlayPanel");
        Transform p = playPanel.transform;

        Label(p, "Escolha a dificuldade", 24, 1200, 48, TextAlignmentOptions.Center, FontStyles.Bold);
        Transform diffRow = MakeRow(p, 24);
        for (int i = 0; i < GameSession.Difficulties.Length; i++)
        {
            int idx = i;
            Button b = MakeButton(diffRow, GameSession.Difficulties[i].name, Color.white, 300, 72, 20, () => SelectDifficulty(idx));
            diffButtons.Add(b.GetComponent<Image>());
        }
        difficultyDesc = Label(p, "", 18, 1400, 76);

        Label(p, "Escolha o mapa", 24, 1200, 48, TextAlignmentOptions.Center, FontStyles.Bold);
        Transform mapRow = MakeRow(p, 24);
        for (int i = 0; i < mapScenes.Length; i++)
        {
            int idx = i;
            string label = i < mapNames.Length ? mapNames[i] : mapScenes[i];
            Button b = MakeButton(mapRow, label, Color.white, 400, 72, 18, () => SelectMap(idx));
            mapButtons.Add(b.GetComponent<Image>());
        }

        Transform nameRow = MakeRow(p, 20);
        Label(nameRow, "Seu nome:", 17, 260, 64, TextAlignmentOptions.MidlineRight);
        nameInput = MakeInput(nameRow, "Jogador", 520, 64);

        statusText = Label(p, "", 16, 1400, 36);
        statusText.color = useGameTheme ? new Color(1f, 0.45f, 0.4f) : new Color(0.8f, 0.15f, 0.25f);

        Transform btnRow = MakeRow(p, 40);
        MakeButton(btnRow, "Voltar", C(Wood, Lilac), 280, 72, 20, () => Show(mainPanel));
        MakeButton(btnRow, "Comecar!", C(Leaf, Mint), 380, 72, 22, StartGame);

        SelectDifficulty(selDiff);
        SelectMap(selMap);
    }

    void BuildRankingPanel()
    {
        rankingPanel = MakePanel("RankingPanel");
        Transform p = rankingPanel.transform;

        Label(p, "Ranking", 36, 1200, 64, TextAlignmentOptions.Center, FontStyles.Bold);
        Label(p, "Os 5 melhores de cada dificuldade", 16, 1400, 40);
        rankingRow = MakeRow(p, 24);

        Transform btnRow = MakeRow(p, 40);
        MakeButton(btnRow, "Voltar", C(Wood, Lilac), 280, 68, 20, () => Show(mainPanel));
        MakeButton(btnRow, "Limpar ranking", C(Berry, Pink), 390, 68, 18, () => { Ranking.Clear(); RefreshRanking(); });
    }

    void Show(GameObject target)
    {
        currentPanel = target;
        foreach (var p in new[] { mainPanel, playPanel, rankingPanel })
            if (p != null) p.SetActive(p == target);
        if (target == rankingPanel) RefreshRanking();
    }

    // ---------------------------------------------------------------- ações
    void SelectDifficulty(int i)
    {
        selDiff = i;
        for (int k = 0; k < diffButtons.Count; k++) SetSelected(diffButtons[k], k == i);

        DifficultyConfig c = GameSession.Difficulties[i];
        difficultyDesc.text = UIFactory.RemoveDiacritics(c.description + "\n" + c.waves + " ondas  |  chefe na ultima onda  |  pontos x" + c.scoreMultiplier.ToString("0.#"));
    }

    void SelectMap(int i)
    {
        selMap = i;
        for (int k = 0; k < mapButtons.Count; k++) SetSelected(mapButtons[k], k == i);
    }

    // aparência de botão marcado / desmarcado
    void SetSelected(Image img, bool selected)
    {
        if (buttonSprite == null && PixelWoodUITheme.WideButton() != null)
        {
            PixelWoodUITheme.ApplyWideBar(img, selected ? 1 : 0);
            img.color = Color.white;
            return;
        }

        if (buttonSprite != null)
        {
            if (buttonSelectedSprite != null)
            {
                ApplySprite(img, selected ? buttonSelectedSprite : buttonSprite);
                img.color = Color.white;
            }
            else img.color = selected ? C(Gold, Sun) : Color.white;
        }
        else if (useGameTheme) img.color = selected ? Gold : Wood;
        else img.color = selected ? Sun : new Color(1f, 1f, 1f, 0.85f);
    }

    void StartGame()
    {
        string scene = (mapScenes != null && selMap < mapScenes.Length) ? mapScenes[selMap] : "";
        if (string.IsNullOrEmpty(scene) || !Application.CanStreamedLevelBeLoaded(scene))
        {
            ShowStatus("A cena \"" + scene + "\" não está no Build Settings.");
            return;
        }

        string n = nameInput != null ? nameInput.text.Trim() : "";
        GameSession.PlayerName = string.IsNullOrEmpty(n) ? "Jogador" : n;
        GameSession.DifficultyIndex = selDiff;
        GameSession.MapIndex = selMap;
        PlayerPrefs.Save();
        SceneManager.LoadScene(scene);
    }

    void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void ShowStatus(string msg)
    {
        statusText.text = msg;
        statusUntil = Time.unscaledTime + 3f;
    }

    void RefreshRanking()
    {
        for (int i = rankingRow.childCount - 1; i >= 0; i--)
        {
            Transform c = rankingRow.GetChild(i);
            c.SetParent(null);
            Destroy(c.gameObject);
        }

        int n = GameSession.Difficulties.Length;
        float w = Mathf.Min(520f, 1500f / n);
        for (int d = 0; d < n; d++)
        {
            GameObject card = MakeCard(rankingRow, w, 520);
            TMP_Text t = MakeText(card.transform, BuildRankingText(d), 18, CardText(), TextAlignmentOptions.TopLeft);
            Stretch(t.rectTransform);
            t.rectTransform.offsetMin = new Vector2(28, 28);
            t.rectTransform.offsetMax = new Vector2(-28, -28);
        }
    }

    string BuildRankingText(int d)
    {
        var sb = new StringBuilder();
        sb.Append("<size=22><b>").Append(GameSession.Difficulties[d].name).Append("</b></size>\n\n");
        List<Ranking.Entry> list = Ranking.Get(d);
        for (int i = 0; i < Ranking.MaxEntries; i++)
        {
            if (i < list.Count)
                sb.Append(i + 1).Append(". ").Append(Clean(list[i].name)).Append(" - ").Append(list[i].score).Append(" pts\n");
            else
                sb.Append(i + 1).Append(". ---\n");
        }
        return sb.ToString();
    }

    static string Clean(string s) { return string.IsNullOrEmpty(s) ? "" : s.Replace("<", "").Replace(">", ""); }

    // ---------------------------------------------------------------- áudio
    void SetupAudio()
    {
        audioSrc = gameObject.AddComponent<AudioSource>();
        audioSrc.playOnAwake = false;
        audioSrc.loop = true;
        audioSrc.spatialBlend = 0f;
        audioSrc.volume = volume;
        if (music != null) { audioSrc.clip = music; audioSrc.Play(); }
    }

    void PlayClick()
    {
        if (clickSound != null && audioSrc != null) audioSrc.PlayOneShot(clickSound, volume);
    }

    void PlayHover()
    {
        if (hoverSound != null && audioSrc != null) audioSrc.PlayOneShot(hoverSound, volume * 0.6f);
    }

    // ---------------------------------------------------------------- construção da UI
    void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null) return;
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }

    void BuildCanvas()
    {
        var go = new GameObject("MenuCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
    }

    void BuildBackground()
    {
        var bg = new GameObject("Background", typeof(RectTransform), typeof(RectMask2D));
        bg.transform.SetParent(canvas.transform, false);
        Stretch((RectTransform)bg.transform);

        if (backgroundSprite != null)
        {
            // imagem do usuário: cobre a tela inteira mantendo a proporção
            var im = new GameObject("BackgroundImage", typeof(RectTransform), typeof(Image), typeof(AspectRatioFitter));
            im.transform.SetParent(bg.transform, false);
            var img = im.GetComponent<Image>();
            img.sprite = backgroundSprite;
            img.raycastTarget = false;
            var fit = im.GetComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = backgroundSprite.rect.width / backgroundSprite.rect.height;
        }
        else
        {
            var img = bg.AddComponent<Image>();
            img.sprite = MakeGradient(topColor, bottomColor);
            img.raycastTarget = false;
        }

        bool custom = floatingSprites != null && floatingSprites.Length > 0;
        Color[] tints = useGameTheme ? new[] { Color.white, Color.white, new Color(1f, 0.97f, 0.9f) }
                                     : new[] { Pink, Mint, Lilac, Sun, Sky, Color.white };

        for (int i = 0; i < floatingCount; i++)
        {
            var go = new GameObject("Floater", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(bg.transform, false);
            var im = go.GetComponent<Image>();
            im.raycastTarget = false;

            float size;
            if (custom)
            {
                Sprite s = floatingSprites[Random.Range(0, floatingSprites.Length)];
                if (s == null) { Destroy(go); continue; }
                im.sprite = s;
                im.preserveAspect = true;
                im.color = Color.white;
                size = Random.Range(110f, 230f);
            }
            else
            {
                im.sprite = circleSprite;
                Color c = tints[Random.Range(0, tints.Length)];
                c.a = useGameTheme ? 0.45f : 0.35f;
                im.color = c;
                size = useGameTheme ? Random.Range(140f, 340f) : Random.Range(70f, 260f);
            }

            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(size, size);
            Vector2 pos = new Vector2(Random.Range(-900f, 900f), Random.Range(-500f, 500f));
            rt.anchoredPosition = pos;

            floaters.Add(new Floater
            {
                rt = rt, basePos = pos, sway = custom,
                speed = Random.Range(0.3f, 0.8f), phase = Random.Range(0f, 6.28f), amp = Random.Range(15f, 40f)
            });
        }
    }

    GameObject MakePanel(string name)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup));
        go.transform.SetParent(canvas.transform, false);
        Stretch((RectTransform)go.transform);
        var v = go.GetComponent<VerticalLayoutGroup>();
        v.childAlignment = TextAnchor.MiddleCenter;
        v.spacing = 22;
        v.padding = new RectOffset(40, 40, 40, 40);
        v.childControlWidth = true;
        v.childControlHeight = true;
        v.childForceExpandWidth = false;
        v.childForceExpandHeight = false;
        return go;
    }

    Transform MakeRow(Transform parent, float spacing)
    {
        var go = new GameObject("Row", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        go.transform.SetParent(parent, false);
        var h = go.GetComponent<HorizontalLayoutGroup>();
        h.childAlignment = TextAnchor.MiddleCenter;
        h.spacing = spacing;
        h.childControlWidth = true;
        h.childControlHeight = true;
        h.childForceExpandWidth = false;
        h.childForceExpandHeight = false;
        return go.transform;
    }

    void Spacer(Transform parent, float height)
    {
        var go = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        go.GetComponent<LayoutElement>().preferredHeight = height;
    }

    // usa o sprite do usuário (9-slice se tiver Border) ou o retângulo arredondado gerado por código
    void ApplySprite(Image img, Sprite custom)
    {
        if (custom != null)
        {
            img.sprite = custom;
            img.type = custom.border.sqrMagnitude > 0.001f ? Image.Type.Sliced : Image.Type.Simple;
        }
        else
        {
            img.sprite = roundSprite;
            img.type = Image.Type.Sliced;
        }
    }

    GameObject MakeCard(Transform parent, float w, float h)
    {
        var card = new GameObject("Card", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        card.transform.SetParent(parent, false);
        var img = card.GetComponent<Image>();
        ApplySprite(img, panelSprite);
        if (panelSprite == null && PixelWoodUITheme.TowerSlot() != null)
        {
            PixelWoodUITheme.ApplyTowerSlot(img, 5);
        }
        else
        {
            img.color = panelSprite != null ? Color.white : (useGameTheme ? Parchment : new Color(1f, 1f, 1f, 0.88f));
        }
        var le = card.GetComponent<LayoutElement>();
        le.preferredWidth = w;
        le.preferredHeight = h;
        return card;
    }

    TMP_Text MakeText(Transform parent, string txt, float size, Color color,
                      TextAlignmentOptions align, FontStyles style = FontStyles.Normal)
    {
        var go = new GameObject("Text", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.text = UIFactory.RemoveDiacritics(txt);
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.fontStyle = style;
        t.raycastTarget = false;
        return t;
    }

    TMP_Text Label(Transform parent, string txt, float size, float w, float h,
                   TextAlignmentOptions align = TextAlignmentOptions.Center, FontStyles style = FontStyles.Normal)
    {
        TMP_Text t = MakeText(parent, txt, size, textColor, align, style);
        if (useGameTheme)
        {
            t.outlineWidth = 0.18f;
            t.outlineColor = DarkWood;
        }
        var le = t.gameObject.AddComponent<LayoutElement>();
        le.preferredWidth = w;
        le.preferredHeight = h;
        return t;
    }

    Button MakeButton(Transform parent, string label, Color color, float w, float h, float fontSize,
                      UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("Btn_" + label,
            typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement), typeof(MenuButtonFx));
        go.transform.SetParent(parent, false);

        var img = go.GetComponent<Image>();
        ApplySprite(img, buttonSprite);
        if (buttonSprite == null && PixelWoodUITheme.WideButton() != null)
        {
            PixelWoodUITheme.ApplyWideBar(img, 0);
            img.type = img.sprite.border.sqrMagnitude > 0.001f ? Image.Type.Sliced : Image.Type.Simple;
            img.color = Color.white;
        }
        else
        {
            img.color = (buttonSprite != null && !tintCustomButtons) ? Color.white : color;
        }

        var le = go.GetComponent<LayoutElement>();
        le.preferredWidth = w;
        le.preferredHeight = h;

        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        if (buttonSprite == null) PixelWoodUITheme.ApplyButton(btn, img);
        var cb = btn.colors;
        cb.highlightedColor = new Color(0.95f, 0.95f, 0.95f, 1f);
        cb.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
        btn.colors = cb;
        btn.onClick.AddListener(() =>
        {
            PlayClick();
            SoundManager.Play(Sfx.ButtonClick);
            onClick();
        });

        TMP_Text t = MakeText(go.transform, label, fontSize, buttonTextColor, TextAlignmentOptions.Center, FontStyles.Bold);
        Stretch(t.rectTransform);
        if (useGameTheme)
        {
            t.outlineWidth = 0.2f;
            t.outlineColor = DarkWood;
        }
        return btn;
    }

    TMP_InputField MakeInput(Transform parent, string placeholder, float w, float h)
    {
        var go = new GameObject("NameInput", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        go.SetActive(false);                                   // monta desativado e liga no fim

        var img = go.GetComponent<Image>();
        ApplySprite(img, inputSprite != null ? inputSprite : panelSprite);
        if (inputSprite == null && panelSprite == null && PixelWoodUITheme.WideButton() != null)
        {
            PixelWoodUITheme.ApplyWideBar(img, 0);
        }
        else img.color = (useGameTheme && inputSprite == null && panelSprite == null) ? Parchment : Color.white;
        var le = go.GetComponent<LayoutElement>();
        le.preferredWidth = w; le.preferredHeight = h;

        var vp = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        vp.transform.SetParent(go.transform, false);
        var vrt = (RectTransform)vp.transform;
        Stretch(vrt);
        vrt.offsetMin = new Vector2(24, 8);
        vrt.offsetMax = new Vector2(-24, -8);

        Color inputText = CardText();
        TMP_Text text = MakeText(vp.transform, "", 22, inputText, TextAlignmentOptions.MidlineLeft);
        Stretch(text.rectTransform);
        TMP_Text ph = MakeText(vp.transform, placeholder, 22, new Color(inputText.r, inputText.g, inputText.b, 0.45f),
                               TextAlignmentOptions.MidlineLeft, FontStyles.Italic);
        Stretch(ph.rectTransform);

        var input = go.AddComponent<TMP_InputField>();
        input.targetGraphic = img;
        input.textViewport = vrt;
        input.textComponent = text;
        input.placeholder = ph;
        input.characterLimit = 12;
        go.SetActive(true);

        input.text = PlayerPrefs.GetString("player_name", "");
        input.onValueChanged.AddListener(v => PlayerPrefs.SetString("player_name", v));
        return input;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    // ---------------------------------------------------------------- sprites gerados por código
    // retângulo arredondado (usado em 9-slice) ou círculo, se radius = size/2
    static Sprite MakeRoundedSprite(int size, int radius)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        float half = size / 2f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(Mathf.Abs(x + 0.5f - half) - (half - radius), 0f);
                float dy = Mathf.Max(Mathf.Abs(y + 0.5f - half) - (half - radius), 0f);
                float dist = Mathf.Sqrt(dx * dx + dy * dy) - radius;     // < 0 = dentro
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(0.5f - dist)));
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                             SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
    }

    // tábua de madeira em pixel art (cinza: a cor de cada botão vem do "tint")
    // borda escura, brilho em cima, sombra embaixo e veios da madeira; 9-slice com pixels grandes
    static Sprite MakeWoodSprite()
    {
        const int w = 32, h = 24, b = 6;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Point;
        Color outline = new Color(0.30f, 0.30f, 0.30f, 1f);
        Color shadow = new Color(0.70f, 0.70f, 0.70f, 1f);
        Color fill = new Color(0.90f, 0.90f, 0.90f, 1f);
        Color grain = new Color(0.82f, 0.82f, 0.82f, 1f);
        Color light = Color.white;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                bool corner = (x == 0 || x == w - 1) && (y == 0 || y == h - 1);
                Color c;
                if (corner) c = Color.clear;                                        // canto "arredondado" de pixel
                else if (x == 0 || x == w - 1 || y == 0 || y == h - 1) c = outline; // contorno
                else if (y == h - 2) c = light;                                     // brilho em cima
                else if (y <= 2) c = shadow;                                        // sombra embaixo
                else if (y == 8 || y == 15) c = grain;                              // veios da madeira
                else c = fill;
                tex.SetPixel(x, y, c);
            }
        tex.Apply();
        // 25 pixels por unidade = cada pixel da textura fica com ~4 unidades na tela (visual pixel art)
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 25f, 0,
                             SpriteMeshType.FullRect, new Vector4(b, b, b, b));
    }

    // a cena do mapa 1 se chama Map_01 (o padrão antigo era GameScene)
    void FixMapScenes()
    {
        if (mapScenes == null) return;
        for (int i = 0; i < mapScenes.Length; i++)
        {
            if (Application.CanStreamedLevelBeLoaded(mapScenes[i])) continue;
            string alt = "Map_0" + (i + 1);
            if (Application.CanStreamedLevelBeLoaded(alt)) mapScenes[i] = alt;
        }
    }

    static Sprite MakeGradient(Color top, Color bottom)
    {
        var tex = new Texture2D(2, 256, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < 256; y++)
        {
            Color c = Color.Lerp(bottom, top, y / 255f);
            tex.SetPixel(0, y, c);
            tex.SetPixel(1, y, c);
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 2, 256), new Vector2(0.5f, 0.5f), 100f);
    }
}

// Efeito de "pulinho" ao passar o mouse / clicar nos botões
public class MenuButtonFx : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public static System.Action Hovered;     // o MainMenu escuta para tocar o som de hover

    float target = 1f;
    bool over;

    public void OnPointerEnter(PointerEventData e) { over = true; target = 1.07f; if (Hovered != null) Hovered(); }
    public void OnPointerExit(PointerEventData e) { over = false; target = 1f; }
    public void OnPointerDown(PointerEventData e) { target = 0.95f; }
    public void OnPointerUp(PointerEventData e) { target = over ? 1.07f : 1f; }

    void Update()
    {
        float s = Mathf.Lerp(transform.localScale.x, target, 14f * Time.unscaledDeltaTime);
        transform.localScale = new Vector3(s, s, 1f);
    }

    void OnDisable() { transform.localScale = Vector3.one; target = 1f; over = false; }
}