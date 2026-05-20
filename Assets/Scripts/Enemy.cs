using UnityEngine;
using System.Collections;


public class Enemy : MonoBehaviour
{
    public int maxHealth = 3;
    private int currentHealth;

    public int expReward = 2;

    public bool canMove = true;
    public bool canAttack = true;

    public GameObject healDropPrefab;
    public float dropChance = 0.3f;

    private void Start()
    {
        currentHealth = maxHealth;
    }
    public void ResetHealth()
    {
        currentHealth = maxHealth;
    }
    public void TakeDamage(int dmg)
    {
        currentHealth -= dmg;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        FindFirstObjectByType<PlayerStats>().AddExp(expReward);
        FindFirstObjectByType<PlayerAttack>().PlayDeathSound();

        if (Random.value < dropChance && healDropPrefab != null)
        {
            GameObject a= Instantiate(healDropPrefab, transform.position, Quaternion.identity);
            a.GetComponent<HealItem>().SetRoom(FindFirstObjectByType<PlayerMovement>().GetCurrentRoom());
        }

        Destroy(gameObject);
    }
    public void ApplyKnockback(Vector2 direction, float force, float duration)
    {
        StartCoroutine(KnockbackCoroutine(direction, force, duration));
    }

    private IEnumerator KnockbackCoroutine(Vector2 direction, float force, float duration)
    {
        float timer = 0f;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();

        while (timer < duration)
        {
            rb.velocity = direction * force; // or rb.velocity depending on Unity version
            timer += Time.deltaTime;
            yield return null;
        }

        rb.velocity = Vector2.zero;
    }
}