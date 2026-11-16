using UnityEngine;

public class EnemyContactDamage : MonoBehaviour
{
    public float damage = 10f;           // 10 = meio coração (com maxHealth 100)
    public float reach = 0.3f;           // folga (em metros) entre as superfícies para contar como "encostou"
    public float cooldown = 1f;          // intervalo entre danos deste inimigo
    public float invulnerability = 0.5f; // folga do player depois de levar dano (todos os inimigos)
    public bool debugLog = false;        // ligue no Inspector para ver o que está bloqueando

    static float globalNext;             // compartilhado entre todos os inimigos

    // zera o static a cada Play (necessário quando o Reload Domain está desligado)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { globalNext = 0f; }

    Health playerHealth;
    Transform playerPos;
    CharacterController playerCC;
    Collider[] myColliders;
    EnemyHealth self;
    float nextHit;
    float nextLog;

    void Start()
    {
        playerHealth = FindAnyObjectByType<Health>();

        // posição: objeto com tag Player > PlayerController > o próprio Health
        var tagged = GameObject.FindGameObjectWithTag("Player");
        if (tagged != null) playerPos = tagged.transform;
        else
        {
            var pc = FindAnyObjectByType<PlayerController>();
            playerPos = pc != null ? pc.transform : (playerHealth != null ? playerHealth.transform : null);
        }

        if (playerHealth != null) playerCC = playerHealth.GetComponent<CharacterController>();

        myColliders = GetComponentsInChildren<Collider>();   // colliders deste monstro
        self = GetComponent<EnemyHealth>();

        if (playerHealth == null || playerPos == null)
            Debug.LogWarning("EnemyContactDamage: não achei o Health ou o Player na cena");
    }

    // distância entre a superfície do monstro e a superfície do player (0 = encostando)
    float SurfaceDistance()
    {
        // centro e raio do player
        Vector3 center;
        float playerRadius;
        if (playerCC != null)
        {
            center = playerCC.bounds.center;
            playerRadius = playerCC.radius;
        }
        else
        {
            center = playerPos.position + Vector3.up * 0.9f;
            playerRadius = 0.3f;
        }

        // ponto mais próximo do player em cada collider do monstro
        float best = float.MaxValue;
        foreach (var c in myColliders)
        {
            if (c == null || !c.enabled || c.isTrigger) continue;
            Vector3 p = c.ClosestPoint(center);
            float d = Vector3.Distance(p, center) - playerRadius;
            if (d < best) best = d;
        }

        // sem colliders no monstro: usa a distância entre os pivôs
        if (best == float.MaxValue)
        {
            Vector3 flat = playerPos.position - transform.position;
            flat.y = 0f;
            best = flat.magnitude - playerRadius;
        }

        return Mathf.Max(0f, best);
    }

    void Update()
    {
        if (playerHealth == null || playerPos == null) return;
        if (self != null && self.IsDead) return;

        float dist = SurfaceDistance();

        if (debugLog && Time.time >= nextLog)
        {
            nextLog = Time.time + 1f;
            Debug.Log("[Contato] distSuperficie=" + dist.ToString("F2") + " alcance=" + reach
                + " bloqueadoCooldown=" + (Time.time < nextHit)
                + " bloqueadoGlobal=" + (Time.time < globalNext)
                + " vida=" + playerHealth.CurrentHealth);
        }

        if (Time.time < nextHit || Time.time < globalNext) return;

        if (dist <= reach)
        {
            playerHealth.TakeDamage(damage);
            nextHit = Time.time + cooldown;
            globalNext = Time.time + invulnerability;
        }
    }
}