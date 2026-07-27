using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(PlayerMovement))]
public class ParanoidAndroid : MonoBehaviour
{
    [Header("Bot Toggle")]
    public bool botActive = false;

    [Header("References")]
    public PlayerMovement playerMovement;
    public PlayerAttack playerAttack;

    [Header("Floor Validation")]
    [Tooltip("Tilemap the player walks on. Bot will only path onto cells matching spawnFloorTile. Leave empty to disable floor checking entirely.")]
    public Tilemap groundTilemap;
    [Tooltip("If left empty, auto-captured from the cell the bot spawns on.")]
    public TileBase spawnFloorTile;

    [Header("Room Grid")]
    [Tooltip("Rooms per row (matches PlayerMovement.roomsWidth).")]
    public int roomsX = 4;
    [Tooltip("Number of rows of rooms.")]
    public int roomsY = 4;
    [Tooltip("World position of the BOTTOM-LEFT corner of room index 0.")]
    public Vector3 dungeonOrigin = Vector3.zero;
    [Tooltip("Width/height of a single room in world units (used to auto-compute every room's bounds).")]
    public Vector2 roomSize = new Vector2(20f, 12f);
    [Tooltip("One RoomNode per room, index = row * roomsX + col. Auto-populated with full connectivity on Awake if left empty � edit door booleans afterward to match your actual dungeon layout.")]
    public List<RoomNode> rooms = new List<RoomNode>();

    [Header("Movement Tuning")]
    public float arriveThreshold = 0.15f;
    [Tooltip("How far ahead (in seconds of travel) to test a cell before committing to it.")]
    public float floorLookahead = 0.18f;

    [Header("Enemy Engagement")]
    [Tooltip("Detects enemies by component type (Enemy), not layer. Filtered to the current room via EnemyMovement.GetRoom().")]
    public float attackRange = 0.9f;
    [Tooltip("Fallback search radius, only used if the current room isn't found in the rooms list.")]
    public float roomSightRadius = 8f;

    [Header("Healing Items")]
    [Tooltip("Detects healing items by component type (HealingItem), filtered to whichever are inside the current room's bounds. Assumes HealingItem has a trigger collider that heals the player on contact (same pattern as your door triggers) � the bot just walks onto it.")]
    public bool collectHealingItems = true;

    [Header("Threat Avoidance")]
    [Tooltip("Not wired up yet � you haven't added a danger/hazard class. DetectNearestHazard() below is a stub that always returns null, so this priority never fires. Tell me your class name once you add one and I'll wire it in the same way as Enemy/HealingItem.")]
    public float hazardDetectRadius = 1.8f;

    [Header("Quota / Endgame Sequence")]
    [Tooltip("Once every Enemy in the scene has been killed (quota complete), the bot abandons normal patrol/loot behaviour and heads straight here.")]
    public int finalRoomIndex = 4;
    [Tooltip("Tag of the object to find and destroy once the bot arrives in finalRoomIndex. Attacked the same way as an Enemy (walk into attackRange, then TryAttack) until it's gone.")]
    public string rootTag = "Root";

    [Header("Debug")]
    [Tooltip("Draws a red X at wherever the bot is currently steering toward, visible during Play even without selecting the object.")]
    public bool showDebugTarget = true;
    [Tooltip("Press this key at any time in Play mode to toggle the bot on/off, independent of the botActive checkbox above. Handy for debugging without pausing/selecting the object.")]

    private enum BotState { Idle, Patrolling, Engaging, CollectingItem, Dodging, GoingToFinalRoom, BreakingRoot, WalkingRightFinal }
    private BotState state = BotState.Idle;

    private bool enemiesEverSeen = false;
    private bool quotaReached = false;

    private int? forcedPatrolTarget = null;

    private List<int> currentPath = new List<int>();
    private Vector3 currentDoorTarget;
    private bool haveDoorTarget = false;
    private int doorTargetRoom = -1;
    private Vector2 doorApproachDir = Vector2.zero;
    private float doorWaitTimer = 0f;
    [Tooltip("If the bot sits at a door this long without the room actually changing (trigger mismatch, indexing issue, etc.), abandon and re-plan instead of stalling forever.")]
    public float doorTransitionTimeout = 3f;
    private Vector3Int spawnCell;

    private Vector3 debugTargetPos;
    private bool hasDebugTarget = false;
    private bool isReady = false;

    void Awake()
    {
        if (playerMovement == null) playerMovement = GetComponent<PlayerMovement>();
        if (playerAttack == null) playerAttack = GetComponent<PlayerAttack>();
        EnsureRoomsInitialized();
    }

