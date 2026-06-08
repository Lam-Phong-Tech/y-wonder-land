using UnityEngine;

/// <summary>
/// Manages the build grid for placement validation.
/// Tracks which cells are occupied, checks if a placement is valid.
/// Grid origin is at (0, 0, 0), each cell = 1x1 Unity unit.
/// </summary>
public class BuildGridManager : MonoBehaviour
{
    public static BuildGridManager Instance { get; private set; }

    [Header("Grid Settings")]
    [SerializeField] private int gridWidth = 50;
    [SerializeField] private int gridHeight = 50;
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private Vector3 gridOrigin = new Vector3(-25f, 0f, -25f);

    [Header("Debug")]
    [SerializeField] private bool showGridGizmos = true;

    // Grid occupancy map: true = occupied
    private bool[,] occupancyMap;

    // Follow target (continuous tracking)
    private Transform followTarget;
    private bool isFollowing = false;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        occupancyMap = new bool[gridWidth, gridHeight];
    }

    void LateUpdate()
    {
        if (!isFollowing || followTarget == null) return;

        // Recalculate grid origin to stay centered on target
        float halfW = (gridWidth * cellSize) * 0.5f;
        float halfH = (gridHeight * cellSize) * 0.5f;

        gridOrigin = new Vector3(
            Mathf.Round(followTarget.position.x) - halfW,
            0f,
            Mathf.Round(followTarget.position.z) - halfH
        );
    }

    /// <summary>
    /// Start following a target transform (e.g., the player).
    /// Grid will continuously re-center on this target every frame.
    /// </summary>
    public void SetFollowTarget(Transform target)
    {
        followTarget = target;
        isFollowing = true;

        // Initial center
        if (target != null)
            CenterOn(target.position);

        Debug.Log($"[BuildGrid] Now following: {(target != null ? target.name : "NULL")}");
    }

    /// <summary>
    /// Stop following the target.
    /// </summary>
    public void StopFollowing()
    {
        isFollowing = false;
        followTarget = null;
    }

    // ── World <-> Grid Conversion ──

    /// <summary>
    /// Convert world position to grid cell coordinates.
    /// </summary>
    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        int x = Mathf.FloorToInt((worldPos.x - gridOrigin.x) / cellSize);
        int z = Mathf.FloorToInt((worldPos.z - gridOrigin.z) / cellSize);
        return new Vector2Int(x, z);
    }

    /// <summary>
    /// Convert grid cell to world position (center of cell, on ground plane).
    /// </summary>
    public Vector3 GridToWorld(Vector2Int cell)
    {
        float x = gridOrigin.x + cell.x * cellSize + cellSize * 0.5f;
        float z = gridOrigin.z + cell.y * cellSize + cellSize * 0.5f;
        return new Vector3(x, 0f, z);
    }

    /// <summary>
    /// Convert grid cell to world position for a multi-cell item (center of footprint).
    /// </summary>
    public Vector3 GridToWorldCentered(Vector2Int cell, Vector2Int size)
    {
        float x = gridOrigin.x + cell.x * cellSize + size.x * cellSize * 0.5f;
        float z = gridOrigin.z + cell.y * cellSize + size.y * cellSize * 0.5f;
        return new Vector3(x, 0f, z);
    }

    /// <summary>
    /// Snap a world position to the nearest grid cell center.
    /// </summary>
    public Vector3 SnapToGrid(Vector3 worldPos)
    {
        Vector2Int cell = WorldToGrid(worldPos);
        return GridToWorld(cell);
    }

    // ── Validation ──

    /// <summary>
    /// Check if a multi-cell placement is valid (all cells in bounds and unoccupied).
    /// </summary>
    public bool CanPlace(Vector2Int gridCell, Vector2Int size)
    {
        for (int x = gridCell.x; x < gridCell.x + size.x; x++)
        {
            for (int z = gridCell.y; z < gridCell.y + size.y; z++)
            {
                if (!IsInBounds(x, z) || occupancyMap[x, z])
                    return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Mark cells as occupied after placement.
    /// </summary>
    public void OccupyCells(Vector2Int gridCell, Vector2Int size)
    {
        for (int x = gridCell.x; x < gridCell.x + size.x; x++)
        {
            for (int z = gridCell.y; z < gridCell.y + size.y; z++)
            {
                if (IsInBounds(x, z))
                    occupancyMap[x, z] = true;
            }
        }
        Debug.Log($"[BuildGrid] Occupied cells at ({gridCell.x},{gridCell.y}) size ({size.x},{size.y})");
    }

    /// <summary>
    /// Free cells after removing a building.
    /// </summary>
    public void FreeCells(Vector2Int gridCell, Vector2Int size)
    {
        for (int x = gridCell.x; x < gridCell.x + size.x; x++)
        {
            for (int z = gridCell.y; z < gridCell.y + size.y; z++)
            {
                if (IsInBounds(x, z))
                    occupancyMap[x, z] = false;
            }
        }
        Debug.Log($"[BuildGrid] Freed cells at ({gridCell.x},{gridCell.y}) size ({size.x},{size.y})");
    }

    private bool IsInBounds(int x, int z)
    {
        return x >= 0 && x < gridWidth && z >= 0 && z < gridHeight;
    }

    // ── Reposition Grid ──

    /// <summary>
    /// Center the grid around a world position (usually the player).
    /// Called when entering Build Mode so the grid is always visible.
    /// </summary>
    public void CenterOn(Vector3 worldPos)
    {
        // Snap the origin so cells align to whole numbers
        float halfW = (gridWidth * cellSize) * 0.5f;
        float halfH = (gridHeight * cellSize) * 0.5f;

        gridOrigin = new Vector3(
            Mathf.Round(worldPos.x) - halfW,
            0f,
            Mathf.Round(worldPos.z) - halfH
        );

        // NOTE: Do NOT reset occupancyMap here — it would wipe all placed building data.
        // The occupancy map is initialized once in Awake() and persists across re-centers.

        Debug.Log($"[BuildGrid] Grid centered on ({worldPos.x:F1}, {worldPos.z:F1}), origin = {gridOrigin}");
    }

    // ── Public Properties ──

    public float CellSize => cellSize;
    public Vector3 GridOrigin => gridOrigin;
    public int Width => gridWidth;
    public int Height => gridHeight;

    // ── Gizmos (Editor visualization) ──

    void OnDrawGizmos()
    {
        if (!showGridGizmos) return;

        Gizmos.color = new Color(1f, 1f, 1f, 0.15f);

        // Draw grid lines
        for (int x = 0; x <= gridWidth; x++)
        {
            Vector3 start = gridOrigin + new Vector3(x * cellSize, 0.01f, 0);
            Vector3 end = gridOrigin + new Vector3(x * cellSize, 0.01f, gridHeight * cellSize);
            Gizmos.DrawLine(start, end);
        }
        for (int z = 0; z <= gridHeight; z++)
        {
            Vector3 start = gridOrigin + new Vector3(0, 0.01f, z * cellSize);
            Vector3 end = gridOrigin + new Vector3(gridWidth * cellSize, 0.01f, z * cellSize);
            Gizmos.DrawLine(start, end);
        }

        // Draw occupied cells
        if (occupancyMap == null) return;
        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.3f);
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                if (occupancyMap[x, z])
                {
                    Vector3 center = GridToWorld(new Vector2Int(x, z));
                    center.y = 0.05f;
                    Gizmos.DrawCube(center, new Vector3(cellSize * 0.9f, 0.02f, cellSize * 0.9f));
                }
            }
        }
    }
}
