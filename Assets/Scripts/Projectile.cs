using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
public class Projectile : MonoBehaviour
{
    public float maxLifetime = 6f;                  // segurança: some se nunca atingir nada
    public Vector3 rotationOffset = Vector3.zero;   // corrige a direção do modelo (ex.: 0,90,0)
    public GameObject hitEffectPrefab;              // efeito opcional no impacto (partículas)

    EnemyHealth target;
    Collider targetCol;
    Vector3 lastPoint;
    Vector3 startPos;
    float damage, speed, splash, arc;
    float t, duration;
    bool launched, done;

    void Awake()
    {
        // Rigidbody cinemático + trigger: gera OnTriggerEnter ao tocar nos inimigos
        var rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        GetComponent<SphereCollider>().isTrigger = true;
    }

    // chamado pela torre logo depois de criar o projétil
    public void Launch(EnemyHealth target, float damage, float speed, float splash, float arc)
    {
        this.target = target;
        this.damage = damage;
        this.speed = Mathf.Max(0.1f, speed);
        this.splash = splash;
        this.arc = arc;

        targetCol = target != null ? target.GetComponentInChildren<Collider>() : null;
        startPos = transform.position;
        lastPoint = AimPoint();
        duration = Mathf.Max(0.05f, Vector3.Distance(startPos, lastPoint) / this.speed);

        launched = true;
        Destroy(gameObject, maxLifetime);
    }

    // centro do inimigo; se ele morreu, usa o último ponto conhecido
    Vector3 AimPoint()
    {
        if (target != null && !target.IsDead)
            lastPoint = targetCol != null ? targetCol.bounds.center
                                          : target.transform.position + Vector3.up * 0.5f;
        return lastPoint;
    }

    void Update()
    {
        if (!launched || done) return;

        Vector3 aim = AimPoint();
        Vector3 prev = transform.position;

        if (arc <= 0f)
        {
            // voo reto: translação em direção ao alvo
            transform.position = Vector3.MoveTowards(prev, aim, speed * Time.deltaTime);
            if ((transform.position - aim).sqrMagnitude < 0.04f) { Hit(target); return; }
        }
        else
        {
            // voo em arco (parábola entre a origem e o alvo)
            t += Time.deltaTime / duration;
            float k = Mathf.Clamp01(t);
            transform.position = Vector3.Lerp(startPos, aim, k) + Vector3.up * (arc * 4f * k * (1f - k));
            if (t >= 1f) { Hit(target); return; }
        }

        // aponta o modelo na direção do movimento
        Vector3 dir = transform.position - prev;
        if (dir.sqrMagnitude > 1e-6f)
            transform.rotation = Quaternion.LookRotation(dir) * Quaternion.Euler(rotationOffset);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!launched || done) return;

        var e = other.GetComponentInParent<EnemyHealth>();
        if (e == null || e.IsDead) return;
        if (arc > 0f && t < 0.7f) return;      // projétil em arco só colide na descida

        Hit(e);
    }

    void Hit(EnemyHealth direct)
    {
        if (done) return;
        done = true;

        if (splash > 0f)
        {
            // dano em área: cada inimigo leva o dano uma vez
            var hit = new HashSet<EnemyHealth>();
            foreach (var col in Physics.OverlapSphere(transform.position, splash, ~0, QueryTriggerInteraction.Ignore))
            {
                var e = col.GetComponentInParent<EnemyHealth>();
                if (e != null && !e.IsDead && hit.Add(e)) e.TakeDamage(damage);
            }
        }
        else if (direct != null && !direct.IsDead)
        {
            direct.TakeDamage(damage);
        }

        if (hitEffectPrefab != null)
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }
}