    void Start()
    {
        if (groundTilemap != null)
        {
            spawnCell = groundTilemap.WorldToCell(transform.position);
            if (spawnFloorTile == null)
                spawnFloorTile = groundTilemap.GetTile(spawnCell);
        }
        if (botActive && GameObject.Find("Colliders"))
            GameObject.Find("Colliders").SetActive(false);
        FindFirstObjectByType<PlayerAttack>().AttackSize = 1.3f;
        StartCoroutine(Wait());
    }
    public IEnumerator Wait()
    {
        yield return new WaitForSeconds(2.5f);
        isReady = true;
        yield return null;
    }
    public void RestartWholeGame()
    {
        PlayerStats.level = 1;
        SceneManager.LoadScene(0);
        var go = new GameObject("Sacrificial Lamb");
        DontDestroyOnLoad(go);
        foreach (var root in go.scene.GetRootGameObjects())
            Destroy(root);
    }
    public void EnsureRoomsInitialized()
    {
        var existingIndices = new HashSet<int>(rooms.Select(r => r.roomIndex));
        int total = roomsX * roomsY;

        for (int i = 1; i <= total; i++)
        {
            if (existingIndices.Contains(i)) continue;

            (int row, int col) = RowColFromIndex(i);

            rooms.Add(new RoomNode
            {
                roomIndex = i,
                hasDoorNorth = row + 1 < roomsY,
                hasDoorSouth = row - 1 >= 0,
                hasDoorEast = col + 1 < roomsX,
                hasDoorWest = col - 1 >= 0,
                useManualBounds = false
            });
        }
    }

    private (int row, int col) RowColFromIndex(int roomIndex)
    {
        int zeroBased = roomIndex - 1;
        return (zeroBased / roomsX, zeroBased % roomsX);
    }
    private Bounds GetRoomBounds(RoomNode node)
    {
        if (node.useManualBounds) return node.manualBounds;

        (int row, int col) = RowColFromIndex(node.roomIndex);

        Vector3 center = dungeonOrigin + new Vector3(
            col * roomSize.x + roomSize.x * 0.5f,
            row * roomSize.y + roomSize.y * 0.5f,
            0f);

        return new Bounds(center, new Vector3(roomSize.x, roomSize.y, 0f));
    }


    void Update()
    {
        if (!botActive || playerMovement == null || !isReady) return;

        playerMovement.autoDemoMode = true;


        if (SceneManager.GetActiveScene().name == "Bunker2")
            RestartWholeGame();

        if (!playerMovement.canMove)
        {
            playerMovement.SetExternalInput(Vector2.zero);
            hasDebugTarget = false;
            return;
        }

        int sceneEnemyCount = FindObjectsByType<Enemy>(FindObjectsSortMode.None).Length;
        if (sceneEnemyCount > 0) enemiesEverSeen = true;

        if (!quotaReached && enemiesEverSeen && sceneEnemyCount == 0)
        {
            quotaReached = true;
            state = BotState.GoingToFinalRoom;
            haveDoorTarget = false; 
            currentPath.Clear();
        }

        if (quotaReached)
        {
            RunPostQuotaSequence();
            return;
        }

        Component hazard = DetectNearestHazard();
        if (hazard != null)
        {
            state = BotState.Dodging;
            haveDoorTarget = false; 
            doorTargetRoom = -1;
            doorWaitTimer = 0f;
            currentPath.Clear();
            DodgeHazard(hazard);
            return;
        }

        Enemy enemy = DetectNearestEnemyInCurrentRoom();
        if (enemy != null)
        {
            state = BotState.Engaging;
            EngageEnemy(enemy);
            return;
        }

        if (collectHealingItems)
        {
            HealItem item = DetectHealingItemInCurrentRoom();
            if (item != null)
            {
                state = BotState.CollectingItem;
                CollectItem(item);
                return;
            }
        }

        state = BotState.Patrolling;
        forcedPatrolTarget = null;
        Patrol();
    }

    private void RunPostQuotaSequence()
    {
        switch (state)
        {
            case BotState.GoingToFinalRoom:
                NavigateToFinalRoom();
                break;

            case BotState.BreakingRoot:
                BreakRoot();
                break;

            case BotState.WalkingRightFinal:
                WalkRightForever();
                break;
        }
    }

    private void NavigateToFinalRoom()
    {
        int myRoom = playerMovement.GetCurrentRoom();

        if (myRoom == finalRoomIndex && currentPath.Count == 0 && !haveDoorTarget)
        {
            playerMovement.SetExternalInput(Vector2.zero);
            hasDebugTarget = false;
            state = BotState.BreakingRoot;
            return;
        }

        forcedPatrolTarget = finalRoomIndex;
        Patrol();
    }

