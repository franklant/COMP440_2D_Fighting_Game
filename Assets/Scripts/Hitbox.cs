using System.Collections;
using UnityEngine;
using UnityEngine.Categorization;

public class Hitbox : MonoBehaviour
{
    [Header("Attack Data")]
    public float damage = 50f;
    public float stun = 15f;
    public float meterGainOnHit = 10f;
    public float hitStop = 0.2f;
    public float maxOffset = 0.2f;
    private GameFeelManager gameFeel;
    private GameObject dimmerObject;
    private ScreenDimmer screenDimmer;

    [Header("Targeting")]
    public string enemyTag = "Player2";

    [Header("HitEffect")]
    public GameObject testHitEffect;
    public ParticleSystem hitVfx;

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

        
        if (CompareTag("Player1"))
        {
            enemyTag = "Player2";
        } 
        // else
        // {
        //     enemyTag = "Player1";
        // }
        
        
        GameObject[] vfx = GameObject.FindGameObjectsWithTag("Particles");
        
        // Safety check to prevent index out of bounds if particles aren't found
        if (vfx.Length > 0)
        {
            if (CompareTag("Kick") && vfx.Length > 0)
            {
                hitVfx = vfx[0].GetComponent<ParticleSystem>();
            } 
            else if (vfx.Length > 1)
            {
                hitVfx = vfx[1].GetComponent<ParticleSystem>();
            }
        }
    }

    IEnumerator FindGameFeel()
    {
        GameObject feelManager = GameObject.FindGameObjectWithTag("GameFeel");
        if(feelManager != null) 
            gameFeel = feelManager.GetComponent<GameFeelManager>();
        yield return new WaitUntil(() => gameFeel != null);
    }

    void EmitParticle(Vector3 position)
    {
        if (hitVfx != null)
        {
            hitVfx.gameObject.transform.position = position;
            hitVfx.Play();
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // 1. If we already hit someone this swing, stop.
        if (hasHit) return; 

        // 2. Check if we hit the correct enemy
        if (collision.CompareTag(enemyTag) || collision.gameObject.name == enemyTag)
        {
            // --- VFX SECTION ---
            CameraShake shaker = Camera.main.GetComponent<CameraShake>();
            if (shaker != null) StartCoroutine(shaker.Shake(0.4f, 0.05f));
            // else Debug.LogError("Shaker is null");

            Vector3 collisionPoint = collision.ClosestPoint(transform.position);
            collisionPoint.x += Random.Range(-maxOffset, maxOffset);
            collisionPoint.y += Random.Range(-maxOffset, maxOffset);

            EmitParticle(collisionPoint);

            // --- DAMAGE LOGIC ---
            bool hitConfirmed = false;

            // CHECK 1: Standard Character Script
            CharacterScript enemyScript = collision.GetComponentInParent<CharacterScript>();
            if (enemyScript == null) enemyScript = collision.GetComponent<CharacterScript>();

            if (enemyScript != null)
            {
                enemyScript.GetHit(damage, stun, isAerial);
                hitConfirmed = true;
                Debug.Log("HIT CONFIRMED (Standard): " + collision.name);
            }
            // CHECK 2: Luffy Combat Controller (If standard check failed)
            else 
            {
                LuffyCombatController luffyScript = collision.GetComponentInParent<LuffyCombatController>();
                if (luffyScript == null) luffyScript = collision.GetComponent<LuffyCombatController>();

                if (luffyScript != null)
                {
                    luffyScript.GetHit(damage, stun, isAerial);
                    hitConfirmed = true;
                    Debug.Log("HIT CONFIRMED (Luffy): " + collision.name);
                }
            }

            // --- COMMON SUCCESS LOGIC ---
            if (hitConfirmed)
            {
                // 3. Mark as hit immediately
                hasHit = true; 

                // 4. Reward Meter to Attacker
                if (myStats != null)
                {
                    myStats.AddHyperMeter(meterGainOnHit);
                }

                // 5. Trigger Hitstop (Game Feel)
                if (gameFeel != null)
                {
                    gameFeel.HitStop(hitStop);
                }
            }
        } 
    }
}