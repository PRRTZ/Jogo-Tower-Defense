using UnityEngine;

public class DeathEffects : MonoBehaviour
{
    [SerializeField] private ParticleSystem deathParticles;  // arraste o Particle System aqui
    [SerializeField] private int amount = 12;                // 10 a 15 partículas bastam
    [SerializeField] private float height = 0.5f;            // altura do efeito acima do pé do inimigo

    void Awake()
    {
        if (deathParticles == null) deathParticles = GetComponent<ParticleSystem>();
    }

    void OnEnable()  => EnemyHealth.OnEnemyDied += Puff;
    void OnDisable() => EnemyHealth.OnEnemyDied -= Puff;

    void Puff(EnemyHealth e)
    {
        if (deathParticles == null || e == null) return;

        var p = new ParticleSystem.EmitParams
        {
            position = e.transform.position + Vector3.up * height,
            applyShapeToPosition = true
        };
        deathParticles.Emit(p, amount);
    }
}
