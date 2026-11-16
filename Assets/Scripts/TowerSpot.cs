using UnityEngine;

public class TowerSpot : MonoBehaviour
{
    public Transform buildPoint;                  // opcional: ponto exato onde a base assenta
    public Vector3 buildOffset = Vector3.zero;    // ajuste fino (ex.: subir 0.2 em Y)

    [HideInInspector] public GameObject tower;    // torre construída aqui (null = vazio)

    public bool IsEmpty => tower == null;

    public Vector3 BuildPosition =>
        (buildPoint != null ? buildPoint.position : transform.position) + buildOffset;

    void OnDrawGizmos()
    {
        Gizmos.color = IsEmpty ? Color.yellow : Color.green;
        Gizmos.DrawWireSphere(BuildPosition, 0.3f);   // mostra onde a torre vai nascer
    }
}