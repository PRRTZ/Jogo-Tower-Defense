using System.Collections;
using UnityEngine;

/// <summary>
/// Comportamento do chefe (adicionado sozinho pelo EnemySpawner na última onda):
///   - chama reforços de tempos em tempos (saem do ponto de spawn, com limite de reforços vivos)
///   - com pouca vida fica FURIOSO: primeiro para e ruge por alguns segundos (janela para as torres),
///     depois anda um pouco mais rápido (nunca mais que um monstro normal da onda)
/// Os números vêm da dificuldade (GameData.cs) pelo EnemySpawner.
/// </summary>
public class BossController : MonoBehaviour
{
    public bool Raged { get; private set; }
    public bool Roaring { get; private set; }

    EnemySpawner spawner;
    float escortInterval;
    int escortCount;
    float rageAt;
    float rageSpeedMultiplier;
    float rageEscortFactor = 1f;
    float rageRoarTime;
    float timer;

    EnemyHealth health;
    EnemyMovement movement;
    Animator anim;

    public void Setup(EnemySpawner owner, float interval, int count, float rageFraction, float rageSpeed,
                      float rageEscortIntervalFactor = 1f, float roarTime = 0f)
    {
        spawner = owner;
        escortInterval = Mathf.Max(1f, interval);
        escortCount = Mathf.Max(0, count);
        rageAt = rageFraction;
        rageSpeedMultiplier = Mathf.Max(1f, rageSpeed);
        rageEscortFactor = Mathf.Clamp(rageEscortIntervalFactor, 0.5f, 2f);
        rageRoarTime = Mathf.Max(0f, roarTime);
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

        // reforços (não chama enquanto ruge)
        if (!Roaring)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                timer = Raged ? escortInterval * rageEscortFactor : escortInterval;
                if (spawner != null && escortCount > 0) spawner.SpawnEscorts(escortCount);
            }
        }

        // fúria
        if (!Raged && health.CurrentHealth <= health.maxHealth * rageAt)
        {
            Raged = true;
            if (spawner != null) spawner.NotifyBossRage();
            StartCoroutine(RageRoutine());
        }
    }

    IEnumerator RageRoutine()
    {
        // ruge parado: as torres ganham alguns segundos de tiro livre
        if (rageRoarTime > 0f && movement != null)
        {
            Roaring = true;
            movement.enabled = false;
            yield return new WaitForSeconds(rageRoarTime);
            if (health == null || health.IsDead) yield break;
            movement.enabled = true;
            Roaring = false;
        }

        // depois acelera um pouco
        if (movement != null) movement.speed *= rageSpeedMultiplier;
        if (anim != null) anim.speed *= Mathf.Min(1.25f, rageSpeedMultiplier);
    }
}
