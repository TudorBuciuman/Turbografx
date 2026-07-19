using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShadowMantleMinion : MonoBehaviour
{
    public int maxHealth = 30;
    public int contactDamage = 8;

    public float moveSpeed = 2.5f;
    public float chaseDistance = 5f;
    public float repathInterval = 0.4f;

    public GameObject healPickupPrefab;

    [Range(0f, 1f)]
    public float healDropChance = 0.4f;

    public GameObject deathParticlePrefab;
    public AudioClip deathSFX;
    public AudioClip spawnSFX;

    private int currentHealth;
    private Transform playerTarget;
    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;
    private bool isDead = false;
    private bool isMoving = false;   
    private bool spawnDone = false;   

    private int tileCol;
    private int tileRow;

    private readonly Queue<Vector2Int> pathQueue = new Queue<Vector2Int>();
    private float wobbleTimer = 0f;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
        currentHealth = maxHealth;
    }

    void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTarget = player.transform;

        SnapToNearestOpenTile();
        PlaySFX(spawnSFX);
        StartCoroutine(SpawnAnimation());
        StartCoroutine(RepathLoop());
    }

    void Update()
    {
        if (isDead || !spawnDone) return;

        wobbleTimer += Time.deltaTime * 4f;
        transform.localScale = Vector3.one * (1f + Mathf.Sin(wobbleTimer) * 0.06f);

        UpdateFacing();
    }

    private void SnapToNearestOpenTile()
    {
        ArenaTileGrid grid = ArenaTileGrid.Instance;
        if (grid == null)
        {
            SnapPositionToPixelGrid();
            return;
        }

        Vector2 worldPos = transform.position;
        grid.WorldToTile(worldPos, out int bestCol, out int bestRow);

        if (!grid.IsWalkable(bestCol, bestRow))
        {
            Vector2Int found = FindNearestOpenTile(grid, bestCol, bestRow);
            bestCol = found.x;
            bestRow = found.y;
        }

        tileCol = bestCol;
        tileRow = bestRow;
        transform.position = (Vector3)grid.TileCenterWorld(tileCol, tileRow);
    }
    private Vector2Int FindNearestOpenTile(ArenaTileGrid grid, int startCol, int startRow)
    {
        Queue<Vector2Int> frontier = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        frontier.Enqueue(new Vector2Int(startCol, startRow));
        visited.Add(new Vector2Int(startCol, startRow));

        while (frontier.Count > 0)
        {
            Vector2Int cur = frontier.Dequeue();
            if (grid.IsWalkable(cur.x, cur.y))
                return cur;

            foreach (Vector2Int n in CardinalNeighbours(cur.x, cur.y))
            {
                if (!visited.Contains(n))
                {
                    visited.Add(n);
                    frontier.Enqueue(n);
                }
            }
        }

        return new Vector2Int(startCol, startRow); 
    }

    private void SnapPositionToPixelGrid(float ppu = 16f)
    {
        Vector3 p = transform.position;
        p.x = Mathf.Round(p.x * ppu) / ppu;
        p.y = Mathf.Round(p.y * ppu) / ppu;
        transform.position = p;
    }
    private IEnumerator RepathLoop()
    {
        yield return new WaitUntil(() => spawnDone);

        while (!isDead)
        {
            yield return new WaitForSeconds(repathInterval);

            if (playerTarget == null) continue;

            float dist = Vector2.Distance(transform.position, playerTarget.position);
            if (dist < chaseDistance)
            {
                List<Vector2Int> newPath = BFSPath(tileCol, tileRow, playerTarget.position);
                pathQueue.Clear();
                foreach (Vector2Int step in newPath)
                    pathQueue.Enqueue(step);

                if (!isMoving && pathQueue.Count > 0)
                    StartCoroutine(WalkPath());
            }
            else
            {
                pathQueue.Clear();
            }
        }
    }

    private List<Vector2Int> BFSPath(int startCol, int startRow, Vector2 targetWorldPos)
    {
        ArenaTileGrid grid = ArenaTileGrid.Instance;
        if (grid == null) return new List<Vector2Int>();

        grid.WorldToTile(targetWorldPos, out int goalCol, out int goalRow);

        if (!grid.IsWalkable(goalCol, goalRow))
        {
            Vector2Int nearest = FindNearestOpenTile(grid, goalCol, goalRow);
            goalCol = nearest.x;
            goalRow = nearest.y;
        }

        Vector2Int start = new Vector2Int(startCol, startRow);
        Vector2Int goal = new Vector2Int(goalCol, goalRow);

        if (start == goal) return new List<Vector2Int>();

        Queue<Vector2Int> frontier = new Queue<Vector2Int>();
        Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();

        frontier.Enqueue(start);
        cameFrom[start] = start;

        bool found = false;

        while (frontier.Count > 0)
        {
            Vector2Int cur = frontier.Dequeue();

            if (cur == goal) { found = true; break; }

            foreach (Vector2Int neighbour in CardinalNeighbours(cur.x, cur.y))
            {
                if (cameFrom.ContainsKey(neighbour)) continue;
                if (!grid.IsWalkable(neighbour.x, neighbour.y)) continue;

                cameFrom[neighbour] = cur;
                frontier.Enqueue(neighbour);
            }
        }

        if (!found) return new List<Vector2Int>();

        List<Vector2Int> path = new List<Vector2Int>();
        Vector2Int step = goal;
        while (step != start)
        {
            path.Add(step);
            step = cameFrom[step];
        }
        path.Reverse();
        return path;
    }

    private IEnumerable<Vector2Int> CardinalNeighbours(int col, int row)
    {
        yield return new Vector2Int(col + 1, row);
        yield return new Vector2Int(col - 1, row);
        yield return new Vector2Int(col, row + 1);
        yield return new Vector2Int(col, row - 1);
    }

    private IEnumerator WalkPath()
    {
        isMoving = true;

        while (pathQueue.Count > 0 && !isDead)
        {
            if (playerTarget != null &&
                Vector2.Distance(transform.position, playerTarget.position) >= chaseDistance)
            {
                pathQueue.Clear();
                break;
            }

            Vector2Int nextTile = pathQueue.Dequeue();

            if (ArenaTileGrid.Instance != null &&
                !ArenaTileGrid.Instance.IsWalkable(nextTile.x, nextTile.y))
                continue;

            Vector2 targetWorld = ArenaTileGrid.Instance != null
                ? ArenaTileGrid.Instance.TileCenterWorld(nextTile.x, nextTile.y)
                : (Vector2)transform.position; 

            yield return StartCoroutine(SlideTo(targetWorld));

            tileCol = nextTile.x;
            tileRow = nextTile.y;
        }

        isMoving = false;
    }

    private IEnumerator SlideTo(Vector2 target)
    {
        Vector2 start = transform.position;

        float tileSize = ArenaTileGrid.Instance != null
            ? ArenaTileGrid.Instance.TileWorldSize
            : 1f;
        float duration = tileSize / Mathf.Max(moveSpeed, 0.01f);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector2.Lerp(start, target, elapsed / duration);
            yield return null;
        }

        transform.position = (Vector3)target;
    }

    private void UpdateFacing()
    {
        if (spriteRenderer == null) return;

        Vector2 lookTarget;

        if (pathQueue.Count > 0)
        {
            Vector2Int next = Peek(pathQueue);
            ArenaTileGrid grid = ArenaTileGrid.Instance;
            lookTarget = grid != null
                ? grid.TileCenterWorld(next.x, next.y)
                : (Vector2)transform.position;
        }
        else if (playerTarget != null)
        {
            lookTarget = playerTarget.position;
        }
        else return;

        spriteRenderer.flipX = lookTarget.x < transform.position.x;
    }

    private static T Peek<T>(Queue<T> q)
    {
        T[] arr = q.ToArray();
        return arr[0];
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;
        currentHealth -= amount;
        StartCoroutine(FlashColor(Color.white, 0.08f));
        if (currentHealth <= 0) Die();
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        StopAllCoroutines();
        PlaySFX(deathSFX);

        if (deathParticlePrefab != null)
        {
            GameObject p = Instantiate(deathParticlePrefab, transform.position, Quaternion.identity);
            Destroy(p, 2f);
        }

        if (Random.value <= healDropChance && healPickupPrefab != null)
            Instantiate(healPickupPrefab, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerHealth ph = other.GetComponent<PlayerHealth>();
            if (ph != null) ph.TakeDamage(contactDamage);
        }
    }

    private IEnumerator SpawnAnimation()
    {
        transform.localScale = Vector3.zero;
        float t = 0f;
        while (t < 0.35f)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.one * Mathf.SmoothStep(0f, 1f, t / 0.35f);
            yield return null;
        }
        transform.localScale = Vector3.one;
        spawnDone = true;
    }

    private IEnumerator FlashColor(Color color, float duration)
    {
        if (spriteRenderer == null) yield break;
        Color original = spriteRenderer.color;
        spriteRenderer.color = color;
        yield return new WaitForSeconds(duration);
        if (spriteRenderer != null) spriteRenderer.color = original;
    }

    private void PlaySFX(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
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
            rb.velocity = direction * force;
            timer += Time.deltaTime;
            yield return null;
        }

        rb.velocity = Vector2.zero;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (ArenaTileGrid.Instance == null) return;
        Gizmos.color = Color.cyan;
        Vector3 prev = transform.position;
        foreach (Vector2Int tile in pathQueue)
        {
            Vector3 next = ArenaTileGrid.Instance.TileCenterWorld(tile.x, tile.y);
            Gizmos.DrawLine(prev, next);
            Gizmos.DrawWireSphere(next, 0.1f);
            prev = next;
        }
    }
#endif
}