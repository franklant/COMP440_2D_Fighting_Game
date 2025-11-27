using System.Collections;
using UnityEngine;

public class ToadHelperScript : MonoBehaviour
{
    [Header("--- Target & Firing ---")]
    public Transform enemyTarget;          // Set via Initialize() from CharacterScript
    public GameObject fireProjectilePrefab;
    public Transform firePoint;            // Where the fireball spawns from

    [Header("--- Fire Settings ---")]
    public float fireInterval = 1.5f;      // Time between fireballs
    public float projectileDamage = 100f;  // Damage per fireball

    private SpriteRenderer mySpriteRenderer;
    private bool hasStartedFiring = false;

    private void Awake()
    {
        // Grab sprite renderer from self or children
        mySpriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void Start()
    {
        // If enemyTarget was already set in the inspector, we can start firing
        if (enemyTarget != null && !hasStartedFiring)
        {
            StartCoroutine(FireLoop());
            hasStartedFiring = true;
        }
    }

    /// <summary>
    /// Called right after the Toad is spawned by CharacterScript.
    /// </summary>
    public void Initialize(Transform target)
    {
        enemyTarget = target;

        if (!hasStartedFiring && gameObject.activeInHierarchy)
        {
            StartCoroutine(FireLoop());
            hasStartedFiring = true;
        }
    }

    private void Update()
    {
        // Toad does NOT move. Only turn to face the enemy visually.
        if (enemyTarget != null && mySpriteRenderer != null)
        {
            bool faceLeft = enemyTarget.position.x < transform.position.x;
            mySpriteRenderer.flipX = faceLeft;
        }
    }

    private IEnumerator FireLoop()
    {
        // Optional short delay before first shot
        yield return new WaitForSeconds(0.3f);

        while (true)
        {
            ShootFire();
            yield return new WaitForSeconds(fireInterval);
        }
    }

    private void ShootFire()
    {
        if (fireProjectilePrefab == null || firePoint == null)
        {
            Debug.LogWarning("ToadHelperScript: Missing fireProjectilePrefab or firePoint.");
            return;
        }

        // Spawn projectile
        GameObject proj = Instantiate(fireProjectilePrefab, firePoint.position, Quaternion.identity);

        // Set damage if your projectile uses ProjectileController like your other moves
        ProjectileController pc = proj.GetComponent<ProjectileController>();
        if (pc != null)
        {
            pc.damage = projectileDamage;
        }

        // Decide direction based on enemy position if available
        bool shootLeft = false;

        if (enemyTarget != null)
        {
            shootLeft = enemyTarget.position.x < transform.position.x;
        }
        else if (mySpriteRenderer != null)
        {
            // Fall back to facing direction if no target
            shootLeft = mySpriteRenderer.flipX;
        }

        if (shootLeft)
        {
            // Flip projectile horizontally
            proj.transform.Rotate(0f, 180f, 0f);
        }
    }
}
