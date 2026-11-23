using System;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Vida")]
    public float maxHealth = 6f; // 6 de vida = 3 corações perfeitos
    private float currentHealth;
    public float CurrentHealth => currentHealth;

    [Header("Animator")]
    public string hitTrigger = "Hit";
    public string dieTrigger = "Die";
    public float deathDelay = 0f;

    [Header("Teste")]
    [Tooltip("Ligado: clicar no inimigo tira 2 de vida. Deixe desligado no jogo (com moedas, vira trapaça).")]
    public bool debugClickDamage = false;

    public static event Action<EnemyHealth> OnEnemyDied;
    public bool IsDead { get; private set; }

    Animator anim;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    void Start()
    {
        anim = GetComponentInChildren<Animator>();

        // Garante que todos os renderizadores visuais do monstro fiquem ativos
        foreach (var r in GetComponentsInChildren<Renderer>(true))
        {
            r.enabled = true;
            var sk = r as SkinnedMeshRenderer;
            if (sk != null) sk.updateWhenOffscreen = true;
        }
    }

    /// <summary>Muda a vida máxima e enche a vida (usado pelo EnemySpawner para fortalecer a cada onda).</summary>
    public void SetMaxHealth(float value)
    {
        maxHealth = Mathf.Max(1f, value);
        currentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        if (IsDead) return;
        currentHealth = Mathf.Max(0f, currentHealth - amount);

        if (currentHealth <= 0f) Die();
        else
        {
            SoundManager.PlayAt(Sfx.EnemyHit, transform.position);   // SOM: dano no inimigo
            if (anim != null && !string.IsNullOrEmpty(hitTrigger)) anim.SetTrigger(hitTrigger);
        }
    }

    void Die()
    {
        IsDead = true;
        SoundManager.PlayAt(Sfx.EnemyDeath, transform.position);     // SOM: morte do inimigo
        if (anim != null) anim.SetTrigger(dieTrigger);

        foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
        OnEnemyDied?.Invoke(this);
        Destroy(gameObject);
    }

    void OnMouseDown()
    {
        if (debugClickDamage) TakeDamage(2f);
    }
}