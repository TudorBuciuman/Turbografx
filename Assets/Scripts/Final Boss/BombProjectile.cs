using System.Collections;
using UnityEngine;

public class BombProjectile : MonoBehaviour
{
    public GameObject waveBulletPrefab;

    public GameObject explosionFXPrefab;

    public float waveBulletSpeed  = 4.5f;
    public int   waveBulletDamage = 1;

    public SpriteRenderer indicatorSprite;
    public Color safeColor = Color.yellow;
    public Color dangerColor = Color.red;

    public AudioClip tickSFX;
    public AudioClip explosionSFX;

    private float fuseTime;
    private int damage;
    private AudioSource audioSource;
    private bool exploded = false;

    private static readonly Vector2[] PlusDirections = new Vector2[]
    {
        Vector2.up, Vector2.down, Vector2.left, Vector2.right
    };

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void Initialize(float fuse, int bombDamage)
    {
        fuseTime = fuse;
        damage   = bombDamage;
        StartCoroutine(FuseRoutine());
    }

    private IEnumerator FuseRoutine()
    {
        float elapsed   = 0f;
        float flashRate = 0.5f;  

        while (elapsed < fuseTime)
        {
            elapsed   += Time.deltaTime;
            float norm = elapsed / fuseTime; 

            flashRate = Mathf.Lerp(0.5f, 0.08f, norm);

            if (indicatorSprite != null)
                indicatorSprite.color = Color.Lerp(safeColor, dangerColor, norm);

            if (audioSource != null && tickSFX != null && elapsed % flashRate < Time.deltaTime)
                audioSource.PlayOneShot(tickSFX);

            yield return null;
        }

        Explode();
    }
    private void Explode()
    {
        if (exploded) return;
        exploded = true;

        if (explosionFXPrefab != null)
        {
            GameObject fx = Instantiate(explosionFXPrefab, transform.position, Quaternion.identity);
            Destroy(fx, 2f);
        }

        if (audioSource != null && explosionSFX != null)
            AudioSource.PlayClipAtPoint(explosionSFX, transform.position);

        if (waveBulletPrefab != null)
        {
            foreach (Vector2 dir in PlusDirections)
            {
                GameObject bullet = Instantiate(waveBulletPrefab, transform.position, Quaternion.identity);

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
