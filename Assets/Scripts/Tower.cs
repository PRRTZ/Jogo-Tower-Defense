using UnityEngine;

public class Tower : MonoBehaviour
{
    [Header("Partes do modelo")]
    public Transform weaponPivot;        // a parte que gira (modelo weapon-*)
    public Transform muzzle;             // de onde sai o projétil
    public float yawOffset = 0f;         // se a arma apontar torto, teste 90, -90 ou 180

    [Header("Detecção e ataque")]
    public float range = 8f;             // alcance em unidades do mundo (1 hexágono ≈ 2)
    public float cooldown = 1f;          // intervalo entre disparos (s)
    public float damage = 20f;
    public float splashRadius = 0f;      // > 0 = dano em área
    public float turnSpeed = 360f;       // graus por segundo
    public float aimTolerance = 12f;     // só atira quando a arma está quase alinhada
    public float retargetInterval = 0.2f;

    [Header("Projétil")]
    public Projectile projectilePrefab;
    public float projectileSpeed = 15f;
    public float arcHeight = 0f;         // > 0 = voo em arco (catapulta)

    EnemyHealth target;
    float nextShot;
    float nextScan;

    void Update()
    {
        // 1. Procura alvo de tempos em tempos (não todo frame)
        if (Time.time >= nextScan)
        {
            nextScan = Time.time + retargetInterval;
            target = FindTarget();
        }
        if (target == null) return;

        // 2. Gira a arma em direção ao alvo (só no plano horizontal)
        Vector3 flat = target.transform.position - transform.position;
        flat.y = 0f;
        float angleError = 0f;

        if (weaponPivot != null && flat.sqrMagnitude > 0.001f)
        {
            Quaternion want = Quaternion.LookRotation(flat) * Quaternion.Euler(0f, yawOffset, 0f);
            weaponPivot.rotation = Quaternion.RotateTowards(weaponPivot.rotation, want, turnSpeed * Time.deltaTime);
            angleError = Quaternion.Angle(weaponPivot.rotation, want);
        }

        // 3. Atira se o intervalo passou e a arma já está apontando
        if (Time.time >= nextShot && angleError <= aimTolerance)
        {
            Shoot();
            nextShot = Time.time + cooldown;
        }
    }

    EnemyHealth FindTarget()
    {
        // mira no inimigo dentro do alcance que está MAIS PERTO DA BASE (mais avançado no caminho),
        // para os da frente não passarem enquanto a torre atira nos de trás
        EnemyHealth best = null;
        float bestProgress = float.MinValue;
        float bestDist = float.MaxValue;
        foreach (var e in FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None))
        {
            if (e.IsDead) continue;
            float d = FlatDistance(e.transform);
            if (d > range) continue;

            var move = e.GetComponent<EnemyMovement>();
            float progress = move != null ? move.Progress : -d;   // sem movimento: o mais próximo
            if (progress > bestProgress || (Mathf.Approximately(progress, bestProgress) && d < bestDist))
            {
                best = e; bestProgress = progress; bestDist = d;
            }
        }
        return best;
    }

    float FlatDistance(Transform t)
    {
        Vector3 d = t.position - transform.position;
        d.y = 0f;                                   // ignora diferença de altura
        return d.magnitude;
    }

    void Shoot()
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning(name + ": falta o Projectile Prefab");
            return;
        }

        Vector3 pos = muzzle != null ? muzzle.position : transform.position + Vector3.up * 1.5f;
        Projectile p = Instantiate(projectilePrefab, pos, Quaternion.identity);
        p.Launch(target, damage, projectileSpeed, splashRadius, arcHeight);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, range);   // mostra o alcance
    }
}