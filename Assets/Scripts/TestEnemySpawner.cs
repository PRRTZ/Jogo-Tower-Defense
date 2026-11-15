using UnityEngine;
using UnityEngine.InputSystem;

public class TestEnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public Transform spawnPoint;
    public bool spawnOnStart = true;
    public float startDelay = 1f;

    void Start()
    {
        if (spawnPoint == null)
        {
            var sp = GameObject.Find("SpawnPoint");
            if (sp != null) spawnPoint = sp.transform;
        }
        if (spawnOnStart) Invoke(nameof(SpawnOne), startDelay);
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame) SpawnOne();
    }

    public void SpawnOne()
    {
        if (enemyPrefab == null || spawnPoint == null) { Debug.LogWarning("Falta prefab ou SpawnPoint"); return; }
        Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);
    }
}