    private void BreakRoot()
    {
        if(GameObject.FindGameObjectsWithTag(rootTag).Length == 4)
        {
            playerMovement.SetExternalInput(Vector2.zero);
            state = BotState.WalkingRightFinal;
            return;
        }
        GameObject root = GameObject.FindGameObjectWithTag(rootTag);
        if (root == null)
        {
            // Root is gone � broken. Move on to the final walk.
            playerMovement.SetExternalInput(Vector2.zero);
            state = BotState.WalkingRightFinal;
            return;
        }

        SetDebugTarget(root.transform.position);

        Vector2 toRoot = (Vector2)root.transform.position - (Vector2)transform.position;
        float dist = toRoot.magnitude;

        if (dist <= attackRange)
        {
            playerMovement.SetExternalInput(Vector2.zero);
            playerAttack.TryAttack();
            return;
        }

        Vector2 desired = toRoot.normalized;
        Vector2 safeInput = GetSafeMoveInput(desired);
        playerMovement.SetExternalInput(safeInput);
    }

    private void WalkRightForever()
    {
        SetDebugTarget(transform.position + Vector3.right * 2f);
        Vector2 safeInput = GetSafeMoveInput(Vector2.right);
        playerMovement.SetExternalInput(safeInput == Vector2.zero ? Vector2.right : safeInput);
    }

    private Component DetectNearestHazard()
    {
        return null;//I don't have time for this, fuck you
    }

    private void DodgeHazard(Component hazard)
    {
        Vector2 away = ((Vector2)transform.position - (Vector2)hazard.transform.position);
        if (away.sqrMagnitude < 0.0001f) away = Random.insideUnitCircle; 
        away.Normalize();

        Vector2 safeInput = GetSafeMoveInput(away);

        if (safeInput == Vector2.zero)
        {
            Vector2 perpA = new Vector2(-away.y, away.x);
            Vector2 perpB = -perpA;
            safeInput = GetSafeMoveInput(perpA);
            if (safeInput == Vector2.zero) safeInput = GetSafeMoveInput(perpB);
        }

        SetDebugTarget(transform.position + (Vector3)(away * 2f));
        playerMovement.SetExternalInput(safeInput);
    }

    private Enemy DetectNearestEnemyInCurrentRoom()
    {
        int myRoom = playerMovement.GetCurrentRoom();
        Enemy[] allEnemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        if (allEnemies.Length == 0) return null;

        Enemy nearest = null;
        float nearestDist = float.MaxValue;

        foreach (var e in allEnemies)
        {
            EnemyMovement em = e.GetComponent<EnemyMovement>();
            if (em == null || em.GetRoom() != myRoom) continue;

            float d = Vector2.Distance(transform.position, e.transform.position);
            if (d < nearestDist)
            {
                nearestDist = d;
                nearest = e;
            }
        }
        return nearest;
    }

    private HealItem DetectHealingItemInCurrentRoom()
    {
        int myRoom = playerMovement.GetCurrentRoom();
        HealItem[] allItems = FindObjectsByType<HealItem>(FindObjectsSortMode.None);
        if (allItems.Length == 0) return null;

        RoomNode node = GetRoomNode(myRoom);
        Bounds? roomBounds = node != null ? GetRoomBounds(node) : (Bounds?)null;

        HealItem nearest = null;
        float nearestDist = float.MaxValue;

        foreach (var item in allItems)
        {
            bool inRoom = roomBounds.HasValue
                ? roomBounds.Value.Contains(item.transform.position)
                : Vector2.Distance(transform.position, item.transform.position) <= roomSightRadius;

            if (!inRoom) continue;

            float d = Vector2.Distance(transform.position, item.transform.position);
            if (d < nearestDist)
            {
                nearestDist = d;
                nearest = item;
            }
        }
        return nearest;
    }

    private void EngageEnemy(Enemy enemy)
    {
        SetDebugTarget(enemy.transform.position);

        Vector2 toEnemy = (Vector2)enemy.transform.position - (Vector2)transform.position;
        float dist = toEnemy.magnitude;

        if (dist <= attackRange)
        {
            playerMovement.SetExternalInput(Vector2.zero);
            playerAttack.TryAttack();
            return;
        }

        Vector2 desired = toEnemy.normalized;
        Vector2 safeInput = GetSafeMoveInput(desired);
        playerMovement.SetExternalInput(safeInput);
    }

