using System.Collections;
using UnityEngine;

public class MeteorClone : MonoBehaviour
{
    private Transform      playerTarget;
    private float          dashSpeed;
    private GameObject     fireTrailPrefab;
    private Vector2        arenaMin;
    private Vector2        arenaMax;

    public int contactDamage = 8;

    public SpriteRenderer cloneSprite;
    public void Initialize(Transform target, float speed, GameObject trailPrefab,
                           Vector2 minBounds, Vector2 maxBounds)
    {
        playerTarget    = target;
        dashSpeed       = speed * 0.85f;  
        fireTrailPrefab = trailPrefab;
        arenaMin        = minBounds;
        arenaMax        = maxBounds;

        if (cloneSprite == null) cloneSprite = GetComponent<SpriteRenderer>();
        if (cloneSprite != null)
            cloneSprite.color = new Color(0.6f, 0.3f, 1f, 0.75f);

        StartCoroutine(DashLoop());
    }

    private IEnumerator DashLoop()
    {
        yield return new WaitForSeconds(Random.Range(0.2f, 0.6f)); 

        while (true)
        {
            yield return StartCoroutine(SingleDash());
            yield return new WaitForSeconds(Random.Range(0.25f, 0.6f));
        }
    }

    private IEnumerator SingleDash()
    {
        if (playerTarget == null) yield break;

        Vector3 startPos = new Vector3(
            Mathf.Clamp(playerTarget.position.x + Random.Range(-3f, 3f),
                        arenaMin.x + 0.5f, arenaMax.x - 0.5f),
            arenaMax.y + 0.8f, 0f);

        transform.position = startPos;

        Vector3 targetPos = playerTarget.position;
        Vector3 dir       = (targetPos - startPos).normalized;

        while (transform.position.y > arenaMin.y - 1f)
        {
            transform.position += dir * dashSpeed * Time.deltaTime;

            if (fireTrailPrefab != null)
            {
                GameObject trail = Instantiate(fireTrailPrefab, transform.position, Quaternion.identity);
                Destroy(trail, 1f);
            }

            yield return null;
        }

        transform.position = startPos;
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerHealth ph = other.GetComponent<PlayerHealth>();
            if (ph != null) ph.TakeDamage(contactDamage);
        }
    }
}
