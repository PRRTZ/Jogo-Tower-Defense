using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.UI;

public class TestEnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public Transform spawnPoint;
    public bool spawnOnStart = true;
    public float startDelay = 1f;
    public float wavesDelay = 10f;
    private float countdown = 2f;
    private int waveNum = 0;

    IEnumerator SpawnWave()
    {
        waveNum++;
        for (int i = 0; i < waveNum; i++)
        {
            SpawnOne();
            yield return new WaitForSeconds(0.8f);
        }
    }
           
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
        if (countdown <= 0f)
        {
            StartCoroutine(SpawnWave());
            countdown = wavesDelay;
        }
        countdown -= Time.deltaTime;
        
        if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame) 
        {
            SpawnOne();
        }
    }

    public void SpawnOne()
    {
        if (enemyPrefab == null || spawnPoint == null) 
        { 
            Debug.LogWarning("Falta prefab ou SpawnPoint"); 
            return; 
        }
        Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);
    }
}