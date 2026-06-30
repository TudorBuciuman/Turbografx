using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class WaveProjectile : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 4.5f;
    public float lifetime = 5f;

    [Header("Damage")]
    public int damage = 1;

    private Vector2 direction;
    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.isKinematic = true;
    }

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    public void Initialize(Vector2 dir, float bulletSpeed, int bulletDamage)
    {
        direction = SnapToCardinal(dir);
        speed = bulletSpeed;
        damage = bulletDamage;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    void Update()
    {
        // PURE linear motion (no curves, no easing, no oscillation)
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
    }

    private Vector2 SnapToCardinal(Vector2 dir)
    {
        dir = dir.normalized;

        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
            return dir.x > 0 ? Vector2.right : Vector2.left;
        else
            return dir.y > 0 ? Vector2.up : Vector2.down;
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