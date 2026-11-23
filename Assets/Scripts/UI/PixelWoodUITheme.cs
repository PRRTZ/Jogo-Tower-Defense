using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Acesso aos sprites já fatiados do pack Pixel Wood UI.
/// As cópias em Resources mantêm as mesmas fatias do pack original e permitem
/// que as interfaces geradas em tempo de execução usem os sprites sem referências manuais.
/// </summary>
public static class PixelWoodUITheme
{
    private const string ResourceFolder = "PixelWoodUI/";
    private static readonly Dictionary<string, Sprite[]> Cache = new Dictionary<string, Sprite[]>();

    private static Sprite Get(string sheet, int index)
    {
        if (!Cache.TryGetValue(sheet, out Sprite[] sprites))
        {
            sprites = Resources.LoadAll<Sprite>(ResourceFolder + sheet);
            Cache[sheet] = sprites;
        }

        string expected = sheet + "_" + index;
        foreach (Sprite sprite in sprites)
        {
            if (sprite != null && sprite.name == expected)
                return sprite;
        }

        return null;
    }

    public static Sprite WideButton(int index = 0)
        => Get("1_wide_wooden_buttons_32x16_100_", index);

    public static Sprite TowerSlot(int index = 5)
        => Get("1_inventory2_100_", index);

    public static Sprite InventoryBar()
        => Get("1_inventory2_100_", 0);

    // O sprite de cantos vem em quatro peças separadas. Este conjunto usa a mesma cor do botão 0.
    public static Sprite SelectorCorner(int corner)
    {
        int[] indices = { 0, 1, 4, 5 }; // superior esquerdo, superior direito, inferior esquerdo, inferior direito
        return Get("1_wood_selector_100_", indices[Mathf.Clamp(corner, 0, 3)]);
    }

    public static Sprite FullHeart() => Get("heart_and_coin_100_", 18);
    public static Sprite HalfHeart() => Get("heart_and_coin_100_", 20);
    public static Sprite EmptyHeart() => Get("heart_and_coin_100_", 16);
    public static Sprite Coin() => Get("heart_and_coin_100_", 12);

    public static void ApplyWideBar(Image image, int variant = 0)
    {
        if (image == null) return;
        Sprite sprite = WideButton(variant);
        if (sprite == null) return;

        image.sprite = sprite;
        image.type = sprite.border.sqrMagnitude > 0.001f ? Image.Type.Sliced : Image.Type.Simple;
        image.preserveAspect = false;
        image.color = Color.white;
    }

    public static void ApplyTowerSlot(Image image, int index = 5)
    {
        if (image == null) return;
        Sprite sprite = TowerSlot(index);
        if (sprite == null) return;

        image.sprite = sprite;
        image.type = sprite.border.sqrMagnitude > 0.001f ? Image.Type.Sliced : Image.Type.Simple;
        image.preserveAspect = false;
        image.color = Color.white;
    }

    public static void ApplyButton(Button button, Image image)
    {
        if (button == null || image == null || WideButton() == null) return;

        button.targetGraphic = image;
        button.transition = Selectable.Transition.SpriteSwap;
        SpriteState state = button.spriteState;
        state.highlightedSprite = WideButton(1);
        state.pressedSprite = WideButton(1);
        state.selectedSprite = WideButton(1);
        state.disabledSprite = WideButton(1);
        button.spriteState = state;
    }

    /// <summary>Cria quatro cantos decorativos sobre um card de torre.</summary>
    public static GameObject CreateSelectionFrame(Transform parent)
    {
        if (parent == null || SelectorCorner(0) == null) return null;

        var frame = new GameObject("SelectionFrame", typeof(RectTransform), typeof(LayoutElement));
        frame.transform.SetParent(parent, false);
        RectTransform frameRect = (RectTransform)frame.transform;
        frameRect.anchorMin = Vector2.zero;
        frameRect.anchorMax = Vector2.one;
        frameRect.offsetMin = Vector2.zero;
        frameRect.offsetMax = Vector2.zero;
        frame.GetComponent<LayoutElement>().ignoreLayout = true;

        CreateCorner(frame.transform, "TopLeft", 0, new Vector2(0f, 1f), new Vector2(0f, 1f));
        CreateCorner(frame.transform, "TopRight", 1, new Vector2(1f, 1f), new Vector2(1f, 1f));
        CreateCorner(frame.transform, "BottomLeft", 2, new Vector2(0f, 0f), new Vector2(0f, 0f));
        CreateCorner(frame.transform, "BottomRight", 3, new Vector2(1f, 0f), new Vector2(1f, 0f));
        frame.SetActive(false);
        return frame;
    }

    private static void CreateCorner(Transform parent, string name, int corner, Vector2 anchor, Vector2 pivot)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(20f, 20f);

        Image image = go.GetComponent<Image>();
        image.sprite = SelectorCorner(corner);
        image.preserveAspect = true;
        image.raycastTarget = false;
    }

    public static void Stretch(RectTransform rect, float inset = 0f)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
    }
}
