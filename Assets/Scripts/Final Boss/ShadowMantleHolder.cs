using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShadowMantleHolder : MonoBehaviour
{
    public GameObject fireballPrefab;
    public GameObject bombPrefab;
    public GameObject friendMinionPrefab;
    public GameObject fireTrailPrefab;
    public GameObject meteorClonePrefab;

    [Header("Arena")]
    public Vector2 arenaMin = new Vector2(-7f, -4f);
    public Vector2 arenaMax = new Vector2(5f, 4f);

    [Header("Stats")]
    private int maxHealth = 800;
    public float moveSpeed = 3f;

    [Header("Phase Thresholds")]
    [Range(0f, 1f)] public float phase2Threshold = 0.75f;
    [Range(0f, 1f)] public float phase3Threshold = 0.50f;
    [Range(0f, 1f)] public float phase4Threshold = 0.25f;

    public float fireballInterval = 0.18f;
    public float bombPlacementDelay = 0.40f;
    public float meteorDashSpeed = 14f;
    public float laughPauseDuration = 2.8f;
    public float betweenAttackDelay = 1.2f;

    public float fireballSpeed = 5f;
    public int hexRings = 4;   

    public int bombCount = 3;
    public float bombFuseTime = 1.2f;

    public int meteorDashCount = 4;   
    public float meteorLandRadius = 0.6f;

    public int friendSpawnCount = 5;
    public int friendMaxSpawnCount = 6;
    public float friendSpawnRadius = 2f;

    public AudioClip fireballSFX;
    public AudioClip bombSFX;
    public AudioClip meteorSFX;
    public AudioClip laughSFX;
    public AudioClip spawnMinionSFX;
    public AudioClip hurtSFX;
    public AudioClip defeatSFX;

    private int currentHealth;
    private int currentPhase = 1;
    private bool isBusy = false;   
    private bool isDead = false;
    private bool isInMeteorForm = false;
    private Transform playerTarget;
    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;

    private float figure8Timer = 0f;
    private const float FIGURE8_SPEED = 0.8f;
    private const float FIGURE8_XSCALE = 3.5f;
    private const float FIGURE8_YSCALE = 1.5f;

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

        StartCoroutine(BossBrain());
    }

    void Update()
    {
        if (isDead || isBusy) return;

        if (!isInMeteorForm)
            DoFigure8Movement();

        if (playerTarget != null)
            spriteRenderer.flipX = playerTarget.position.x < transform.position.x;
    }

    private IEnumerator BossBrain()
    {
        yield return new WaitForSeconds(1f); 

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
            }
        }
    }

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
        yield return StartCoroutine(MoveToPosition(ArenaCenter(), 1.5f));
        for (int i = 0; i < 6; i++)
        {
            spriteRenderer.enabled = !spriteRenderer.enabled;
            yield return new WaitForSeconds(0.1f);
        }
        spriteRenderer.enabled = true;
        isBusy = false;
    }


    private IEnumerator PickPhase1Attack()
    {
        if (Random.value > 0.5f)
            yield return StartCoroutine(AttackHexFireballs());
        else
            yield return StartCoroutine(AttackBombs(bombCount));
    }

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
        for (int i = 0; i < 6; i++)
        {
            float angle = i * 60f * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            SpawnProjectile(fireballPrefab, origin, dir, fireballSpeed, 1);
        }
    }

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

    private IEnumerator Phase2MeteorCycle()
    {
        isBusy = true;
        EnterMeteorForm();

        for (int dash = 0; dash < meteorDashCount; dash++)
        {
            yield return StartCoroutine(MeteorDash());
            yield return new WaitForSeconds(0.3f);
        }

        yield return StartCoroutine(MeteorLandCenter());
        yield return StartCoroutine(LaughAndSpawnFriends());

        ExitMeteorForm();
        isBusy = false;
    }

    private IEnumerator MeteorDash()
    {
        if (playerTarget == null) yield break;

        Vector3 topStart = new Vector3(
            Mathf.Clamp(playerTarget.position.x + Random.Range(-2f, 2f),
                        arenaMin.x + 1f, arenaMax.x - 1f),
            arenaMax.y + 1f, 0f);

        transform.position = topStart;
        PlaySFX(meteorSFX);

        Vector3 target = playerTarget.position;
        Vector3 dir = (target - transform.position).normalized;

        while (transform.position.y > arenaMin.y - 1f)
        {
            transform.position += dir * meteorDashSpeed * Time.deltaTime;
            SpawnFireTrail();
            yield return null;
        }

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
        for (float t = 0; t < laughPauseDuration; t += 0.2f)
        {
            transform.localScale = Vector3.one * (1f + Mathf.Sin(t * 18f) * 0.08f);
            yield return new WaitForSeconds(0.2f);
        }
        transform.localScale = Vector3.one;

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

    private IEnumerator Phase3CombinedAttack()
    {
        isBusy = true;
        PlaySFX(fireballSFX);

        Coroutine hexRoutine  = StartCoroutine(AttackHexFireballsUnlocked());
        Coroutine bombRoutine = StartCoroutine(AttackBombsUnlocked(4));

        yield return hexRoutine;
        yield return bombRoutine;

        yield return StartCoroutine(Phase2MeteorCycle());

        isBusy = false;
    }

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

    private IEnumerator Phase4Finale()
    {
        isBusy = true;
        EnterMeteorForm();

        Vector3 cloneOffsetA = new Vector3(-1.5f, 0f, 0f);
        Vector3 cloneOffsetB = new Vector3( 1.5f, 0f, 0f);
        GameObject cloneA = Instantiate(meteorClonePrefab, transform.position + cloneOffsetA, Quaternion.identity);
        GameObject cloneB = Instantiate(meteorClonePrefab, transform.position + cloneOffsetB, Quaternion.identity);

        MeteorClone mcA = cloneA.GetComponent<MeteorClone>();
        MeteorClone mcB = cloneB.GetComponent<MeteorClone>();
        if (mcA) 
            mcA.Initialize(playerTarget, meteorDashSpeed, fireTrailPrefab, arenaMin, arenaMax);
        if (mcB) 
            mcB.Initialize(playerTarget, meteorDashSpeed, fireTrailPrefab, arenaMin, arenaMax);

        for (int dash = 0; dash < meteorDashCount + 2; dash++)
        {
            yield return StartCoroutine(MeteorDash());
            yield return new WaitForSeconds(0.2f);
        }
        yield return StartCoroutine(MeteorLandCenter());

        Destroy(cloneA);
        Destroy(cloneB);

        ExitMeteorForm();
        DefeatBoss();
    }
    public void TakeDamage(int amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        PlaySFX(hurtSFX);

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
            Destroy(a.gameObject);
        }
        foreach (ShadowMantleMinion m in FindObjectsByType<ShadowMantleMinion>(FindObjectsSortMode.None))
        {
            Destroy(m.gameObject);
        }
        PlaySFX(defeatSFX);

        FindFirstObjectByType<BossDialogueTrigger>().TriggerPostFight();
        StartCoroutine(DefeatAnimation());
    }

    private IEnumerator DefeatAnimation()
    {
        float t = 0f;
        Vector3 startScale = transform.localScale;
        while (t < 1.2f)
        {
            t += Time.deltaTime;
            transform.Rotate(0f, 0f, 360f * Time.deltaTime * 2f);
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t / 1.2f);
            yield return null;
        }
        BossFightManager manager = FindFirstObjectByType<BossFightManager>();
        if (manager != null) manager.OnBossDefeated();

        Destroy(gameObject);
    }

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
    private Vector2 PickBombTile(System.Collections.Generic.List<Vector2> alreadyUsed)
    {
        if (ArenaTileGrid.Instance != null)
        {
            return alreadyUsed.Count == 0
                ? ArenaTileGrid.Instance.GetRandomEmptyTile(edgeMarginTiles: 1)
                : ArenaTileGrid.Instance.GetRandomEmptyTileExcluding(alreadyUsed, edgeMarginTiles: 1);
        }

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

#if UNITY_EDITOR
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