using System;
using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    public float speed = 2f;
    public float turnSpeed = 540f;

    // outros scripts podem escutar isso (vida da base, pontuação, dança...)
    public static event Action<EnemyMovement> OnReachedBase;

    Transform[] wps;
    int index = 0;            // começa indo para o WP_00
    EnemyHealth health;

    void Start()
    {
        health = GetComponent<EnemyHealth>();

        var root = GameObject.Find("Waypoints");
        if (root == null) { Debug.LogWarning("EnemyMovement: não achei o objeto Waypoints"); enabled = false; return; }

        wps = new Transform[root.transform.childCount];
        for (int i = 0; i < wps.Length; i++) wps[i] = root.transform.GetChild(i);
    }

    void Update()
    {
        if (wps == null || wps.Length == 0) return;
        if (health != null && health.IsDead) return;
        if (index >= wps.Length) return;

        Vector3 target = wps[index].position;
        Vector3 flat = target - transform.position;
        flat.y = 0f;
        if (flat.sqrMagnitude > 0.0001f)
        {
            Quaternion look = Quaternion.LookRotation(flat);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, look, turnSpeed * Time.deltaTime);
        }

        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

        if ((transform.position - target).sqrMagnitude < 0.0001f)
        {
            index++;
            if (index >= wps.Length) ReachedBase();
        }
    }

    void ReachedBase()
    {
        if (OnReachedBase != null) OnReachedBase(this);
        Destroy(gameObject);
    }
}