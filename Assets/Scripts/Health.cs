using UnityEngine;
using UnityEngine.UI;

public class Health : MonoBehaviour
{
    public float maxHealth = 100f;
    public Image fillImage;            // arraste aqui o objeto "Fill"
    float current;                     // vida atual

    public bool IsDead { get; private set; }
    public static event System.Action OnPlayerDied;   // para a futura tela de derrota

    Animator anim;

    // Propriedade pública para ler a vida atual de forma segura
    public float CurrentHealth => current;

    void Awake()
    {
        anim = GetComponentInChildren<Animator>();   // o modelo é filho do Player
    }

    void Start()
    {
        current = maxHealth;
        UpdateBar();
    }

    // chamado pelos inimigos quando atingem o player
    public void TakeDamage(float amount)
    {
        if (IsDead) return;                                  // morto não toma dano

        current = Mathf.Max(0f, current - amount);           // nunca abaixo de 0
        UpdateBar();

        if (current <= 0f) { Die(); return; }
        if (anim != null) anim.SetTrigger("Hit");            // reação de dano
    }

    public void Heal(float amount)
    {
        if (IsDead) return;
        current = Mathf.Min(maxHealth, current + amount);    // nunca acima do máximo
        UpdateBar();
    }

    void UpdateBar()
    {
        // Se a barra antiga não estiver sendo usada com corações, protege para não dar erro
        if (fillImage != null)
        {
            float pct = current / maxHealth;                         // 0 a 1
            fillImage.fillAmount = pct;
            fillImage.color = Color.Lerp(Color.red, Color.green, pct);   // vermelho -> verde
        }
    }

    void Die()
    {
        if (IsDead) return;
        IsDead = true;

        if (anim != null)
        {
            anim.ResetTrigger("Hit");                        // evita Hit depois da morte
            anim.SetTrigger("Die");
        }

        var pc = GetComponent<PlayerController>();
        if (pc != null) pc.enabled = false;                  // trava o movimento

        Debug.Log("Player morreu");
        OnPlayerDied?.Invoke();                              // depois: tela de derrota
    }

    void Update()
    {
        // TESTE: aperte H para levar 10 de dano (apague depois)
        if (Input.GetKeyDown(KeyCode.H)) TakeDamage(10f);
    }
}