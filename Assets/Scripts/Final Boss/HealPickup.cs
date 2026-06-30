using UnityEngine;

/// <summary>
/// HealPickup — dropped by FRIEND minions on death.
/// Attach to a pickup prefab with a trigger Collider2D.
/// </summary>
public class HealPickup : MonoBehaviour
{
    [Tooltip("HP restored when picked up")]
    public int healAmount = 20;

    [Tooltip("Seconds before auto-destroy if not collected")]
    public float lifetime = 8f;

    [Header("8-bit Bob")]
    public float bobSpeed     = 2f;
    public float bobAmplitude = 0.15f;

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        float y = startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
        transform.position = new Vector3(startPos.x, y, startPos.z);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerHealth ph = other.GetComponent<PlayerHealth>();
        if (ph != null)
        {
            ph.Heal(healAmount);
            Destroy(gameObject);
        }
    }
}
