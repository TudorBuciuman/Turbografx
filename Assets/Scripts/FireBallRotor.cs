using BITROOT.Health;
using UnityEngine;

public class FireOrbRotor : MonoBehaviour
{
    [SerializeField] private GameObject orbPrefab; 
    [SerializeField] private int orbCount = 4;   //orbs number, without the center orb       
    [SerializeField] private float orbSpacing = 0.75f;   
    [SerializeField] private bool includeCenterOrb = true; 
    [SerializeField] private float rotationSpeed = 90f; 
    [SerializeField] private float startAngle = 0f;     

    [SerializeField] private int damage = 1;

    private Transform[] armOrbs;
    private float currentAngle;

    private void Start()
    {
        currentAngle = startAngle;

        if (includeCenterOrb && orbPrefab != null)
        {
            GameObject center = Instantiate(orbPrefab, transform.position, Quaternion.identity, transform);
            center.name = "FireOrb_Center";
            center.GetComponent<SpriteRenderer>().sortingOrder = 5;
            ConfigureOrbAsStatic(center);
        }

        armOrbs = new Transform[orbCount];
        for (int i = 0; i < orbCount; i++)
        {
            GameObject orb = Instantiate(orbPrefab, transform.position, Quaternion.identity, transform);
            orb.GetComponent<SpriteRenderer>().sortingOrder = 5;
            orb.name = "FireOrb_Arm_" + i;
            ConfigureOrbAsStatic(orb);
            armOrbs[i] = orb.transform;
        }

        UpdateArmPositions();
    }

    private void Update()
    {
        currentAngle -= rotationSpeed * Time.deltaTime;
        UpdateArmPositions();
    }

    private void UpdateArmPositions()
    {
        float rad = currentAngle * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

        for (int i = 0; i < armOrbs.Length; i++)
        {
            float dist = orbSpacing * (i + 1); 
            armOrbs[i].position = (Vector2)transform.position + dir * dist;
        }
    }

    private void ConfigureOrbAsStatic(GameObject orb)
    {
        FireOrb movementScript = orb.GetComponent<FireOrb>();
        if (movementScript != null)
        {
            movementScript.enabled = false;
        }

        FireOrbContactDamage contact = orb.GetComponent<FireOrbContactDamage>();
        if (contact == null)
        {
            contact = orb.AddComponent<FireOrbContactDamage>();
        }
        contact.SetDamage(damage);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.15f);

        Gizmos.color = new Color(1f, 0.5f, 0f);
        float rad = startAngle * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        for (int i = 0; i < orbCount; i++)
        {
            float dist = orbSpacing * (i + 1);
            Gizmos.DrawWireSphere((Vector2)transform.position + dir * dist, 0.15f);
        }
    }
}

[RequireComponent(typeof(Collider2D))]
public class FireOrbContactDamage : MonoBehaviour
{
    private int damage = 1;

    public void SetDamage(int value) => damage = value;

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerHealth damageable = other.GetComponent<PlayerHealth>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage);
        }
        PlayerMovement p = other.GetComponent<PlayerMovement>();
        if (p != null)
        {
            p.ApplySmallKnockback(transform.position);
        }
    }
}