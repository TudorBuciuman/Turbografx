using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemyMovement : MonoBehaviour
{
    public enum State { Idle, Patrol, Chase, Return }
    public State currentState = State.Idle;
    public int animState=0;

    public float moveSpeed = 2f;
    public float detectionRange = 5f;
    public float stopDistance = 0.8f;

    public Transform[] patrolPoints;
    private int patrolIndex = 0;

    private Transform player;

    public int damage = 1;
    private float damageCooldown = 0.3f;
    public int room = 1;
    public State DefaultState = State.Idle;
    //it is calculated based of the matrix, where it starts down left

    private float lastDamageTime;

    private Vector2 startPosition;
    private bool playerInRoom = false;

    private bool isKnockedBack = false;
    private float knockbackTimer;
    private Vector2 knockbackVelocity;
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        startPosition = transform.position;
        if (animState == 1)
        {
            MadEnemy();
        }
    }

    void Update()
    {
        if (!playerInRoom || DefaultState==State.Idle) return;
        float distToPlayer = Vector2.Distance(transform.position, player.position);

        if (isKnockedBack && DefaultState!=State.Idle)
        {
            transform.position += (Vector3)(knockbackVelocity * Time.deltaTime);

            knockbackTimer -= Time.deltaTime;
            if (knockbackTimer <= 0)
            {
                isKnockedBack = false;
            }

            return;
        }
        if (playerInRoom && distToPlayer < detectionRange && (DefaultState==State.Patrol || DefaultState == State.Chase))
        {
            currentState = State.Chase;
        }

        switch (currentState)
        {
            case State.Idle:
                break;

            case State.Patrol:
                Patrol();
                break;

            case State.Chase:
                Chase();
                break;
                    
            case State.Return:
                ReturnToStart();
                break;
        }
    }

    void Patrol()
    {
        if (patrolPoints.Length == 0) return;
        Transform target = patrolPoints[patrolIndex];

        MoveTowards(target.position);

        if (Vector2.Distance(transform.position, target.position) < 0.2f)
        {
            patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
        }
    }
    public void OnPlayerExitRoom()
    {
        playerInRoom = false;
        currentState = State.Return;
    }
    public void ResetEnemy()
    {
        transform.position = startPosition;
        playerInRoom = true;
        currentState = DefaultState; 
        GetComponent<Enemy>().ResetHealth();
    }
    public int GetRoom()
    {
        return room;
    }
    public void MadEnemy()
    {
        this.GetComponent<Animator>().Play("enemy2");
    }
    void ReturnToStart()
    {
        MoveTowards(startPosition);

        if (Vector2.Distance(transform.position, startPosition) < 0.1f)
        {
            transform.position = startPosition;
            currentState = State.Idle; //or Patrol, I dont even know anaymore....
        }
    }
    void Chase()
    {
        float dist = Vector2.Distance(transform.position, player.position);

        if (dist > detectionRange * 1.5f)
        {
            currentState = State.Patrol;
            return;
        }

        if (dist > stopDistance)
        {
            MoveTowards(player.position);
        }
        else
        {
            return;
        }
    }
    public void ApplyKnockback(Vector2 direction, float force, float duration)
    {
        isKnockedBack = true;
        knockbackTimer = duration;
        knockbackVelocity = direction * force;
    }
    void MoveTowards(Vector2 target)
    {
        Vector2 dir = (target - (Vector2)transform.position).normalized;
        //dir = Vector2.Lerp(Vector2.zero, dir, 0.8f);
        transform.position += (Vector3)(dir * moveSpeed * Time.deltaTime);

    }
    void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player") && DefaultState!=State.Idle)
        {
            if (Time.time > lastDamageTime + damageCooldown)
            {
                PlayerHealth hp = collision.gameObject.GetComponent<PlayerHealth>();

                if (hp != null)
                {
                    hp.TakeDamage(damage);
                    lastDamageTime = Time.time;
                }
            }
        }
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player") && DefaultState != State.Idle)
        {
            PlayerMovement p = collision.gameObject.GetComponent<PlayerMovement>();

            if (p != null)
            {
                p.ApplyKnockback(transform.position);
            }
        }
    }
}