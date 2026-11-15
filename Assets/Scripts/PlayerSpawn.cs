using System.Collections;
using UnityEngine;

public class PlayerSpawn : MonoBehaviour
{
    public Transform spawnPoint;            // arraste o PlayerSpawnPoint aqui
    public string fallbackName = "PlayerStart";

    IEnumerator Start()
    {
        yield return null;
        Transform sp = spawnPoint;
        if (sp == null) { var go = GameObject.Find(fallbackName); if (go) sp = go.transform; }
        if (sp == null) { Debug.LogWarning("PlayerSpawn: nenhum ponto de spawn"); yield break; }

        var cc = GetComponent<CharacterController>();
        var rb = GetComponent<Rigidbody>();
        if (cc) cc.enabled = false;
        transform.SetPositionAndRotation(sp.position, sp.rotation);
        if (rb && !rb.isKinematic) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
        if (cc) cc.enabled = true;
        Physics.SyncTransforms();
        Debug.Log("PlayerSpawn: movido para " + sp.name + " " + sp.position);
    }
}