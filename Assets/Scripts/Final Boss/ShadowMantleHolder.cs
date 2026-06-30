using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// ArenaTileGrid is referenced for grid-snapped bomb placement — ensure it is in the scene.

/// <summary>
/// Shadow Mantle Holder - Secret Boss (inspired by Deltarune Chapter 3)
/// 
/// PHASE OVERVIEW:
///   Phase 1 — Hexagonal fireball bursts + bomb placement
///   Phase 2 — Meteor dash attacks (4 rams) + FRIEND minion spawns on 5th land
///   Phase 3 — Phase 1 attacks simultaneously + transitions to Phase 2 meteor segment
///   Phase 4 — Triple meteor finale (boss + 2 half-damage clones), then defeat
///
/// SETUP REQUIREMENTS:
///   Attach this script to the boss GameObject.
///   Assign all prefab references in the Inspector.
///   Arena bounds are defined by arenaMin / arenaMax.
///   The boss sprite should face right by default; the script handles flipping.
/// </summary>
public class ShadowMantleHolder : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Inspector References
    // ─────────────────────────────────────────────
    [Header("Prefabs")]
    [Tooltip("Hexagonal orb projectile (Phase 1 / Phase 3)")]
    public GameObject fireballPrefab;

    [Tooltip("Bomb that explodes in a plus-pattern (Phase 1 / Phase 3)")]
    public GameObject bombPrefab;

    [Tooltip("'FRIEND' smiling-face minion (Phase 2+)")]
    public GameObject friendMinionPrefab;

    [Tooltip("Fire trail particle left during meteor dash (Phase 2+)")]
    public GameObject fireTrailPrefab;

    [Tooltip("Clone of the boss used during Phase 4 finale")]
    public GameObject meteorClonePrefab;

    [Header("Arena")]
    public Vector2 arenaMin = new Vector2(-7f, -4f);
    public Vector2 arenaMax = new Vector2(5f, 4f);

    [Header("Stats")]
    public int maxHealth = 800;
    public float moveSpeed = 3f;

    [Header("Phase Thresholds (% HP remaining)")]
    [Range(0f, 1f)] public float phase2Threshold = 0.75f;
    [Range(0f, 1f)] public float phase3Threshold = 0.50f;
    [Range(0f, 1f)] public float phase4Threshold = 0.25f;

    [Header("Attack Timings (seconds)")]
    public float fireballInterval    = 0.18f;
    public float bombPlacementDelay  = 0.40f;
    public float meteorDashSpeed     = 14f;
    public float laughPauseDuration  = 2.8f;
    public float betweenAttackDelay  = 1.2f;

    [Header("Fireball Settings")]
    public float fireballSpeed       = 5f;
    public int   hexRings            = 4;   // how many hexagonal bursts per attack

    [Header("Bomb Settings")]
    public int   bombCount           = 3;
    public float bombFuseTime        = 1.2f;

    [Header("Meteor Settings")]
    public int   meteorDashCount     = 4;   // dashes before landing in center
    public float meteorLandRadius    = 0.6f;

    [Header("Friend Minion Settings")]
    public int   friendSpawnCount    = 5;
    public int   friendMaxSpawnCount    = 6;
    public float friendSpawnRadius   = 2f;

    [Header("Audio (optional)")]
    public AudioClip fireballSFX;
    public AudioClip bombSFX;
    public AudioClip meteorSFX;
    public AudioClip laughSFX;
    public AudioClip spawnMinionSFX;
    public AudioClip hurtSFX;
    public AudioClip defeatSFX;

    // ─────────────────────────────────────────────
    //  Internal State
    // ─────────────────────────────────────────────
    private int          currentHealth;
    private int          currentPhase    = 1;
    private bool         isBusy          = false;   // true while executing an attack routine
    private bool         isDead          = false;
    private bool         isInMeteorForm  = false;
    private Transform    playerTarget;
    private SpriteRenderer spriteRenderer;
    private AudioSource  audioSource;

    // Figure-8 patrol for Phase 1 idle movement
    private float        figure8Timer    = 0f;
    private const float  FIGURE8_SPEED   = 0.8f;
    private const float  FIGURE8_XSCALE  = 3.5f;
    private const float  FIGURE8_YSCALE  = 1.5f;

    // ─────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────
    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource    = GetComponent<AudioSource>();
        currentHealth  = maxHealth;
    }

    void Start()
    {
        // Find player by tag; assign before first frame of combat
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTarget = player.transform;

        StartCoroutine(BossBrain());
    }

    void Update()
    {
        if (isDead || isBusy) return;

        // Idle figure-8 patrol during non-attack frames
        if (!isInMeteorForm)
            DoFigure8Movement();

        // Face player
        if (playerTarget != null)
            spriteRenderer.flipX = playerTarget.position.x < transform.position.x;
    }

    // ─────────────────────────────────────────────
    //  Core Brain — selects attacks based on phase
    // ─────────────────────────────────────────────
    private IEnumerator BossBrain()
    {
        yield return new WaitForSeconds(1f); // brief opening pause

        while (!isDead)
        {
            yield return new WaitUntil(() => !isBusy);
            yield return new WaitForSeconds(betweenAttackDelay);

            CheckPhaseTransition();

            if (currentPhase == 1)
            {
                yield return StartCoroutine(PickPhase1Attack());
            }
            else if (currentPhase == 2)
            {
                yield return StartCoroutine(Phase2MeteorCycle());
            }
            else if (currentPhase == 3)
            {
                yield return StartCoroutine(Phase3CombinedAttack());
            }
            else if (currentPhase == 4)
            {
                yield return StartCoroutine(Phase4Finale());
                // Phase 4 ends the fight — loop will exit via isDead flag
            }
        }
    }

    // ─────────────────────────────────────────────
    //  Phase Transition
    // ─────────────────────────────────────────────
    private void CheckPhaseTransition()
    {
        float hpRatio = (float)currentHealth / maxHealth;
        int   newPhase = currentPhase;

        if      (hpRatio <= phase4Threshold && currentPhase < 4) newPhase = 4;
        else if (hpRatio <= phase3Threshold && currentPhase < 3) newPhase = 3;
        else if (hpRatio <= phase2Threshold && currentPhase < 2) newPhase = 2;

        if (newPhase != currentPhase)
        {
            currentPhase = newPhase;
            StartCoroutine(PhaseTransitionEffect());
        }
    }

    private IEnumerator PhaseTransitionEffect()
    {
        isBusy = true;
        // Move to arena center during transition (mimics background color change)
        yield return StartCoroutine(MoveToPosition(ArenaCenter(), 1.5f));
        // Flash effect
        for (int i = 0; i < 6; i++)
        {
            spriteRenderer.enabled = !spriteRenderer.enabled;
            yield return new WaitForSeconds(0.1f);
        }
        spriteRenderer.enabled = true;
        isBusy = false;
    }

    // ─────────────────────────────────────────────
    //  PHASE 1 — Hexagonal fireballs OR bombs
    // ─────────────────────────────────────────────
    private IEnumerator PickPhase1Attack()
    {
        if (Random.value > 0.5f)
            yield return StartCoroutine(AttackHexFireballs());
        else
            yield return StartCoroutine(AttackBombs(bombCount));
    }

    /// <summary>
    /// Fires hexRings sets of 6 orbs arranged in a hexagonal pattern.
    /// Boss moves in a loose figure-8 while shooting.
    /// </summary>
    private IEnumerator AttackHexFireballs()
    {
        isBusy = true;
        PlaySFX(fireballSFX);

        for (int ring = 0; ring < hexRings; ring++)
        {
            SpawnHexRing(transform.position);
            yield return new WaitForSeconds(fireballInterval);
        }

        isBusy = false;
    }

    private void SpawnHexRing(Vector3 origin)
    {
        // 6 projectiles at 60-degree intervals
        for (int i = 0; i < 6; i++)
        {
            float angle = i * 60f * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            SpawnProjectile(fireballPrefab, origin, dir, fireballSpeed, 1);
        }
    }

    /// <summary>
    /// Places <count> bombs on empty floor tiles (queried from ArenaTileGrid).
    /// Each bomb snaps to the centre of a 16px tile and avoids landing on walls
    /// or on tiles already occupied by another bomb placed this same attack.
    /// </summary>
    private IEnumerator AttackBombs(int count)
    {
        isBusy = true;
        PlaySFX(bombSFX);

        List<Vector2> usedPositions = new List<Vector2>();

        for (int i = 0; i < count; i++)
        {
            Vector2 bombPos = PickBombTile(usedPositions);
            usedPositions.Add(bombPos);

            GameObject bomb = Instantiate(bombPrefab, bombPos, Quaternion.identity);
            BombProjectile bp = bomb.GetComponent<BombProjectile>();
            if (bp != null)
                bp.Initialize(bombFuseTime, 1);

            yield return new WaitForSeconds(bombPlacementDelay);
        }

        isBusy = false;
    }

    // ─────────────────────────────────────────────
    //  PHASE 2 — Meteor dash cycle + FRIEND spawns
    // ─────────────────────────────────────────────
    private IEnumerator Phase2MeteorCycle()
    {
        isBusy = true;
        EnterMeteorForm();

        // 4 ram dashes targeting player position
        for (int dash = 0; dash < meteorDashCount; dash++)
        {
            yield return StartCoroutine(MeteorDash());
            yield return new WaitForSeconds(0.3f);
        }

        // 5th: land in center, laugh, spawn FRIENDs
        yield return StartCoroutine(MeteorLandCenter());
        yield return StartCoroutine(LaughAndSpawnFriends());

        ExitMeteorForm();
        isBusy = false;
    }

    private IEnumerator MeteorDash()
    {
        if (playerTarget == null) yield break;

        // Begin at top of arena, aim slightly toward player's last position
        Vector3 topStart = new Vector3(
            Mathf.Clamp(playerTarget.position.x + Random.Range(-2f, 2f),
                        arenaMin.x + 1f, arenaMax.x - 1f),
            arenaMax.y + 1f, 0f);

        transform.position = topStart;
        PlaySFX(meteorSFX);

        Vector3 target = playerTarget.position;
        Vector3 dir    = (target - transform.position).normalized;

        // Dash until we cross the arena bottom
        while (transform.position.y > arenaMin.y - 1f)
        {
            transform.position += dir * meteorDashSpeed * Time.deltaTime;
            SpawnFireTrail();
            yield return null;
        }

        // Wrap back to top
        transform.position = topStart;
    }

    private IEnumerator MeteorLandCenter()
    {
        Vector3 center = ArenaCenter();
        yield return StartCoroutine(MoveToPosition(center, 0.4f));
        SpawnFireTrail();
    }

    private IEnumerator LaughAndSpawnFriends()
    {
        PlaySFX(laughSFX);
        // Animate laugh — simple scale pulse
        for (float t = 0; t < laughPauseDuration; t += 0.2f)
        {
            transform.localScale = Vector3.one * (1f + Mathf.Sin(t * 18f) * 0.08f);
            yield return new WaitForSeconds(0.2f);
        }
        transform.localScale = Vector3.one;

        // Spawn FRIEND minions in a circle
        PlaySFX(spawnMinionSFX);
        int u = FindObjectsByType<ShadowMantleMinion>(FindObjectsSortMode.None).Length;
        for (int i = u; i < friendSpawnCount; i++)
        {
            float angle = (i / (float)friendSpawnCount) * 360f * Mathf.Deg2Rad;
            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * friendSpawnRadius;
            Vector3 spawnPos = transform.position + (Vector3)offset;
            Instantiate(friendMinionPrefab, spawnPos, Quaternion.identity);
            yield return new WaitForSeconds(0.15f);
        }

        yield return new WaitForSeconds(0.8f);
    }

    // ─────────────────────────────────────────────
    //  PHASE 3 — Simultaneous Phase 1 attacks
    // ─────────────────────────────────────────────
    private IEnumerator Phase3CombinedAttack()
    {
        isBusy = true;
        PlaySFX(fireballSFX);

        // Simultaneously fire hexagonal rings AND place bombs
        Coroutine hexRoutine  = StartCoroutine(AttackHexFireballsUnlocked());
        Coroutine bombRoutine = StartCoroutine(AttackBombsUnlocked(4));

        yield return hexRoutine;
        yield return bombRoutine;

        // Then fall into a mini meteor cycle (same as Phase 2)
        yield return StartCoroutine(Phase2MeteorCycle());

        isBusy = false;
    }

    // Unlocked versions that don't set isBusy (called from Phase 3 which controls it)
    private IEnumerator AttackHexFireballsUnlocked()
    {
        for (int ring = 0; ring < hexRings + 1; ring++)
        {
            SpawnHexRing(transform.position);
            yield return new WaitForSeconds(fireballInterval * 0.8f);
        }
    }

    private IEnumerator AttackBombsUnlocked(int count)
    {
        List<Vector2> usedPositions = new List<Vector2>();

        for (int i = 0; i < count; i++)
        {
            Vector2 bombPos = PickBombTile(usedPositions);
            usedPositions.Add(bombPos);

            GameObject bomb = Instantiate(bombPrefab, bombPos, Quaternion.identity);
            BombProjectile bp = bomb.GetComponent<BombProjectile>();
            if (bp != null)
                bp.Initialize(bombFuseTime * 0.85f, 1);

            yield return new WaitForSeconds(bombPlacementDelay * 0.75f);
        }
    }

    // ─────────────────────────────────────────────
    //  PHASE 4 — Triple meteor finale
    // ─────────────────────────────────────────────
    private IEnumerator Phase4Finale()
    {
        isBusy = true;
        EnterMeteorForm();

        // Spawn 2 half-damage clones
        Vector3 cloneOffsetA = new Vector3(-1.5f, 0f, 0f);
        Vector3 cloneOffsetB = new Vector3( 1.5f, 0f, 0f);
        GameObject cloneA = Instantiate(meteorClonePrefab, transform.position + cloneOffsetA, Quaternion.identity);
        GameObject cloneB = Instantiate(meteorClonePrefab, transform.position + cloneOffsetB, Quaternion.identity);

        MeteorClone mcA = cloneA.GetComponent<MeteorClone>();
        MeteorClone mcB = cloneB.GetComponent<MeteorClone>();
        if (mcA) mcA.Initialize(playerTarget, meteorDashSpeed, fireTrailPrefab, arenaMin, arenaMax);
        if (mcB) mcB.Initialize(playerTarget, meteorDashSpeed, fireTrailPrefab, arenaMin, arenaMax);

        // Boss itself does its final frenzied dashes
        for (int dash = 0; dash < meteorDashCount + 2; dash++)
        {
            yield return StartCoroutine(MeteorDash());
            yield return new WaitForSeconds(0.2f);
        }

        // Final landing — defeat the boss
        yield return StartCoroutine(MeteorLandCenter());

        Destroy(cloneA);
        Destroy(cloneB);

        ExitMeteorForm();
        DefeatBoss();
    }

    // ─────────────────────────────────────────────
    //  Damage / Defeat
    // ─────────────────────────────────────────────
    public void TakeDamage(int amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        PlaySFX(hurtSFX);

        // Flash red
        StartCoroutine(FlashColor(Color.red, 0.12f));

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            DefeatBoss();
        }
        else
        {
            CheckPhaseTransition();
        }
    }

    private void DefeatBoss()
    {
        if (isDead) return;
        isDead = true;

        StopAllCoroutines();
        foreach(MeteorClone a in FindObjectsByType<MeteorClone>(FindObjectsSortMode.None))
        {
            Destroy(a);
        }
        foreach (ShadowMantleMinion m in FindObjectsByType<ShadowMantleMinion>(FindObjectsSortMode.None))
        {
            Destroy(m);
        }
        PlaySFX(defeatSFX);

        StartCoroutine(DefeatAnimation());
    }

    private IEnumerator DefeatAnimation()
    {
        // Spin and shrink
        float t = 0f;
        Vector3 startScale = transform.localScale;
        while (t < 1.2f)
        {
            t += Time.deltaTime;
            transform.Rotate(0f, 0f, 360f * Time.deltaTime * 2f);
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t / 1.2f);
            yield return null;
        }

        // Notify any boss-fight manager
        BossFightManager manager = FindFirstObjectByType<BossFightManager>();
        if (manager != null) manager.OnBossDefeated();

        Destroy(gameObject);
    }

    // ─────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────
    private void DoFigure8Movement()
    {
        figure8Timer += Time.deltaTime * FIGURE8_SPEED;
        float x = Mathf.Sin(figure8Timer)       * FIGURE8_XSCALE;
        float y = Mathf.Sin(figure8Timer * 2f)  * FIGURE8_YSCALE;
        transform.position = Vector3.Lerp(
            transform.position,
            new Vector3(x, y, 0f),
            Time.deltaTime * moveSpeed);
    }

    private IEnumerator MoveToPosition(Vector3 target, float duration)
    {
        Vector3 start = transform.position;
        float   elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(start, target, elapsed / duration);
            yield return null;
        }
        transform.position = target;
    }

    private void SpawnProjectile(GameObject prefab, Vector3 origin, Vector2 direction, float speed, int damage)
    {
        if (prefab == null) return;
        GameObject proj = Instantiate(prefab, origin, Quaternion.identity);
        Projectile p = proj.GetComponent<Projectile>();
        if (p != null) p.Initialize(direction, speed, damage);
    }

    private void SpawnFireTrail()
    {
        if (fireTrailPrefab == null) return;
        GameObject trail = Instantiate(fireTrailPrefab, transform.position, Quaternion.identity);
        Destroy(trail, 1.5f);
    }

    private void EnterMeteorForm()
    {
        isInMeteorForm = true;
        transform.localScale = Vector3.one * 1.3f;
        if (spriteRenderer) spriteRenderer.color = new Color(1f, 0.4f, 0.1f);
    }

    private void ExitMeteorForm()
    {
        isInMeteorForm = false;
        transform.localScale = Vector3.one;
        if (spriteRenderer) spriteRenderer.color = Color.white;
    }

    private IEnumerator FlashColor(Color color, float duration)
    {
        if (spriteRenderer == null) yield break;
        Color original = spriteRenderer.color;
        spriteRenderer.color = color;
        yield return new WaitForSeconds(duration);
        spriteRenderer.color = original;
    }

    private Vector3 ArenaCenter() =>
        new Vector3((arenaMin.x + arenaMax.x) * 0.5f,
                    (arenaMin.y + arenaMax.y) * 0.5f, 0f);

    /// <summary>
    /// Returns a pixel-snapped world position on an empty floor tile,
    /// excluding any positions already used this attack wave.
    /// Falls back to a raw random point if ArenaTileGrid is not in the scene.
    /// </summary>
    private Vector2 PickBombTile(System.Collections.Generic.List<Vector2> alreadyUsed)
    {
        if (ArenaTileGrid.Instance != null)
        {
            return alreadyUsed.Count == 0
                ? ArenaTileGrid.Instance.GetRandomEmptyTile(edgeMarginTiles: 1)
                : ArenaTileGrid.Instance.GetRandomEmptyTileExcluding(alreadyUsed, edgeMarginTiles: 1);
        }

        // Fallback when no grid is present — snap to 16px units manually
        Vector2 raw    = RandomArenaPoint(1f);
        float   snapX  = Mathf.Round(raw.x)+0.5f;
        float   snapY  = Mathf.Round(raw.y)+0.5f;
        return new Vector2(snapX, snapY);
    }

    private Vector2 RandomArenaPoint(float edgeMargin) =>
        new Vector2(Random.Range(arenaMin.x + edgeMargin, arenaMax.x - edgeMargin),
                    Random.Range(arenaMin.y + edgeMargin, arenaMax.y - edgeMargin));

    private void PlaySFX(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }

    // ─────────────────────────────────────────────
    //  Collision — player's sword/attack hits boss
    // ─────────────────────────────────────────────

#if UNITY_EDITOR
    // Draw arena gizmo in Scene view
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 center = new Vector3((arenaMin.x + arenaMax.x) * 0.5f,
                                     (arenaMin.y + arenaMax.y) * 0.5f, 0f);
        Vector3 size   = new Vector3(arenaMax.x - arenaMin.x,
                                     arenaMax.y - arenaMin.y, 0f);
        Gizmos.DrawWireCube(center, size);
    }
#endif
}