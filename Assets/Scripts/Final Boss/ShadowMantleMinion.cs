using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ShadowMantleMinion — the "FRIEND" smiling-face enemy spawned by the Shadow Mantle Holder.
///
/// Behaviour:
///   • Spawns snapped to the centre of the nearest open tile (integer pixel coords).
///   • Idles in place when the player is >= chaseDistance away.
///   • When the player is within chaseDistance, runs a tile-based BFS through
///     ArenaTileGrid to find a path that avoids walls/obstacles, then walks it
///     one tile at a time (classic 8-bit grid movement).
///   • Re-paths every repath interval so it reacts to a moving player.
///   • On death: chance to drop a heal pickup, spawn death particles.
///   • Deals contact damage to the player.
///
/// SETUP:
///   • Attach to the FRIEND minion prefab.
///   • ArenaTileGrid must be present in the scene (it is the obstacle map).
///   • The prefab needs a Collider2D (trigger) and a Rigidbody2D (kinematic).
///   • Assign healPickupPrefab and optionally deathParticlePrefab in the Inspector.
/// </summary>
public class ShadowMantleMinion : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────
    [Header("Stats")]
    public int maxHealth = 30;
    public int contactDamage = 8;

    [Tooltip("World-units per second while walking between tiles")]
    public float moveSpeed = 2.5f;

    [Header("Chase")]
    [Tooltip("Player must be closer than this (world units) for the minion to start chasing")]
    public float chaseDistance = 5f;

    [Tooltip("How often (seconds) the minion recalculates its path to the player")]
    public float repathInterval = 0.4f;

    [Header("Heal Drop")]
    public GameObject healPickupPrefab;

    [Range(0f, 1f)]
    public float healDropChance = 0.4f;

    [Header("Visual / Audio")]
    public GameObject deathParticlePrefab;
    public AudioClip deathSFX;
    public AudioClip spawnSFX;

    // ─────────────────────────────────────────────
    //  Internal state
    // ─────────────────────────────────────────────
    private int currentHealth;
    private Transform playerTarget;
    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;
    private bool isDead = false;
    private bool isMoving = false;   // true while sliding between tiles
    private bool spawnDone = false;   // set after SpawnAnimation finishes

    // Current tile position in grid coords
    private int tileCol;
    private int tileRow;

    // BFS path — queue of grid coords to walk
    private readonly Queue<Vector2Int> pathQueue = new Queue<Vector2Int>();

    // Wobble
    private float wobbleTimer = 0f;

    // ─────────────────────────────────────────────
    //  Unity
    // ─────────────────────────────────────────────
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

        // Cheerful wobble (scale only — position is driven by MoveToTile coroutine)
        wobbleTimer += Time.deltaTime * 4f;
        transform.localScale = Vector3.one * (1f + Mathf.Sin(wobbleTimer) * 0.06f);

        // Face the next tile in the path (or the player when idle)
        UpdateFacing();
    }

    // ─────────────────────────────────────────────
    //  Tile-snapped spawn
    // ─────────────────────────────────────────────

    /// <summary>
    /// Moves the minion to the pixel-centre of the nearest walkable tile.
    /// Called once on Start so the minion is never born inside a wall.
    /// </summary>
    private void SnapToNearestOpenTile()
    {
        ArenaTileGrid grid = ArenaTileGrid.Instance;
        if (grid == null)
        {
            // No grid — snap to 16px grid manually and stay put
            SnapPositionToPixelGrid();
            return;
        }

        // Find the closest open tile to our current world position
        Vector2 worldPos = transform.position;
        grid.WorldToTile(worldPos, out int bestCol, out int bestRow);

        // If the nearest tile is blocked, spiral outward to find an open one
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

    /// <summary>BFS spiral to find the nearest walkable tile to (startCol, startRow).</summary>
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

        return new Vector2Int(startCol, startRow); // fallback
    }

    /// <summary>Snaps world position to a 16px pixel grid without a grid instance.</summary>
    private void SnapPositionToPixelGrid(float ppu = 16f)
    {
        Vector3 p = transform.position;
        p.x = Mathf.Round(p.x * ppu) / ppu;
        p.y = Mathf.Round(p.y * ppu) / ppu;
        transform.position = p;
    }

    // ─────────────────────────────────────────────
    //  Pathfinding — BFS on ArenaTileGrid
    // ─────────────────────────────────────────────

    /// <summary>
    /// Periodically recalculates the path to the player.
    /// Only runs while the player is within chaseDistance.
    /// </summary>
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

                // Start walking if not already moving
                if (!isMoving && pathQueue.Count > 0)
                    StartCoroutine(WalkPath());
            }
            else
            {
                // Player too far — stop planning
                pathQueue.Clear();
            }
        }
    }

    /// <summary>
    /// BFS from (startCol, startRow) toward the tile closest to targetWorldPos.
    /// Returns a list of grid coords to walk (not including the start tile).
    /// </summary>
    private List<Vector2Int> BFSPath(int startCol, int startRow, Vector2 targetWorldPos)
    {
        ArenaTileGrid grid = ArenaTileGrid.Instance;
        if (grid == null) return new List<Vector2Int>();

        // Convert player world pos to tile
        grid.WorldToTile(targetWorldPos, out int goalCol, out int goalRow);

        // Clamp goal to a walkable tile (player may be standing in a non-tile area)
        if (!grid.IsWalkable(goalCol, goalRow))
        {
            Vector2Int nearest = FindNearestOpenTile(grid, goalCol, goalRow);
            goalCol = nearest.x;
            goalRow = nearest.y;
        }

        Vector2Int start = new Vector2Int(startCol, startRow);
        Vector2Int goal = new Vector2Int(goalCol, goalRow);

        if (start == goal) return new List<Vector2Int>();

        // BFS
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

        // Reconstruct path (exclude start tile)
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

    /// <summary>Returns the 4 cardinal neighbours of a tile.</summary>
    private IEnumerable<Vector2Int> CardinalNeighbours(int col, int row)
    {
        yield return new Vector2Int(col + 1, row);
        yield return new Vector2Int(col - 1, row);
        yield return new Vector2Int(col, row + 1);
        yield return new Vector2Int(col, row - 1);
    }

    // ─────────────────────────────────────────────
    //  Grid movement
    // ─────────────────────────────────────────────

    /// <summary>
    /// Consumes the pathQueue one tile at a time, smoothly sliding between centres.
    /// </summary>
    private IEnumerator WalkPath()
    {
        isMoving = true;

        while (pathQueue.Count > 0 && !isDead)
        {
            // Re-check distance before each step
            if (playerTarget != null &&
                Vector2.Distance(transform.position, playerTarget.position) >= chaseDistance)
            {
                pathQueue.Clear();
                break;
            }

            Vector2Int nextTile = pathQueue.Dequeue();

            // Safety: skip if the tile became blocked since we pathed
            if (ArenaTileGrid.Instance != null &&
                !ArenaTileGrid.Instance.IsWalkable(nextTile.x, nextTile.y))
                continue;

            // Slide to next tile centre
            Vector2 targetWorld = ArenaTileGrid.Instance != null
                ? ArenaTileGrid.Instance.TileCenterWorld(nextTile.x, nextTile.y)
                : (Vector2)transform.position; // fallback

            yield return StartCoroutine(SlideTo(targetWorld));

            tileCol = nextTile.x;
            tileRow = nextTile.y;
        }

        isMoving = false;
    }

    /// <summary>Smoothly moves the minion from its current position to <target> world pos.</summary>
    private IEnumerator SlideTo(Vector2 target)
    {
        Vector2 start = transform.position;

        // Time to cross one tile = tileWorldSize / moveSpeed
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

        // Snap precisely to pixel centre at end of slide
        transform.position = (Vector3)target;
    }

    // ─────────────────────────────────────────────
    //  Facing
    // ─────────────────────────────────────────────
    private void UpdateFacing()
    {
        if (spriteRenderer == null) return;

        Vector2 lookTarget;

        if (pathQueue.Count > 0)
        {
            // Face the next queued tile
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

    /// <summary>Peek at the front of a Queue without dequeuing.</summary>
    private static T Peek<T>(Queue<T> q)
    {
        // Queue has no built-in Peek in all Unity .NET versions — safe wrapper
        T[] arr = q.ToArray();
        return arr[0];
    }

    // ─────────────────────────────────────────────
    //  Damage & Death
    // ─────────────────────────────────────────────
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

    // ─────────────────────────────────────────────
    //  Collision
    // ─────────────────────────────────────────────
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerHealth ph = other.GetComponent<PlayerHealth>();
            if (ph != null) ph.TakeDamage(contactDamage);
        }
    }

    // ─────────────────────────────────────────────
    //  Animations / Helpers
    // ─────────────────────────────────────────────
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
    // Draw the current planned path in Scene view
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