using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class EnemySpawner : MonoBehaviour
{
    [Header("Inimigos")]
    public GameObject[] enemyPrefabs;        // arraste TODOS os prefabs dos monstros aqui

    [Header("Local de spawn")]
    public Transform spawnPoint;             // se vazio, procura "EnemySpawnPoint" ou "SpawnPoint"

    [Header("Spawn")]
    public bool spawnOnStart = true;
    public float startDelay = 1f;            // espera antes do primeiro
    public float delayBetween = 0.8f;        // intervalo entre um monstro e outro
    public float wavesDelay = 10f;           // intervalo entre as ondas

    private float countdown = 2f;
    private int waveNum = 0;
    //private int maxWave = 7;

    int nextIndex;                           // alterna entre os prefabs da lista

    void Start()
    {
        if (spawnPoint == null)
        {
            var sp = GameObject.Find("EnemySpawnPoint");
            if (sp == null) sp = GameObject.Find("SpawnPoint");
            if (sp != null) spawnPoint = sp.transform;
        }

        if (spawnOnStart) Invoke(nameof(SpawnNext), startDelay);
    }

    void Update()
    {
        if (countdown <= 0f)
        {
            StartCoroutine(SpawnWave());
            countdown = wavesDelay;
        }
        countdown -= Time.deltaTime;

        var kb = Keyboard.current;
        if (kb == null) return;

        // T: spawna o próximo da lista (alterna entre os prefabs)
        if (kb.tKey.wasPressedThisFrame) SpawnNext();

        // Y: spawna todos, um depois do outro
        if (kb.yKey.wasPressedThisFrame) StartCoroutine(SpawnAllRoutine());
    }

    IEnumerator SpawnWave()
    {
        waveNum++;
        for (int i = 0; i < waveNum; i++)
        {
            SpawnNext();
            yield return new WaitForSeconds(delayBetween);
        }
    }

    IEnumerator SpawnAllRoutine()
    {
        yield return new WaitForSeconds(startDelay);
        for (int i = 0; i < enemyPrefabs.Length; i++)
        {
            SpawnOne(i);
            yield return new WaitForSeconds(delayBetween);
        }
    }

    public void SpawnNext()
    {
        SpawnOne(nextIndex++);
    }

    // index é circular: se passar do tamanho da lista, volta ao início
    public void SpawnOne(int index)
    {
        if (spawnPoint == null || enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            Debug.LogWarning("Falta prefab ou SpawnPoint");
            return;
        }

        var prefab = enemyPrefabs[index % enemyPrefabs.Length];
        if (prefab == null) return;

        Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
    }
}