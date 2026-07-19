using System.Collections.Generic;
using UnityEngine;

public class ArenaTileGrid : MonoBehaviour
{
    public static ArenaTileGrid Instance { get; private set; }
    public Vector2 originWorldPos = new Vector2(-8f, -4f);
    public int gridWidth = 16;
    public int gridHeight = 8;
    public float pixelsPerUnit = 16f;
    public int tileSize = 16;
    public LayerMask wallLayer;

    [Range(0.05f, 0.45f)]
    public float scanRadius = 0.3f;

    public bool drawGizmos = true;
    private bool[,] walkable;

    public float TileWorldSize => tileSize / pixelsPerUnit;

    void Awake()
    {
        if (Instance != null && Instance != this) { 
            Destroy(gameObject); 
            return; 
        }

        Instance = this;
    }
    void Start()
    {
        RebuildGrid();
    }
    public void RebuildGrid()
    {
        walkable = new bool[gridWidth, gridHeight];

        for (int col = 0; col < gridWidth; col++)
        {
            for (int row = 0; row < gridHeight; row++)
            {
                Vector2 center = TileCenterWorld(col, row);
                Collider2D hit = Physics2D.OverlapCircle(center, scanRadius, wallLayer);
                walkable[col, row] = (hit == null);
            }
        }

    }

    public void SetBlocked(int col, int row, bool blocked)
    {
        if (!InBounds(col, row)) return;
        walkable[col, row] = !blocked;
    }

    public bool IsWalkable(int col, int row)
    {
        if (!InBounds(col, row)) return false;
        return walkable[col, row];
    }

    public bool WorldToTile(Vector2 worldPos, out int col, out int row)
    {
        float tws = TileWorldSize;
        col = Mathf.FloorToInt((worldPos.x - originWorldPos.x) / tws);
        row = Mathf.FloorToInt((worldPos.y - originWorldPos.y) / tws);
        return InBounds(col, row);
    }

    public Vector2 TileCenterWorld(int col, int row)
    {
        float tws = TileWorldSize;
        float halfPx = 0.5f / pixelsPerUnit; 

        float x = originWorldPos.x + col * tws + tws * 0.5f;
        float y = originWorldPos.y + row * tws + tws * 0.5f;

        x = Mathf.Round(x * pixelsPerUnit) / pixelsPerUnit + halfPx;
        y = Mathf.Round(y * pixelsPerUnit) / pixelsPerUnit + halfPx;

        return new Vector2(x, y);
    }

    public Vector2 GetRandomEmptyTile(int edgeMarginTiles = 1)
    {
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
            Debug.LogWarning("FUCK");
            return originWorldPos;
        }

        Vector2Int chosen = candidates[Random.Range(0, candidates.Count)];
        return TileCenterWorld(chosen.x, chosen.y);
    }

    public Vector2 GetRandomEmptyTileExcluding(IEnumerable<Vector2> occupiedPositions,int edgeMarginTiles = 1)
    {
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
            return GetRandomEmptyTile(edgeMarginTiles); 

        Vector2Int chosen = candidates[Random.Range(0, candidates.Count)];
        return TileCenterWorld(chosen.x, chosen.y);
    }

    private bool InBounds(int col, int row) => 
        col >= 0 && col < gridWidth && row >= 0 && row < gridHeight;

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
                    ? new Color(0f, 1f, 0f, 0.15f)   //green tint = open
                    : new Color(1f, 0f, 0f, 0.25f);   //red tint = blocked

                Gizmos.DrawCube(center, new Vector3(tws * 0.9f, tws * 0.9f, 0f));

                //border is white, duh
                Gizmos.color = new Color(1f, 1f, 1f, 0.08f);
                Gizmos.DrawWireCube(center, new Vector3(tws, tws, 0f));
            }
        }
    }
#endif
}