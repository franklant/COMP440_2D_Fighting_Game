using System.Collections;
using UnityEngine;

public class SummonToad : MonoBehaviour
{
    [Header("Toad Settings")]
    public GameObject toadObject;            // Existing toad in scene (start disabled)
    public Transform enemyTarget;            // Who the toad shoots at
    public float toadVerticalOffset = 0.5f;  // How far below the player it spawns
    public float toadLifetime = 15f;         // Duration the toad stays active

    private bool toadActive = false;

    void Start()
    {
        // Hide the toad at the beginning
        if (toadObject != null)
        {
            toadObject.SetActive(false);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            SummonToadUnderPlayer();
        }
    }

    private void SummonToadUnderPlayer()
    {
        if (toadObject == null)
        {
            Debug.LogWarning("SummonToad ERROR: No toadObject assigned!");
            return;
        }

        // Prevent duplicate summons
        if (toadActive) return;

        // Position toad under the player
        Vector3 spawnPos = new Vector3(
            transform.position.x,
            transform.position.y - toadVerticalOffset,
            transform.position.z
        );

        toadObject.transform.position = spawnPos;
        toadObject.SetActive(true);
        toadActive = true;

        // Initialize fire behavior
        ToadHelperScript helper = toadObject.GetComponent<ToadHelperScript>();
        if (helper != null)
        {
            helper.Initialize(enemyTarget);
        }

        StartCoroutine(ToadLifetimeRoutine());
    }

    private IEnumerator ToadLifetimeRoutine()
    {
        yield return new WaitForSeconds(toadLifetime);

        if (toadObject != null)
        {
            toadObject.SetActive(false);
        }

        toadActive = false;
    }
}
