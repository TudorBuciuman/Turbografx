using BITROOT.Health;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class FireOrb : MonoBehaviour
{
    public enum MovementPattern
    {
        Linear,     
        Sine,       
        Homing,     
        Circular,  
        Zigzag      
    }

    [SerializeField] private MovementPattern pattern = MovementPattern.Linear;
    [SerializeField] private float speed = 4f;
    [SerializeField] private int damage = 1;
    [SerializeField] private float lifetime = 8f; 

    [SerializeField] private Vector2 direction = Vector2.left;

    [SerializeField] private bool bounceOffBounds = true;
    [SerializeField] private Rect arenaBounds = new Rect(-8f, -4.5f, 16f, 9f); 

    [SerializeField] private float sineAmplitude = 1.5f;
    [SerializeField] private float sineFrequency = 2f;

    [SerializeField] private Transform target;
    [SerializeField] private float homingTurnSpeed = 90f; 

    [SerializeField] private Transform pivot;
    [SerializeField] private float orbitRadius = 2f;
    [SerializeField] private float orbitSpeed = 90f; 
    private float orbitAngle;

    [SerializeField] private float zigzagInterval = 0.5f;
    [SerializeField] private float zigzagAngle = 45f;
    private float zigzagTimer;

    private Vector2 sineOrigin;
    private float sineTime;
    private float spawnTime;

    private void Start()
    {
        direction = direction.normalized;
        sineOrigin = transform.position;
        spawnTime = Time.time;

        if (pattern == MovementPattern.Homing && target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
        }

        if (pattern == MovementPattern.Circular && pivot == null)
        {
            GameObject p = new GameObject("OrbitPivot_" + name);
            p.transform.position = (Vector2)transform.position - direction * orbitRadius;
            pivot = p.transform;
            orbitAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        }
    }

    private void Update()
    {
        switch (pattern)
        {
            case MovementPattern.Linear:
                MoveLinear();
                break;
            case MovementPattern.Sine:
                MoveSine();
                break;
            case MovementPattern.Homing:
                MoveHoming();
                break;
            case MovementPattern.Circular:
                MoveCircular();
                break;
            case MovementPattern.Zigzag:
                MoveZigzag();
                break;
        }

        if (lifetime > 0f && Time.time - spawnTime >= lifetime)
        {
            Despawn();
        }
    }

    private void MoveLinear()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
        if (bounceOffBounds) HandleBounce();
    }

    private void MoveSine()
    {
        sineTime += Time.deltaTime;
        sineOrigin += direction * speed * Time.deltaTime;

        Vector2 perpendicular = new Vector2(-direction.y, direction.x);
        Vector2 wobble = perpendicular * Mathf.Sin(sineTime * sineFrequency) * sineAmplitude;

        transform.position = sineOrigin + wobble;

        if (bounceOffBounds && IsOutOfBounds(sineOrigin))
        {
            direction = -direction;
        }
    }

    private void MoveHoming()
    {
        if (target != null)
        {
            Vector2 toTarget = ((Vector2)target.position - (Vector2)transform.position).normalized;
            direction = Vector3.RotateTowards(direction, toTarget, homingTurnSpeed * Mathf.Deg2Rad * Time.deltaTime, 0f).normalized;
        }
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
    }

    private void MoveCircular()
    {
        orbitAngle += orbitSpeed * Time.deltaTime;
        float rad = orbitAngle * Mathf.Deg2Rad;
        Vector2 offset = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * orbitRadius;
        transform.position = (Vector2)pivot.position + offset;
    }

    private void MoveZigzag()
    {
        zigzagTimer += Time.deltaTime;
        if (zigzagTimer >= zigzagInterval)
        {
            zigzagTimer = 0f;
            float angle = Random.Range(-zigzagAngle, zigzagAngle);
            direction = Quaternion.Euler(0, 0, angle) * direction;
            direction.Normalize();
        }

        transform.position += (Vector3)(direction * speed * Time.deltaTime);
        if (bounceOffBounds) HandleBounce();
    }

    private void HandleBounce()
    {
        Vector2 pos = transform.position;

        if (pos.x < arenaBounds.xMin || pos.x > arenaBounds.xMax)
        {
            direction.x = -direction.x;
        }
        if (pos.y < arenaBounds.yMin || pos.y > arenaBounds.yMax)
        {
            direction.y = -direction.y;
        }

        pos.x = Mathf.Clamp(pos.x, arenaBounds.xMin, arenaBounds.xMax);
        pos.y = Mathf.Clamp(pos.y, arenaBounds.yMin, arenaBounds.yMax);
        transform.position = pos;
    }

    private bool IsOutOfBounds(Vector2 pos)
    {
        return pos.x < arenaBounds.xMin || pos.x > arenaBounds.xMax ||
               pos.y < arenaBounds.yMin || pos.y > arenaBounds.yMax;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerHealth damageable = other.GetComponent<PlayerHealth>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage);
        }
    }

    private void Despawn()
    {
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        if (bounceOffBounds)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(arenaBounds.center, arenaBounds.size);
        }
        if (pattern == MovementPattern.Circular && pivot != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(pivot.position, orbitRadius);
        }
    }
}