using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    [SerializeField] Image bar;              // o objeto "Bar"
    [SerializeField] Sprite[] sprites;       // [0] cheia, [1] 75%, [2] 50%, [3] 25%, [4] 0%
    [SerializeField] TMP_Text label;         // opcional: nome + vida em número
    public string displayName = "";          // se vazio, usa o nome do prefab
    public bool hideWhenFull = true;         // só mostra a barra depois do primeiro dano
    public bool showLabelOnHover = true;     // mostra "Nome 150/500" ao passar o mouse

    EnemyHealth enemy;
    Camera cam;
    Canvas canvas;
    int lastIndex = -1;

    // calculado uma vez por frame e compartilhado por todas as barras
    static int hoverFrame = -1;
    static EnemyHealth hovered;

    void Awake()
    {
        enemy = GetComponentInParent<EnemyHealth>();
        canvas = GetComponent<Canvas>();
        cam = Camera.main;
        if (displayName == "" && enemy != null) displayName = enemy.name.Replace("(Clone)", "").Trim();
        if (label != null) label.gameObject.SetActive(false);
    }

    void Start()
    {
        if (label == null) return;
        if (UIFactory.DefaultFont != null) label.font = UIFactory.DefaultFont;
        label.fontSize = 16f;
        label.enableAutoSizing = true;
        label.fontSizeMin = 12f;
        label.fontSizeMax = 16f;
    }

    // ---- ADAPTE ESTAS DUAS LINHAS aos nomes do seu EnemyHealth ----
    float Cur() { return enemy.CurrentHealth; }
    float Max() { return enemy.maxHealth; }
    // ---------------------------------------------------------------

    void LateUpdate()
    {
        if (enemy == null) { Destroy(gameObject); return; }
        if (cam == null) cam = Camera.main;

        // sempre virada para a câmera
        if (cam != null) transform.rotation = cam.transform.rotation;

        float max = Mathf.Max(1f, Max());
        float cur = Mathf.Clamp(Cur(), 0f, max);
        float pct = cur / max;

        bool show = !enemy.IsDead && !(hideWhenFull && pct >= 0.999f);
        if (canvas != null) canvas.enabled = show;
        if (!show) return;

        // quartos arredondados para cima: 4 = cheia, 3 = 75%, 2 = 50%, 1 = 25%, 0 = vazia
        int quarters = Mathf.Clamp(Mathf.CeilToInt(pct * 4f), 0, 4);
        int index = 4 - quarters;                       // posição no array de sprites
        if (index != lastIndex && bar != null && sprites != null && index < sprites.Length)
        {
            bar.sprite = sprites[index];
            lastIndex = index;
        }

        if (label != null)
        {
            bool hover = showLabelOnHover && IsHovered();
            label.gameObject.SetActive(hover);
            if (hover) label.text = UIFactory.RemoveDiacritics(displayName) + "  "
                + Mathf.CeilToInt(cur) + "/" + Mathf.CeilToInt(max);
        }
    }

    bool IsHovered()
    {
        if (hoverFrame != Time.frameCount)
        {
            hoverFrame = Time.frameCount;
            hovered = null;
            if (cam != null && Mouse.current != null)
            {
                Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
                if (Physics.Raycast(ray, out RaycastHit hit, 300f, ~0, QueryTriggerInteraction.Ignore))
                    hovered = hit.collider.GetComponentInParent<EnemyHealth>();
            }
        }
        return hovered == enemy;
    }
}