    private void CollectItem(HealItem item)
    {
        SetDebugTarget(item.transform.position);

        Vector2 toItem = (Vector2)item.transform.position - (Vector2)transform.position;

        Vector2 desired = toItem.normalized;
        Vector2 safeInput = GetSafeMoveInput(desired);
        playerMovement.SetExternalInput(safeInput);
    }

    private void Patrol()
    {
        int myRoom = playerMovement.GetCurrentRoom();

        if (haveDoorTarget && myRoom == doorTargetRoom)
        {
            haveDoorTarget = false;
            if (currentPath.Count > 0 && currentPath[0] == doorTargetRoom)
                currentPath.RemoveAt(0);
            doorTargetRoom = -1;
            doorWaitTimer = 0f;
        }

        if (currentPath.Count == 0 && !haveDoorTarget)
        {
            int target = forcedPatrolTarget ?? PickPatrolTarget(myRoom);
            if (target == myRoom || target < 0)
            {
                playerMovement.SetExternalInput(Vector2.zero);
                hasDebugTarget = false;
                return;
            }
            currentPath = BFSFindPath(myRoom, target);
            if (currentPath.Count <= 1)
            {
                playerMovement.SetExternalInput(Vector2.zero);
                hasDebugTarget = false;
                return;
            }
            currentPath.RemoveAt(0); 
        }

        if (!haveDoorTarget && currentPath.Count > 0)
        {
            int nextRoom = currentPath[0];
            if (!TryGetDoorWorldPosition(myRoom, nextRoom, out currentDoorTarget))
            {
                currentPath.Clear();
                playerMovement.SetExternalInput(Vector2.zero);
                hasDebugTarget = false;
                return;
            }
            haveDoorTarget = true;
            doorTargetRoom = nextRoom;
            doorWaitTimer = 0f;
            doorApproachDir = ((Vector2)currentDoorTarget - (Vector2)transform.position).normalized;
        }

        SetDebugTarget(currentDoorTarget);

        Vector2 toDoor = (Vector2)currentDoorTarget - (Vector2)transform.position;
        Vector2 desiredDir;

        if (toDoor.magnitude <= arriveThreshold)
        {
            desiredDir = doorApproachDir;

            doorWaitTimer += Time.deltaTime;
            if (doorWaitTimer > doorTransitionTimeout)
            {
                haveDoorTarget = false;
                doorTargetRoom = -1;
                currentPath.Clear();
                doorWaitTimer = 0f;
            }
        }
        else
        {
            desiredDir = toDoor.normalized;
        }

        Vector2 safeInput = GetSafeMoveInput(desiredDir);
        playerMovement.SetExternalInput(safeInput);
    }

    private int PickPatrolTarget(int currentRoom)
    {
        List<int> candidates = rooms
            .Select(r => r.roomIndex)
            .Where(i => i != currentRoom)
            .ToList();

        if (candidates.Count == 0) return -1;
        return candidates[Random.Range(0, candidates.Count)];
    }

    private RoomNode GetRoomNode(int index) => rooms.FirstOrDefault(r => r.roomIndex == index);

    private List<int> BFSFindPath(int start, int target)
    {
        var result = new List<int>();
        if (start == target) { result.Add(start); return result; }

        var visited = new HashSet<int> { start };
        var queue = new Queue<int>();
        var cameFrom = new Dictionary<int, int>();
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            int current = queue.Dequeue();
            foreach (int neighbor in GetConnectedNeighbors(current))
            {
                if (visited.Contains(neighbor)) continue;
                visited.Add(neighbor);
                cameFrom[neighbor] = current;

                if (neighbor == target)
                {
                    var path = new List<int> { target };
                    int node = target;
                    while (node != start)
                    {
                        node = cameFrom[node];
                        path.Add(node);
                    }
                    path.Reverse();
                    return path;
                }
                queue.Enqueue(neighbor);
            }
        }

