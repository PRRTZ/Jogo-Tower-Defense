using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    public float speed = 2f;
    public float turnSpeed = 540f;                 // graus por segundo
    public int skipFirstWaypoints = 0;             // pula os N primeiros Waypoints (use 0 no vilarejo)
    public bool followWaypointHeight = true;       // acompanha a altura dos pontos
    public bool debugLog = true;                   // escreve o diagnóstico no Console

    [Header("Passos")]
    public bool playFootsteps = true;
    [Min(0.1f)] public float footstepInterval = 0.5f;

    public static event Action<EnemyMovement> OnReachedBase;

    /// <summary>Quanto o monstro já andou no caminho (maior = mais perto da base). Usado pelas torres para mirar.</summary>
    public float Progress
    {
        get
        {
            if (pts.Count == 0) return 0f;
            if (index >= pts.Count) return pts.Count * 1000f;
            Vector3 d = pts[index].position - transform.position;
            d.y = 0f;
            return index * 1000f - d.magnitude;   // waypoint atual pesa mais; dentro dele, quem está mais perto do próximo ponto vem antes
        }
    }

    readonly List<Transform> pts = new List<Transform>();
    int index = 0;
    EnemyHealth health;
    float footstepTimer;

    void Start()
    {
        health = GetComponent<EnemyHealth>();

        // 1. avisa se há mais de um grupo com o mesmo nome (o Find pega só o primeiro)
        CheckDuplicates("EntrancePoints");
        CheckDuplicates("Waypoints");

        // 2. monta a lista de pontos
        AddChildren("EntrancePoints", 0);
        AddChildren("Waypoints", skipFirstWaypoints);

        if (pts.Count == 0)
        {
            Debug.LogWarning("EnemyMovement: não achei Waypoints nem EntrancePoints", this);
            enabled = false;
            return;
        }

        if (debugLog)
            Debug.Log("[Movimento] " + name + ": " + pts.Count + " pontos. Primeiro: " + pts[0].name
                + " em " + pts[0].position + " | eu estou em " + transform.position
                + " | skipFirstWaypoints=" + skipFirstWaypoints + " | speed=" + speed, this);
    }

    void CheckDuplicates(string groupName)
    {
        int count = 0;
        foreach (var t in FindObjectsByType<Transform>(FindObjectsSortMode.None))
            if (t.name == groupName) count++;
        if (count > 1)
            Debug.LogWarning("[Movimento] Existem " + count + " objetos chamados \"" + groupName + "\". O Find usa só o primeiro: apague os que sobraram.");
    }

    void AddChildren(string groupName, int skip)
    {
        var g = GameObject.Find(groupName);
        if (g == null) return;
        if (debugLog) Debug.Log("[Movimento] Grupo \"" + groupName + "\" encontrado em " + GetPath(g.transform) + " com " + g.transform.childCount + " filhos");
        for (int i = skip; i < g.transform.childCount; i++) pts.Add(g.transform.GetChild(i));
    }

    static string GetPath(Transform t)
    {
        string p = t.name;
        while (t.parent != null) { t = t.parent; p = t.name + "/" + p; }
        return p;
    }

    void Update()
    {
        if (index >= pts.Count) return;
        if (health != null && health.IsDead) return;

        Vector3 target = pts[index].position;
        if (!followWaypointHeight) target.y = transform.position.y;

        // gira em direção ao próximo ponto (só no plano horizontal)
        Vector3 flat = target - transform.position;
        flat.y = 0f;
        if (flat.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(flat), turnSpeed * Time.deltaTime);

        Vector3 previousPosition = transform.position;
        transform.position = Vector3.MoveTowards(previousPosition, target, speed * Time.deltaTime);

        if (playFootsteps && (transform.position - previousPosition).sqrMagnitude > 0.0001f)
        {
            footstepTimer -= Time.deltaTime;
            if (footstepTimer <= 0f)
            {
                SoundManager.PlayAt(Sfx.Footstep, transform.position);
                footstepTimer = footstepInterval;
            }
        }
        else footstepTimer = 0f;

        if ((transform.position - target).sqrMagnitude < 0.0001f)
        {
            index++;
            if (index >= pts.Count) ReachedBase();
        }
    }

    void ReachedBase()
    {
        if (OnReachedBase != null) OnReachedBase(this);
        Destroy(gameObject);
    }

    // com o inimigo selecionado: EntrancePoints em amarelo, Waypoints em ciano
    void OnDrawGizmosSelected()
    {
        DrawGroup("EntrancePoints", Color.yellow);
        DrawGroup("Waypoints", Color.cyan);
    }

    static void DrawGroup(string groupName, Color c)
    {
        var g = GameObject.Find(groupName);
        if (g == null) return;
        Gizmos.color = c;
        Transform prev = null;
        foreach (Transform t in g.transform)
        {
            Gizmos.DrawSphere(t.position, 0.35f);
            if (prev != null) Gizmos.DrawLine(prev.position, t.position);
            prev = t;
        }
    }
}