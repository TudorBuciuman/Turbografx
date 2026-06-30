using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ArenaTileGrid — singleton that owns the arena's walkability matrix.
///
/// Each cell maps to one 16x16 pixel sprite tile in world space.
/// Cells are marked blocked by walls, and the boss queries this grid
/// when choosing bomb spawn positions so bombs always land on open floor.
///
/// SETUP (two ways — pick one):
///
///   A) AUTO-SCAN (recommended for Tilemap setups)
///      • Attach this script to any persistent GameObject (e.g. "ArenaManager").
///      • Set originWorldPos to the bottom-left corner of your arena.
///      • Set gridWidth / gridHeight to the tile dimensions of your arena.
///      • Assign the wallLayer mask so the scan can detect wall colliders.
///      • Call RebuildGrid() any time the layout changes (or it runs in Start).
///
///   B) MANUAL PAINT
///      • Same as above but call SetBlocked(col, row, true/false) from your
///        level editor script instead of relying on the physics scan.
///
/// PIXEL SETTINGS:
///   pixelsPerUnit must match your sprites' Pixels Per Unit import setting (default 16).
///   One tile = 1f / pixelsPerUnit * tileSize world units wide/tall.
///   e.g. 16px tile at 16 PPU = 1 world unit per tile (most common for 8-bit games).
/// </summary>
public class ArenaTileGrid : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Singleton
    // ─────────────────────────────────────────────
    public static ArenaTileGrid Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ─────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────
    [Header("Grid Dimensions")]
    [Tooltip("Bottom-left corner of the arena in world space")]
    public Vector2 originWorldPos = new Vector2(-8f, -4f);

    [Tooltip("Number of tiles across (X)")]
    public int gridWidth = 16;

    [Tooltip("Number of tiles tall (Y)")]
    public int gridHeight = 8;

    [Header("Pixel / Tile Settings")]
    [Tooltip("Must match the Pixels Per Unit of your sprite imports")]
    public float pixelsPerUnit = 16f;

    [Tooltip("Tile size in pixels (almost always 16 for 8-bit games)")]
    public int tileSize = 16;

    [Header("Wall Detection (auto-scan)")]
    [Tooltip("Physics layer(s) that count as walls / blocked tiles")]
    public LayerMask wallLayer;

    [Tooltip("Radius of the overlap circle used to detect walls per tile. " +
             "Keep below 0.5 * tileWorldSize to avoid false positives.")]
    [Range(0.05f, 0.45f)]
    public float scanRadius = 0.3f;

    [Header("Debug")]
    public bool drawGizmos = true;

    // ─────────────────────────────────────────────
    //  Internal
    // ─────────────────────────────────────────────

    // true = passable floor, false = blocked wall/obstacle
    private bool[,] walkable;

    /// <summary>World-space size of one tile.</summary>
    public float TileWorldSize => tileSize / pixelsPerUnit;

    // ─────────────────────────────────────────────
    //  Unity
    // ─────────────────────────────────────────────
    void Start()
    {
        RebuildGrid();
    }

    // ─────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────

    /// <summary>
    /// Scans the arena with Physics2D overlap checks and rebuilds the walkability matrix.
    /// Call this whenever walls are added or removed at runtime.
    /// </summary>
    public void RebuildGrid()
    {
        walkable = new bool[gridWidth, gridHeight];

        for (int col = 0; col < gridWidth; col++)
        {
            for (int row = 0; row < gridHeight; row++)
            {
                Vector2 center = TileCenterWorld(col, row);
                // A tile is walkable if nothing on the wall layer overlaps it
                Collider2D hit = Physics2D.OverlapCircle(center, scanRadius, wallLayer);
                walkable[col, row] = (hit == null);
            }
        }

    }

    /// <summary>
    /// Manually mark a tile as blocked or open.
    /// Use from a level editor script if you prefer not to rely on physics scanning.
    /// </summary>
    public void SetBlocked(int col, int row, bool blocked)
    {
        if (!InBounds(col, row)) return;
        walkable[col, row] = !blocked;
    }

    /// <summary>
    /// Returns whether the tile at (col, row) is walkable floor.
    /// </summary>
    public bool IsWalkable(int col, int row)
    {
        if (!InBounds(col, row)) return false;
        return walkable[col, row];
    }

    /// <summary>
    /// Converts a world position to the nearest tile's grid coordinates.
    /// Returns false if the position is outside the grid.
    /// </summary>
    public bool WorldToTile(Vector2 worldPos, out int col, out int row)
    {
        float tws = TileWorldSize;
        col = Mathf.FloorToInt((worldPos.x - originWorldPos.x) / tws);
        row = Mathf.FloorToInt((worldPos.y - originWorldPos.y) / tws);
        return InBounds(col, row);
    }

    /// <summary>
    /// Converts grid coordinates to the world-space center of that tile.
    /// Snapped to the pixel grid — safe to use as a spawn position.
    /// </summary>
    public Vector2 TileCenterWorld(int col, int row)
    {
        float tws = TileWorldSize;
        float halfPx = 0.5f / pixelsPerUnit; // sub-pixel centre offset

        float x = originWorldPos.x + col * tws + tws * 0.5f;
        float y = originWorldPos.y + row * tws + tws * 0.5f;

        // Snap to pixel grid
        x = Mathf.Round(x * pixelsPerUnit) / pixelsPerUnit + halfPx;
        y = Mathf.Round(y * pixelsPerUnit) / pixelsPerUnit + halfPx;

        return new Vector2(x, y);
    }

    /// <summary>
    /// Returns a random walkable tile centre in world space.
    /// Optionally excludes tiles within <edgeMarginTiles> tiles of the arena border.
    /// Returns Vector2.zero and logs a warning if no open tile is found.
    /// </summary>
    public Vector2 GetRandomEmptyTile(int edgeMarginTiles = 1)
    {
        // Build candidate list once per call — fast enough for small arenas
        List<Vector2Int> candidates = new List<Vector2Int>();

        int colMin = edgeMarginTiles;
        int colMax = gridWidth - edgeMarginTiles - 1;
        int rowMin = edgeMarginTiles;
        int rowMax = gridHeight - edgeMarginTiles - 1;

        for (int col = colMin; col <= colMax; col++)
            for (int row = rowMin; row <= rowMax; row++)
                if (walkable[col, row])
                    candidates.Add(new Vector2Int(col, row));

        if (candidates.Count == 0)
        {
            Debug.LogWarning("[ArenaTileGrid] No empty tiles found — returning origin.");
            return originWorldPos;
        }

        Vector2Int chosen = candidates[Random.Range(0, candidates.Count)];
        return TileCenterWorld(chosen.x, chosen.y);
    }

    /// <summary>
    /// Same as GetRandomEmptyTile but excludes tiles currently occupied by
    /// existing bombs (pass their world positions to avoid overlap).
    /// </summary>
    public Vector2 GetRandomEmptyTileExcluding(IEnumerable<Vector2> occupiedPositions,
                                                int edgeMarginTiles = 1)
    {
        // Mark occupied tiles
        HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();
        foreach (Vector2 pos in occupiedPositions)
        {
            if (WorldToTile(pos, out int oc, out int or))
                occupied.Add(new Vector2Int(oc, or));
        }

        List<Vector2Int> candidates = new List<Vector2Int>();
        int colMin = edgeMarginTiles, colMax = gridWidth - edgeMarginTiles - 1;
        int rowMin = edgeMarginTiles, rowMax = gridHeight - edgeMarginTiles - 1;

        for (int col = colMin; col <= colMax; col++)
            for (int row = rowMin; row <= rowMax; row++)
                if (walkable[col, row] && !occupied.Contains(new Vector2Int(col, row)))
                    candidates.Add(new Vector2Int(col, row));

        if (candidates.Count == 0)
            return GetRandomEmptyTile(edgeMarginTiles); // fall back, allowing overlap

        Vector2Int chosen = candidates[Random.Range(0, candidates.Count)];
        return TileCenterWorld(chosen.x, chosen.y);
    }

    // ─────────────────────────────────────────────
    //  Internal helpers
    // ─────────────────────────────────────────────
    private bool InBounds(int col, int row) =>
        col >= 0 && col < gridWidth && row >= 0 && row < gridHeight;

    // ─────────────────────────────────────────────
    //  Gizmos
    // ─────────────────────────────────────────────
#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;

        float tws = TileWorldSize;

        for (int col = 0; col < gridWidth; col++)
        {
            for (int row = 0; row < gridHeight; row++)
            {
                Vector2 center = TileCenterWorld(col, row);

                bool isOpen = (walkable != null)
                    ? walkable[col, row]
                    : (Physics2D.OverlapCircle(center, scanRadius, wallLayer) == null);

                Gizmos.color = isOpen
                    ? new Color(0f, 1f, 0f, 0.15f)   // green tint = open
                    : new Color(1f, 0f, 0f, 0.25f);   // red tint   = blocked

                Gizmos.DrawCube(center, new Vector3(tws * 0.9f, tws * 0.9f, 0f));

                // Border
                Gizmos.color = new Color(1f, 1f, 1f, 0.08f);
                Gizmos.DrawWireCube(center, new Vector3(tws, tws, 0f));
            }
        }
    }
#endif
}