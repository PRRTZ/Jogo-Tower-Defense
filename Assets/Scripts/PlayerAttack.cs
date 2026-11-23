using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
    [Header("Ataque Normal")]
    public float damage = 2f;             // 2 = 1 coração
    // nome novo de propósito: o alcance antigo (1.8) salvo no prefab e nas cenas deixa de valer
    // e os dois mapas passam a usar este valor
    public float attackRange = 2.8f;      // alcance à frente do player (m)
    [Range(10f, 180f)]
    public float arcAngle = 100f;         // abertura do golpe normal em graus
    public float hitDelay = 0.25f;        // segundos até o golpe acertar
    public float cooldown = 0.6f;         // intervalo entre ataques

    [Header("Entrada")]
    public bool useMouse = true;          // ativado para usar o clique esquerdo

    [Header("Efeito do golpe")]
    public bool showSlash = true;
    public Color slashColor = new Color(1f, 1f, 1f, 0.85f);
    public float slashHeight = 0.6f;      // altura do rastro acima do chão
    public float slashSweep = 0.08f;      // tempo da varredura (s)
    public float slashFade = 0.15f;       // tempo para sumir (s)
    [Tooltip("Opcional. Vazio = material transparente automático (URP Particles/Unlit).")]
    public Material slashMaterial;

    Animator anim;
    Health health;
    float nextAttack;
    SlashEffect slash;

    void Awake()
    {
        anim = GetComponentInChildren<Animator>();
        health = GetComponent<Health>();
    }

    void Update()
    {
        if (PauseSettingsMenu.IsOpen) return;
        if (health != null && health.IsDead) return;
        if (Time.time < nextAttack) return;

        var kb = Keyboard.current;
        var mouse = Mouse.current;

        // Ataque rápido direto pela tecla J
        if (kb != null && kb.jKey.wasPressedThisFrame)
        {
            StartCoroutine(Attack());
            return;
        }

        // Ataque direto pelo botão esquerdo do mouse
        if (useMouse && mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            StartCoroutine(Attack());
        }
    }

    IEnumerator Attack()
    {
        nextAttack = Time.time + cooldown;

        if (anim != null)
        {
            anim.SetTrigger("Attack");
        }

        SoundManager.Play(Sfx.PlayerAttack);                 // SOM: golpe da espada

        yield return new WaitForSeconds(hitDelay);
        if (health != null && health.IsDead) yield break;

        ShowSlash();
        DealDamage();
    }

    void DealDamage()
    {
        var already = new HashSet<EnemyHealth>();
        Vector3 origin = transform.position + Vector3.up * 0.8f;

        foreach (var col in Physics.OverlapSphere(origin, attackRange, ~0, QueryTriggerInteraction.Ignore))
        {
            var enemy = col.GetComponentInParent<EnemyHealth>();
            if (enemy == null || enemy.IsDead || !already.Add(enemy)) continue;

            Vector3 to = enemy.transform.position - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude > 0.001f &&
                Vector3.Angle(transform.forward, to) > arcAngle * 0.5f) continue;

            enemy.TakeDamage(damage);
        }
    }

    void ShowSlash()
    {
        if (!showSlash) return;
        if (slash == null) slash = SlashEffect.Create(slashMaterial);   // criado uma vez e reaproveitado

        Quaternion yaw = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        slash.Play(transform.position + Vector3.up * slashHeight, yaw, attackRange, arcAngle,
                   slashColor, slashSweep, slashFade);
    }

    void OnDestroy()
    {
        if (slash != null) Destroy(slash.gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.8f, attackRange);
    }
}