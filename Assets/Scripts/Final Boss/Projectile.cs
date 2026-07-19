using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour
{
    private float speed = 10f;
    private int damage = 1;
    private float lifetime = 0.5f;
    private bool faceDirection = true;
    private bool  pixelSnap = false;
    private float pixelsPerUnit = 16f;

    private Vector2 moveDir;
    private Rigidbody2D rb;
    private bool initialised = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.isKinematic  = true;
    }

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

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

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerHealth ph = other.GetComponent<PlayerHealth>();
            if (ph != null) ph.TakeDamage(damage);
            Destroy(gameObject);
        }

        if (other.CompareTag("Wall"))
            Destroy(gameObject);
    }
}
