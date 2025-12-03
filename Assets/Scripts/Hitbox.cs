using System.Collections;
using UnityEngine;

public class Hitbox : MonoBehaviour
{
    [Header("Attack Data")]
    public float damage = 50f;
    public float stun = 15f;
    public float meterGainOnHit = 10f;
    public float hitStop = 0.2f;
    public float maxOffset = 0.5f;
    private GameFeelManager gameFeel;
    private GameObject dimmerObject;
    private ScreenDimmer screenDimmer;

    [Header("Targeting")]
    public string enemyTag = "Player2";

    [Header("HitEffect")]
    public GameObject testHitEffect;

    [Header("References")]
    public FighterStatsManager myStats; // Reference to YOUR stats (to gain meter)

    public bool isAerial = false;

    // --- ONE HIT LOGIC ---
    // Prevents the hitbox from registering multiple hits in a single swing
    private bool hasHit = false; 

    void OnEnable()
    {
        // Reset the flag every time the Animation turns this object ON
        hasHit = false;
    }

    void Start()
    {
        // Attempt to find the stats manager on the parent (Gojo)
        myStats = GetComponentInParent<FighterStatsManager>();
        StartCoroutine(FindGameFeel());

        dimmerObject = GameObject.FindGameObjectWithTag("ScreenDimmer");

        if (dimmerObject == null)
        {
            Debug.LogError("Could not find dimmer object.");
        }

        screenDimmer = dimmerObject.GetComponent<ScreenDimmer>();

        if (screenDimmer == null)
        {
            Debug.LogError("Cannot find screen dimmer");
        }

        if (testHitEffect == null)
            Debug.LogError("Hit effect not instantiated.");
    }

    IEnumerator FindGameFeel()
    {
        GameObject feelManager = GameObject.FindGameObjectWithTag("GameFeel");
        gameFeel = feelManager.GetComponent<GameFeelManager>();
        yield return new WaitUntil(() => gameFeel != null);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // 1. If we already hit someone this swing, stop.
        if (hasHit) return; 

        // 2. Check if we hit the correct enemy
        if (collision.CompareTag(enemyTag) || collision.gameObject.name == enemyTag)
        {
            CameraShake shaker = Camera.main.GetComponent<CameraShake>();
            if (shaker != null) StartCoroutine(shaker.Shake(0.4f, 0.05f));
            else Debug.LogError("Shaker is null");

            //if (screenDimmer != null) screenDimmer.TriggerDim(0.1f);


            CharacterScript enemyScript = collision.GetComponentInParent<CharacterScript>();
            // Fallback: Check parent if we hit a child hurtbox
            if (enemyScript == null) enemyScript = collision.GetComponent<CharacterScript>();

            if (enemyScript != null)
            {
                // 3. Mark as hit immediately
                hasHit = true; 

                Debug.Log("HIT CONFIRMED: " + collision.name);

                // 4. Deal Damage & Stun
                enemyScript.GetHit(damage, stun, isAerial);

                // 5. Reward Meter to Attacker
                if (myStats != null)
                {
                    myStats.AddHyperMeter(meterGainOnHit);
                }

                // spawn hiteffect
                Vector3 collisionPoint = collision.ClosestPoint(transform.position);
                collisionPoint.x += Random.Range(-maxOffset, maxOffset);
                collisionPoint.y += Random.Range(-maxOffset, maxOffset);

                Instantiate(testHitEffect, collisionPoint, Quaternion.identity);

                // 6. Trigger Hitstop (Game Feel)
                // 0.08f is snappy for melee hits
                gameFeel.HitStop(hitStop); 
                //else
                // {
                //     Debug.LogWarning("And a little bit of.. SPICE");
                //     gameFeel.HitStop(0.08f);
                // }
            }
        }
    }
}