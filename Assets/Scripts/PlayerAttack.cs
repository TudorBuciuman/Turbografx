using Unity.VisualScripting;
using UnityEngine;
using System;
using System.Collections;

public class PlayerAttack : MonoBehaviour
{
    private float attackRange = 1f;
    private byte swordIndex = 1;
    public LayerMask enemyLayer;

    public Transform attackPoint; // empty object in front of player
    public AudioSource audioSource;

    private float attackCooldown = 0.4f;
    private float lastAttackTime;

    private PlayerStats stats;

    private Vector2 lastDirection;
    void Start()
    {
        stats = GetComponent<PlayerStats>();
    }

    void Update()
    {
        if (UTInput.GetButtonDown("Z"))
        {
            TryAttack();
        }
    }

    public void TryAttack()
    {
        if (Time.time < lastAttackTime + attackCooldown)
            return;

        if (FindFirstObjectByType<PlayerMovement>().canMove)
        {
            lastAttackTime = Time.time;
            Attack();
        }
    }
    public void Attackk()
    {
        PlayerMovement a = FindFirstObjectByType<PlayerMovement>();
        a.canMove = false;
        string dir = "up";
        if (a.GetDirection().x == -1)
            dir = "left";
        else if (a.GetDirection().x == 1)
            dir = "right";
        else if (a.GetDirection().y == -1)
            dir = "down";
        else
            dir = "up";
        this.GetComponent<AudioSource>().clip = Resources.Load<AudioClip>("Sound_effects/snd_sword" + (swordIndex).ToString());
        this.GetComponent<AudioSource>().Play();
        swordIndex++;
        if (swordIndex > 3)
            swordIndex = 1;
        a.anim.Play("attack_" + dir);
        StartCoroutine(AttackDelay());
    }
    public IEnumerator AttackDelay()
    {
        PlayerMovement a = FindFirstObjectByType<PlayerMovement>();
        a.MovementToZero();
        float t = 0;
        while (t < 0.4f)
        {
            t += Time.deltaTime;
            yield return null;
        }
        a.canMove = true;
        yield return null;
    }
    void Attack()
    {
        Attackk();
        lastDirection = FindFirstObjectByType<PlayerMovement>().GetFaceDirection();
        Collider2D[] hits = GetDirectionalHits(lastDirection);
        foreach (var hit in hits)
        {
            Enemy enemy = hit.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(stats.GetDamage());

                Vector2 knockDir = (hit.transform.position - transform.position).normalized;
                hit.GetComponent<EnemyMovement>().ApplyKnockback(knockDir, 4f, 0.1f);
                enemy.ApplyKnockback(knockDir, 4f, 0.1f);
            }
            Breakable b = hit.GetComponent<Breakable>();
            if (b != null)
            {
                b.TryBreak(stats);
            }
        }
    }
    public Collider2D[] GetDirectionalHits(Vector2 direction)
    {
        Vector2 size = new Vector2(0.7f, 0.7f); 
        Vector2 offset = direction * 0.65f;      

        Vector2 center = (Vector2)transform.position + offset;

        return Physics2D.OverlapBoxAll(center, size, 0f, enemyLayer);
    }
    void OnDrawGizmosSelected()
    {
        lastDirection = Vector2.left;
        Vector2 offset = lastDirection * 0.65f;
        Vector2 center = (Vector2)transform.position + offset;

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(center, new Vector3(0.7f, 0.7f, 0));
    } 
    public void PlayDeathSound()
    {
        audioSource.clip = Resources.Load<AudioClip>("Sound_effects/snd_enemy_death");
        audioSource.Play();
    }
}