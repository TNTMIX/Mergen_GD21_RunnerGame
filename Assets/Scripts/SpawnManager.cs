using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem.XR;

public class SpawnManager : MonoBehaviour
{
    public GameObject obstaclePrefab;
    private Vector3 spawnPos = new Vector3(24f, 0f, 0f);
    private PlayerController playerControllerScript;
    public float speed = 5f;
    [Tooltip("Maximum negative X offset applied to spawned obstacles. Actual offset is random in [-xNegativeSpawnOffset, 0].")]
    public float xNegativeSpawnOffset = 2f;
    [Tooltip("Minimum interval (seconds) between spawns when randomizing spawn times.")]
    public float minSpawnInterval = 0.8f;
    [Tooltip("Maximum interval (seconds) between spawns when randomizing spawn times.")]
    public float maxSpawnInterval = 2f;
    public float startDelay = 2f;
    // legacy field: kept for compatibility but not used for repeat spawning when random intervals are enabled
    public float repeatRate = 2f;

    [Header("Clustering")]
    [Tooltip("Chance (0-1) after a main spawn to spawn a small cluster of extra obstacles closer to player")] 
    [Range(0f, 1f)] public float clusterChance = 0.15f;
    [Tooltip("How many extra obstacles to spawn when a cluster is triggered")]
    public int clusterExtraCount = 1;
    [Tooltip("X offset for each extra obstacle in a cluster (negative to spawn slightly closer)")]
    public float clusterExtraOffsetX = -1f;

    private Coroutine spawnCoroutine;
    private bool isSpawning = false;
    void Start()
    {
        // Find PlayerController safely to avoid NullReferenceExceptions
        playerControllerScript = Object.FindFirstObjectByType<PlayerController>();
        if (playerControllerScript == null)
        {
            var pgo = GameObject.Find("Player");
            if (pgo != null)
                playerControllerScript = pgo.GetComponent<PlayerController>();
        }
        if (playerControllerScript == null)
            Debug.LogWarning("SpawnManager: PlayerController not found in Start; spawn attempts will wait until a PlayerController appears.");

        // Do NOT start spawning automatically — wait for the Start button to call StartSpawning()
        isSpawning = false;
    }

    public void StartSpawning()
    {
        if (isSpawning)
        {
            Debug.Log("SpawnManager: StartSpawning called but isSpawning already true.");
            return;
        }
        isSpawning = true;
        // Start coroutine loop that randomizes spawn intervals
        spawnCoroutine = StartCoroutine(SpawnLoop());
        Debug.Log($"SpawnManager: StartSpawning called — spawn loop started (startDelay={startDelay}, minInterval={minSpawnInterval}, maxInterval={maxSpawnInterval}).");

        // Spawn one immediately so the player sees movement right away after Start
        SpawnObstacle();
    }

    private IEnumerator SpawnLoop()
    {
        // initial delay
        yield return new WaitForSeconds(startDelay);
        while (isSpawning)
        {
            // Wait a randomized interval between spawns
            float wait = Mathf.Max(0.01f, Random.Range(minSpawnInterval, maxSpawnInterval));
            yield return new WaitForSeconds(wait);
            if (!isSpawning) yield break;
            SpawnObstacle();
        }
    }

    void SpawnObstacle()
    {
        if (playerControllerScript == null)
        {
            // Try to recover reference if for some reason it was lost
            var pgo = GameObject.Find("Player");
            if (pgo != null)
                playerControllerScript = pgo.GetComponent<PlayerController>();

            if (playerControllerScript == null)
            {
                Debug.LogWarning("SpawnManager.SpawnObstacle: No PlayerController found — aborting spawn.");
                return;
            }
        }

        if (!playerControllerScript.gameOver)
        {
            // Apply a slight negative X offset so obstacles can spawn slightly closer to the player (random in [-xNegativeSpawnOffset, 0])
            float offsetX = xNegativeSpawnOffset > 0f ? Random.Range(-xNegativeSpawnOffset, 0f) : 0f;
            var spawnPosition = spawnPos + new Vector3(offsetX, 0f, 0f);
            Instantiate(obstaclePrefab, spawnPosition, obstaclePrefab.transform.rotation);
            Debug.Log($"SpawnManager.SpawnObstacle: obstacle instantiated at {spawnPosition} (offsetX={offsetX}), player.gameOver=false");

            // Optional clustering to reduce large gaps: spawn a small cluster of extra obstacles slightly closer
            if (clusterExtraCount > 0 && Random.value < clusterChance)
            {
                for (int i = 0; i < clusterExtraCount; i++)
                {
                    float extraOffset = clusterExtraOffsetX * (i + 1);
                    var extraPos = spawnPosition + new Vector3(extraOffset, 0f, 0f);
                    Instantiate(obstaclePrefab, extraPos, obstaclePrefab.transform.rotation);
                    Debug.Log($"SpawnManager.SpawnObstacle: cluster extra instantiated at {extraPos} (extraIndex={i}, extraOffset={extraOffset})");
                }
            }
        }
        else
        {
            Debug.Log("SpawnManager.SpawnObstacle: player.gameOver == true — skipping spawn.");
        }
    }

    public void StopSpawning()
    {
        if (!isSpawning)
            return;
        isSpawning = false;
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
        Debug.Log("SpawnManager: StopSpawning called — spawns stopped.");
    }
}