using UnityEngine;
using System.Collections;

public class EnemyArcher : MonoBehaviour
{
    public GameObject arrowPrefab;
    private Transform player;

    public float detectionRange = 5f;

    public float aimTime = 0.3f;

    private bool attacking;
    public float attackRange = 5f;
    public float shootCooldown = 1.2f;

    private bool shooting;
    private float nextShot;

    public void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    public void Update()
    {
        float dist = Vector2.Distance(player.position, transform.position);

        if (dist <= attackRange)
        {
            if (!shooting && Time.time > nextShot)
            {
                StartCoroutine(ShootRoutine());
            }
        }
        else
        {
            //Patrol();
        }
    }

    public IEnumerator ShootRoutine()
    {
        shooting = true;
        yield return new WaitForSeconds(aimTime);
        ShootArrow();
        nextShot = Time.time + shootCooldown;
        shooting = false;
    }

    private void ShootArrow()
    {
        Vector2 rawDirection = player.position - transform.position;

        Vector2 normalizedDir = Vector2.zero;

        if (Mathf.Abs(rawDirection.x) > Mathf.Abs(rawDirection.y))
        {
            normalizedDir = new Vector2(Mathf.Sign(rawDirection.x), 0); 
        }
        else
        {
            normalizedDir = new Vector2(0, Mathf.Sign(rawDirection.y));
        }

        GameObject arrow = Instantiate(arrowPrefab, transform.position, Quaternion.identity);

        Arrow a = arrow.GetComponent<Arrow>();
        if (a != null)
        {
            a.SetDirection(normalizedDir);
        }
    }
}