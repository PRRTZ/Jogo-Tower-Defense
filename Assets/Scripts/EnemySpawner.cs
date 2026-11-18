using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class EnemySpawner : MonoBehaviour
{
    [Header("Inimigos")]
    public GameObject[] enemyPrefabs;        // arraste TODOS os prefabs dos monstros aqui

    [Header("Local de spawn")]
    public Transform spawnPoint;             // se vazio, procura "EnemySpawnPoint" ou "SpawnPoint"

    [Header("Spawn automático (teste)")]
    public bool spawnOnStart = true;
    public float startDelay = 1f;            // espera antes do primeiro
    public float delayBetween = 1f;          // intervalo entre um monstro e outro

    int nextIndex;                           // para a tecla T alternar entre os prefabs

    void Start()
    {
        if (spawnPoint == null)
        {
            var sp = GameObject.Find("EnemySpawnPoint");
            if (sp == null) sp = GameObject.Find("SpawnPoint");
            if (sp != null) spawnPoint = sp.transform;
        }

        if (spawnOnStart) StartCoroutine(SpawnAllRoutine());
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        // T: spawna o próximo da lista (alterna entre os prefabs)
        if (kb.tKey.wasPressedThisFrame) SpawnOne(nextIndex++);

        // Y: spawna todos, um depois do outro
        if (kb.yKey.wasPressedThisFrame) StartCoroutine(SpawnAllRoutine());
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

        var e = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
        Debug.Log("Spawn: " + prefab.name + " em " + e.transform.position);
    }
}