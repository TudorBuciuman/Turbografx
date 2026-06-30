using System.Collections;
using UnityEngine;

/// <summary>
/// BombProjectile — a placed bomb that detonates after a fuse delay and fires
/// 4 wave-shaped bullets in a plus-pattern (up, down, left, right),
/// matching the Shadow Mantle Holder's bomb attack behaviour.
///
/// SETUP:
///   Attach to bomb prefab (includes sprite + Collider2D trigger + AudioSource).
///   Assign waveBulletPrefab (should itself be a Projectile with a wavy movement script).
///   Call Initialize() after Instantiate.
/// </summary>
public class BombProjectile : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The wave-bullet spawned on explosion — should have a Projectile component")]
    public GameObject waveBulletPrefab;

    [Tooltip("Optional explosion VFX prefab")]
    public GameObject explosionFXPrefab;

    [Header("Explosion")]
    public float waveBulletSpeed  = 4.5f;
    public int   waveBulletDamage = 1;

    [Header("8-Bit Indicator")]
    [Tooltip("Sprite flashes faster as fuse timer approaches zero")]
    public SpriteRenderer indicatorSprite;
    public Color          safeColor    = Color.yellow;
    public Color          dangerColor  = Color.red;

    [Header("Audio")]
    public AudioClip tickSFX;
    public AudioClip explosionSFX;

    // ─────────────────────────────────────────────
    private float       fuseTime;
    private int         damage;
    private AudioSource audioSource;
    private bool        exploded = false;

    // Plus-pattern directions
    private static readonly Vector2[] PlusDirections = new Vector2[]
    {
        Vector2.up, Vector2.down, Vector2.left, Vector2.right
    };

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    /// <summary>Called by ShadowMantleHolder after spawning the bomb.</summary>
    public void Initialize(float fuse, int bombDamage)
    {
        fuseTime = fuse;
        damage   = bombDamage;
        StartCoroutine(FuseRoutine());
    }

    // ─────────────────────────────────────────────
    //  Fuse
    // ─────────────────────────────────────────────
    private IEnumerator FuseRoutine()
    {
        float elapsed   = 0f;
        float flashRate = 0.5f;  // seconds between flashes; speeds up near end

        while (elapsed < fuseTime)
        {
            elapsed   += Time.deltaTime;
            float norm = elapsed / fuseTime; // 0 → 1

            // Accelerate flash
            flashRate = Mathf.Lerp(0.5f, 0.08f, norm);

            // Visual indicator colour
            if (indicatorSprite != null)
                indicatorSprite.color = Color.Lerp(safeColor, dangerColor, norm);

            // Tick SFX at flash rate
            if (audioSource != null && tickSFX != null && elapsed % flashRate < Time.deltaTime)
                audioSource.PlayOneShot(tickSFX);

            yield return null;
        }

        Explode();
    }

    // ─────────────────────────────────────────────
    //  Explosion
    // ─────────────────────────────────────────────
    private void Explode()
    {
        if (exploded) return;
        exploded = true;

        // Spawn explosion VFX
        if (explosionFXPrefab != null)
        {
            GameObject fx = Instantiate(explosionFXPrefab, transform.position, Quaternion.identity);
            Destroy(fx, 2f);
        }

        // Play explosion SFX
        if (audioSource != null && explosionSFX != null)
            AudioSource.PlayClipAtPoint(explosionSFX, transform.position);

        // Fire 4 wave bullets in plus-pattern
        if (waveBulletPrefab != null)
        {
            foreach (Vector2 dir in PlusDirections)
            {
                GameObject bullet = Instantiate(waveBulletPrefab, transform.position, Quaternion.identity);

                // If the bullet has a WaveProjectile component, use it; otherwise fall back to Projectile
                WaveProjectile wp = bullet.GetComponent<WaveProjectile>();
                if (wp != null)
                {
                    wp.Initialize(dir, waveBulletSpeed, damage);
                }
                else
                {
                    Projectile p = bullet.GetComponent<Projectile>();
                    if (p != null) p.Initialize(dir, waveBulletSpeed, damage);
                }
            }
        }

        // Push nearby players back (the "push" effect described in the source game)
        Collider2D[] nearby = Physics2D.OverlapCircleAll(transform.position, 1.5f);
        foreach (Collider2D col in nearby)
        {
            if (col.CompareTag("Player"))
            {
                Rigidbody2D prb = col.GetComponent<Rigidbody2D>();
                if (prb != null)
                {
                    Vector2 pushDir = ((Vector2)col.transform.position - (Vector2)transform.position).normalized;
                    prb.AddForce(pushDir * 6f, ForceMode2D.Impulse);
                }
            }
        }

        Destroy(gameObject);
    }
}
