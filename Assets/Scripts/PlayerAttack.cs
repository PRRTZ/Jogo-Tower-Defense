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

    [Header("Ataque Carregado (Giratório)")]
    public float chargeTime = 0.5f;       // tempo segurando o botão para carregar
    public float spinDamage = 4f;         // dano do ataque giratório (2 corações)
    public float spinRange = 2.5f;        // alcance maior ao girar
    public string spinTrigger = "SpinAttack"; // nome do Trigger no Animator

    [Header("Entrada")]
    public bool useMouse = true;          // ativado para usar o clique esquerdo

    Animator anim;
    Health health;
    float nextAttack;
    float holdTimer = 0f;
    bool isHolding = false;

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
            StartCoroutine(Attack(false));
            return;
        }

        // Controle do botão esquerdo do mouse (clique vs segurar)
        if (useMouse && mouse != null)
        {
            if (mouse.leftButton.wasPressedThisFrame)
            {
                isHolding = true;
                holdTimer = 0f;
            }

            if (isHolding && mouse.leftButton.isPressed)
            {
                holdTimer += Time.deltaTime;

                // Dispara o ataque giratório assim que atingir o tempo de carga
                if (holdTimer >= chargeTime)
                {
                    isHolding = false;
                    StartCoroutine(Attack(true));
                }
            }

            // Se soltou antes de completar a carga, dá o ataque normal
            if (isHolding && mouse.leftButton.wasReleasedThisFrame)
            {
                isHolding = false;
                StartCoroutine(Attack(false));
            }
        }
    }

    IEnumerator Attack(bool isSpin)
    {
        nextAttack = Time.time + cooldown;

        if (anim != null)
        {
            anim.SetTrigger(isSpin ? spinTrigger : "Attack");
        }

        yield return new WaitForSeconds(hitDelay);
        if (health != null && health.IsDead) yield break;

        DealDamage(isSpin);
    }

    void DealDamage(bool isSpin)
    {
        var already = new HashSet<EnemyHealth>();
        Vector3 origin = transform.position + Vector3.up * 0.8f;
        float currentRange = isSpin ? spinRange : range;
        float currentDamage = isSpin ? spinDamage : damage;

        foreach (var col in Physics.OverlapSphere(origin, currentRange, ~0, QueryTriggerInteraction.Ignore))
        {
            var enemy = col.GetComponentInParent<EnemyHealth>();
            if (enemy == null || enemy.IsDead || !already.Add(enemy)) continue;

            // Se NÃO for giratório, verifica o ângulo frontal. Se for giratório, acerta em 360°
            if (!isSpin)
            {
                Vector3 to = enemy.transform.position - transform.position;
                to.y = 0f;
                if (to.sqrMagnitude > 0.001f &&
                    Vector3.Angle(transform.forward, to) > arcAngle * 0.5f) continue;
            }

            enemy.TakeDamage(currentDamage);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.8f, range);
        
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.8f, spinRange);
    }
}