using UnityEngine;
using UnityEngine.UI;

public class Health : MonoBehaviour
{
    public float maxHealth = 100f;
    public Image fillImage;          // arraste aqui o objeto "Fill"
    float current;                   // vida atual

    void Start()
    {
        current = maxHealth;
        UpdateBar();
    }

    // chamado pelos inimigos quando atingem o player
    public void TakeDamage(float amount)
    {
        current = Mathf.Max(0f, current - amount);   // nunca abaixo de 0
        UpdateBar();
        if (current <= 0f) Die();
    }

    public void Heal(float amount)
    {
        current = Mathf.Min(maxHealth, current + amount);   // nunca acima do máximo
        UpdateBar();
    }

    void UpdateBar()
    {
        float pct = current / maxHealth;                      // 0 a 1
        fillImage.fillAmount = pct;
        fillImage.color = Color.Lerp(Color.red, Color.green, pct);   // vermelho -> verde
    }

    void Die()
    {
        Debug.Log("Player morreu");   // depois: tela de derrota
    }

    void Update()
    {
        // TESTE: aperte H para levar 10 de dano (apague depois)
        if (Input.GetKeyDown(KeyCode.H)) TakeDamage(10f);
    }
}