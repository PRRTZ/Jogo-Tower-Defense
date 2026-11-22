using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Funções auxiliares para criar a UI por código (Canvas, painéis, textos e botões).
/// Assim os painéis de moeda, loja e upgrade funcionam sem montar nada na mão no Canvas.
/// </summary>
public static class UIFactory
{
    public static readonly Color PanelColor     = new Color(0.08f, 0.09f, 0.12f, 0.90f);
    public static readonly Color ButtonColor    = new Color(0.22f, 0.50f, 0.28f, 1f);
    public static readonly Color ButtonAltColor = new Color(0.55f, 0.24f, 0.20f, 1f);
    public static readonly Color ButtonNeutral  = new Color(0.25f, 0.27f, 0.32f, 1f);
    public static readonly Color Gold           = new Color(1f, 0.84f, 0.25f, 1f);

    public static Canvas CreateCanvas(string name, int sortingOrder)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    /// <summary>
    /// Painel com layout vertical que cresce conforme o conteúdo.
    /// anchor (0.5, 0) = embaixo no centro; (1, 1) = canto superior direito; etc.
    /// </summary>
    public static RectTransform CreatePanel(Transform parent, string name, float width, Vector2 anchor, Vector2 anchoredPosition)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = new Vector2(width, 0f);

        go.GetComponent<Image>().color = PanelColor;

        var layout = go.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(18, 18, 14, 16);
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        var fitter = go.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        return rt;
    }

    public static TextMeshProUGUI CreateText(Transform parent, string text, float fontSize,
                                             TextAlignmentOptions alignment = TextAlignmentOptions.Center)
    {
        var go = new GameObject("Texto", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);

        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = Color.white;
        tmp.richText = true;
        tmp.raycastTarget = false;
        return tmp;
    }

    public static Button CreateButton(Transform parent, string label, UnityAction onClick, Color color,
                                      out TextMeshProUGUI labelText, float height = 46f)
    {
        var go = new GameObject("Botao", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(parent, false);

        go.GetComponent<Image>().color = color;
        go.GetComponent<LayoutElement>().preferredHeight = height;

        var button = go.GetComponent<Button>();
        var colors = button.colors;
        colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
        colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.6f);
        button.colors = colors;
        if (onClick != null) button.onClick.AddListener(onClick);

        labelText = CreateText(go.transform, label, 22f);
        var rt = labelText.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(8f, 0f);
        rt.offsetMax = new Vector2(-8f, 0f);

        return button;
    }
}
