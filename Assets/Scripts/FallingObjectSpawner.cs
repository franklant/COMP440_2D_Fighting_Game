using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FallingObjectSpawner : MonoBehaviour
{
    [Header("Prefab To Spawn")]
    public GameObject fallingObjectPrefab;

    [Header("Ground Reference")]
    public Transform groundObject;

    [Header("Spawn Area Settings")]
    public float spawnHeightOffset = 2f;

    [Header("Spawn Timing Settings")]
    public bool useRandomSpawnTime = true;
    public float spawnInterval = 3f;
    public float minSpawnInterval = 1f;
    public float maxSpawnInterval = 4f;

    [Header("Multiple Spawn Settings")]
    public int minObjectsPerSpawn = 1;
    public int maxObjectsPerSpawn = 3;
    public float perObjectStagger = 0.25f;

    [Header("Spacing & Overlap Check")]
    public float minHorizontalSpacing = 1f;
    public float spawnCheckRadius = 0.45f;
    public LayerMask overlapMask = Physics2D.DefaultRaycastLayers;
    public int maxSpawnAttempts = 10;

    private float minX;
    private float maxX;
    private float spawnHeight;

    private void Start()
    {
        if (groundObject == null)
        {
            Debug.LogError("FallingObjectSpawner ERROR: You MUST assign the Ground object in the inspector.");
            return;
        }

        // ✅ Use REAL ground width from renderer bounds
        float groundWidth = groundObject.GetComponent<Renderer>().bounds.size.x;
        float halfWidth = groundWidth / 2f;
        float groundCenterX = groundObject.position.x;

        minX = groundCenterX - halfWidth;
        maxX = groundCenterX + halfWidth;

        spawnHeight = groundObject.position.y + spawnHeightOffset;

        // Start spawner
        if (useRandomSpawnTime)
            StartCoroutine(RandomSpawnLoop());
        else
            InvokeRepeating(nameof(TriggerSpawn), 1f, spawnInterval);
    }

    private IEnumerator RandomSpawnLoop()
    {
        yield return new WaitForSeconds(1f);

        while (true)
        {
            TriggerSpawn();
            float delay = Random.Range(minSpawnInterval, maxSpawnInterval);
            yield return new WaitForSeconds(delay);
        }
    }

    private void TriggerSpawn()
    {
        int count = Random.Range(minObjectsPerSpawn, maxObjectsPerSpawn + 1);
        StartCoroutine(SpawnObjectsRoutine(count));
    }

    private IEnumerator SpawnObjectsRoutine(int count)
    {
        List<float> chosenXs = new();

        for (int i = 0; i < count; i++)
        {
            Vector3 spawnPos = Vector3.zero;
            bool found = false;

            for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
            {
                float randomX = Random.Range(minX, maxX);

                bool tooClose = false;
                foreach (float usedX in chosenXs)
                {
                    if (Mathf.Abs(usedX - randomX) < minHorizontalSpacing)
                    {
                        tooClose = true;
                        break;
                    }
                }
                if (tooClose) continue;

                float jitterY = Random.Range(0f, 1f);
                Vector3 candidate = new(randomX, spawnHeight + jitterY, 0f);

                if (!Physics2D.OverlapCircle(candidate, spawnCheckRadius, overlapMask))
                {
                    spawnPos = candidate;
                    chosenXs.Add(randomX);
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                float fallbackX = Random.Range(minX, maxX);
                spawnPos = new(fallbackX, spawnHeight, 0f);
            }

            // ✅ INSTANTIATION
            Instantiate(fallingObjectPrefab, spawnPos, Quaternion.identity);

            yield return new WaitForSeconds(perObjectStagger);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (groundObject != null)
        {
            float width = groundObject.GetComponent<Renderer>().bounds.size.x;
            float half = width / 2f;

            Gizmos.color = Color.green;

            float y = groundObject.position.y + spawnHeightOffset;
            Gizmos.DrawLine(
                new Vector3(groundObject.position.x - half, y, 0),
                new Vector3(groundObject.position.x + half, y, 0)
            );
        }
    }
}