using System;
using UnityEngine;
using UnityEngine.UI;

public class EnemyHealth : MonoBehaviour
{
    [Header("Vida")]
    public float maxHealth = 50f;

    [Header("Barra (Canvas World Space)")]
    public Canvas barCanvas;            // achado sozinho se ficar vazio
    public Image fillImage;             // achado sozinho se ficar vazio
    public bool hideWhenFull = true;    // só mostra a barra depois do primeiro dano
    public float barMargin = 0.3f;      // distância da barra acima do topo do modelo

    [Header("Animator (nomes dos Triggers)")]
    public string hitTrigger = "Hit";
    public string dieTrigger = "Die";
    public float deathDelay = 2f;

    public static event Action<EnemyHealth> OnEnemyDied;
    public bool IsDead { get; private set; }

    float current;
    Animator anim;
    Camera cam;
    Renderer[] renderers;               // para medir a altura do modelo

    void Awake()
    {
        // acha a barra sozinho, sem precisar arrastar em cada monstro
        if (barCanvas == null) barCanvas = GetComponentInChildren<Canvas>(true);
        if (fillImage == null && barCanvas != null)
        {
            var t = barCanvas.transform.Find("Background/Fill");
            if (t != null) fillImage = t.GetComponent<Image>();
        }
    }

    void Start()
    {
        current = maxHealth;
        anim = GetComponentInChildren<Animator>();
        cam = Camera.main;
        renderers = GetComponentsInChildren<Renderer>();
        // faz o bounds acompanhar a animação (pulos, etc.)
        foreach (var r in renderers)
        {
            var sk = r as SkinnedMeshRenderer;
            if (sk != null) sk.updateWhenOffscreen = true;
        }
        UpdateBar();
    }

    public void TakeDamage(float amount)
    {
        if (IsDead) return;
        current = Mathf.Max(0f, current - amount);
        UpdateBar();

        if (current <= 0f) Die();
        else if (anim != null && !string.IsNullOrEmpty(hitTrigger)) anim.SetTrigger(hitTrigger);
    }

    void Die()
    {
        IsDead = true;
        if (anim != null) anim.SetTrigger(dieTrigger);
        if (barCanvas != null) barCanvas.gameObject.SetActive(false);
        foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
        OnEnemyDied?.Invoke(this);
        Destroy(gameObject, deathDelay);
    }

    void UpdateBar()
    {
        float pct = current / maxHealth;
        if (fillImage != null)
        {
            fillImage.fillAmount = pct;
            fillImage.color = Color.Lerp(Color.red, Color.green, pct);
        }
        if (barCanvas != null && hideWhenFull)
            barCanvas.gameObject.SetActive(pct < 1f && !IsDead);
    }

    // altura do ponto mais alto do modelo neste quadro
    float TopY()
    {
        float y = transform.position.y;
        foreach (var r in renderers)
            if (r != null && r.enabled) y = Mathf.Max(y, r.bounds.max.y);
        return y;
    }

    // a barra fica acima do topo do modelo (acompanha o pulinho) e olha para a câmera
    void LateUpdate()
    {
        if (barCanvas == null || cam == null || renderers == null) return;
        Vector3 p = transform.position;
        p.y = TopY() + barMargin;
        barCanvas.transform.position = p;
        barCanvas.transform.rotation = cam.transform.rotation;
    }

    // TESTE: clique no monstro na janela Game (em Play) para tirar 10 de vida
    void OnMouseDown() { TakeDamage(10f); }
}