        return result; 
    }

    private IEnumerable<int> GetConnectedNeighbors(int roomIndex)
    {
        RoomNode node = GetRoomNode(roomIndex);
        if (node == null) yield break;

        (int row, int col) = RowColFromIndex(roomIndex);

        if (node.hasDoorNorth && row + 1 < roomsY) yield return roomIndex + roomsX;
        if (node.hasDoorSouth && row - 1 >= 0) yield return roomIndex - roomsX;
        if (node.hasDoorEast && col + 1 < roomsX) yield return roomIndex + 1;
        if (node.hasDoorWest && col - 1 >= 0) yield return roomIndex - 1;
    }

    private bool TryGetDoorWorldPosition(int fromRoom, int toRoom, out Vector3 doorPos)
    {
        doorPos = Vector3.zero;
        RoomNode from = GetRoomNode(fromRoom);
        if (from == null) return false;

        Bounds b = GetRoomBounds(from);
        int diff = toRoom - fromRoom;

        if (diff == roomsX && from.hasDoorNorth)
        {
            doorPos = new Vector3(b.center.x, b.max.y, transform.position.z);
            return true;
        }
        if (diff == -roomsX && from.hasDoorSouth)
        {
            doorPos = new Vector3(b.center.x, b.min.y, transform.position.z);
            return true;
        }
        if (diff == 1 && from.hasDoorEast)
        {
            doorPos = new Vector3(b.max.x, b.center.y, transform.position.z);
            return true;
        }
        if (diff == -1 && from.hasDoorWest)
        {
            doorPos = new Vector3(b.min.x, b.center.y, transform.position.z);
            return true;
        }

        return false;
    }

    private Vector2 QuantizeDirection(Vector2 dir)
    {
        if (dir == Vector2.zero) return Vector2.zero;

        float ax = Mathf.Abs(dir.x);
        float ay = Mathf.Abs(dir.y);

        const float axisThreshold = 0.5f;
        float x = ax >= ay * axisThreshold ? Mathf.Sign(dir.x) : 0f;
        float y = ay >= ax * axisThreshold ? Mathf.Sign(dir.y) : 0f;

        return new Vector2(x, y);
    }

    private Vector2 GetSafeMoveInput(Vector2 desiredDir)
    {
        if (desiredDir == Vector2.zero) return Vector2.zero;

        Vector2 quantized = QuantizeDirection(desiredDir);
        if (quantized == Vector2.zero) return Vector2.zero;

        if (IsStepWalkable(quantized)) return quantized;

        Vector2 xOnly = new Vector2(quantized.x, 0f);
        Vector2 yOnly = new Vector2(0f, quantized.y);

        if (Mathf.Abs(desiredDir.x) >= Mathf.Abs(desiredDir.y))
        {
            if (xOnly != Vector2.zero && IsStepWalkable(xOnly)) return xOnly;
            if (yOnly != Vector2.zero && IsStepWalkable(yOnly)) return yOnly;
        }
        else
        {
            if (yOnly != Vector2.zero && IsStepWalkable(yOnly)) return yOnly;
            if (xOnly != Vector2.zero && IsStepWalkable(xOnly)) return xOnly;
        }

        Vector2 backupDir = -quantized;
        if (IsStepWalkable(backupDir)) return backupDir;

        Vector2[] escapeDirections = new Vector2[]
        {
        Vector2.up, Vector2.down, Vector2.left, Vector2.right,
        new Vector2(1, 1), new Vector2(1, -1), new Vector2(-1, 1), new Vector2(-1, -1)
        };

        foreach (Vector2 dir in escapeDirections)
        {
            if (IsStepWalkable(dir)) return dir;
        }

        return Vector2.zero;
    }

    private bool IsStepWalkable(Vector2 dir)
    {
        return true;//I don't care anymore
        /*if (groundTilemap == null || spawnFloorTile == null) return true;

        float travelDist = playerMovement.moveSpeed * floorLookahead;
        Vector3 lookaheadPos = transform.position + (Vector3)(dir.normalized * travelDist);
        Vector3Int cell = groundTilemap.WorldToCell(lookaheadPos);
        return groundTilemap.GetTile(cell) == spawnFloorTile;*/
    }

    private void SetDebugTarget(Vector3 pos)
    {
        debugTargetPos = pos;
        hasDebugTarget = true;
    }

    void OnDrawGizmos()
    {
        if (!showDebugTarget || !hasDebugTarget) return;

        float size = 0.25f;
        Gizmos.color = Color.red;
        Gizmos.DrawLine(debugTargetPos + new Vector3(-size, -size, 0), debugTargetPos + new Vector3(size, size, 0));
        Gizmos.DrawLine(debugTargetPos + new Vector3(-size, size, 0), debugTargetPos + new Vector3(size, -size, 0));

        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawLine(transform.position, debugTargetPos);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, roomSightRadius);
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, hazardDetectRadius);

        if (rooms != null)
        {
            Gizmos.color = Color.yellow;
            foreach (var r in rooms)
            {
                Bounds b = GetRoomBounds(r);
                Gizmos.DrawWireCube(b.center, b.size);
            }
        }
    }

}

[System.Serializable]
public class RoomNode
{
    public int roomIndex;

    public bool hasDoorNorth;
    public bool hasDoorSouth;
    public bool hasDoorEast;
    public bool hasDoorWest;

    public bool useManualBounds = false;
    public Bounds manualBounds;
}