using UnityEngine;

/// <summary>
/// Comportamento do chefe (adicionado sozinho pelo EnemySpawner na última onda):
///   - chama reforços de tempos em tempos (saem do ponto de spawn)
///   - com metade da vida fica FURIOSO: anda mais rápido e chama reforços com mais frequência
/// </summary>
public class BossController : MonoBehaviour
{
    public bool Raged { get; private set; }

    EnemySpawner spawner;
    float escortInterval;
    int escortCount;
    float rageAt;
    float rageSpeedMultiplier;
    float timer;

    EnemyHealth health;
    EnemyMovement movement;
    Animator anim;

    public void Setup(EnemySpawner owner, float interval, int count, float rageFraction, float rageSpeed)
    {
        spawner = owner;
        escortInterval = Mathf.Max(1f, interval);
        escortCount = Mathf.Max(0, count);
        rageAt = rageFraction;
        rageSpeedMultiplier = rageSpeed;
        timer = escortInterval;
    }

    void Start()
    {
        health = GetComponent<EnemyHealth>();
        movement = GetComponent<EnemyMovement>();
        anim = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (health == null || health.IsDead) return;

        // reforços
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            timer = Raged ? escortInterval * 0.6f : escortInterval;
            if (spawner != null && escortCount > 0) spawner.SpawnEscorts(escortCount);
        }

        // fúria
        if (!Raged && health.CurrentHealth <= health.maxHealth * rageAt)
        {
            Raged = true;
            if (movement != null) movement.speed *= rageSpeedMultiplier;
            if (anim != null) anim.speed *= 1.25f;
            if (spawner != null) spawner.NotifyBossRage();
        }
    }
}
