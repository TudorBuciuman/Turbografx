using UnityEngine;

/// <summary>
/// Projectile — generic travelling bullet (used for Shadow Mantle's hexagonal orbs).
///
/// SETUP:
///   Attach to any projectile prefab.
///   Requires a Collider2D (trigger) and a Rigidbody2D (kinematic).
///   Call Initialize() immediately after Instantiate to set direction, speed and damage.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour
{
    [Header("Settings (overridden at runtime by Initialize)")]
    public float   speed          = 5f;
    public int     damage         = 1;
    public float   lifetime       = 6f;

    [Header("Visual")]
    [Tooltip("Optional: sprite rotates to face travel direction")]
    public bool faceDirection     = true;

    [Header("8-Bit Pixel Effect")]
    [Tooltip("If true, movement snaps to a pixel grid each frame")]
    public bool  pixelSnap        = false;
    public float pixelsPerUnit    = 16f;

    // ─────────────────────────────────────────────
    private Vector2       moveDir;
    private Rigidbody2D   rb;
    private bool          initialised = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.isKinematic  = true;
    }

    void Start()
    {
        // Auto-destroy after lifetime
        Destroy(gameObject, lifetime);
    }

    /// <summary>Called by ShadowMantleHolder immediately after spawning.</summary>
    public void Initialize(Vector2 direction, float bulletSpeed, int bulletDamage)
    {
        moveDir     = direction.normalized;
        speed       = bulletSpeed;
        damage      = bulletDamage;
        initialised = true;

        if (faceDirection && direction != Vector2.zero)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    void Update()
    {
        if (!initialised) return;

        Vector3 newPos = transform.position + (Vector3)(moveDir * speed * Time.deltaTime);

        if (pixelSnap)
        {
            float ppu = pixelsPerUnit > 0f ? pixelsPerUnit : 16f;
            newPos.x = Mathf.Round(newPos.x * ppu) / ppu;
            newPos.y = Mathf.Round(newPos.y * ppu) / ppu;
        }

        transform.position = newPos;
    }

    // ─────────────────────────────────────────────
    //  Collision
    // ─────────────────────────────────────────────
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerHealth ph = other.GetComponent<PlayerHealth>();
            if (ph != null) ph.TakeDamage(damage);
            Destroy(gameObject);
        }

        // Destroyed by walls / arena bounds
        if (other.CompareTag("Wall"))
            Destroy(gameObject);
    }
}
