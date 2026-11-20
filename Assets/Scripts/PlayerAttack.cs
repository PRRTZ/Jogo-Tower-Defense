using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
    [Header("Ataque Normal")]
    public float damage = 2f;             // 2 = 1 coração
    public float range = 1.8f;            // alcance à frente do player (m)
    [Range(10f, 180f)]
    public float arcAngle = 100f;         // abertura do golpe normal em graus
    public float hitDelay = 0.25f;        // segundos até o golpe acertar
    public float cooldown = 0.6f;         // intervalo entre ataques

    [Header("Entrada")]
    public bool useMouse = true;          // ativado para usar o clique esquerdo

    Animator anim;
    Health health;
    float nextAttack;

    void Awake()
    {
        anim = GetComponentInChildren<Animator>();
        health = GetComponent<Health>();
    }

    void Update()
    {
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

        yield return new WaitForSeconds(hitDelay);
        if (health != null && health.IsDead) yield break;

        DealDamage();
    }

    void DealDamage()
    {
        var already = new HashSet<EnemyHealth>();
        Vector3 origin = transform.position + Vector3.up * 0.8f;

        foreach (var col in Physics.OverlapSphere(origin, range, ~0, QueryTriggerInteraction.Ignore))
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

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.8f, range);
    }
}