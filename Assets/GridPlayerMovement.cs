using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GridPlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private Grid grid;

    [Header("Collision Settings (Choose One or Both)")]
    [Tooltip("用於判斷不可走格子的 Tilemap (例如 WallTilemap)")]
    [SerializeField] private Tilemap obstacleTilemap;
    
    [Tooltip("使用 3D 物理球體偵測障礙物圖層 (適合有 3D Collider 的障礙物)")]
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float collisionRadius = 0.15f;

    private bool isMoving;
    private Vector3 gridOffset; // 紀錄 Player 與網格中心點的初始偏差量（如高度差）

    private void Start()
    {
        if (grid == null)
        {
            grid = FindFirstObjectByType<Grid>();
        }

        // 檢查並提示 Rigidbody 影響
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null && !rb.isKinematic)
        {
            Debug.LogWarning("Player 擁有 Rigidbody。一格一格移動建議勾選 'Is Kinematic'，或關閉 'Use Gravity'，避免物理引擎導致位置位移！");
        }

        // 計算 Player 相對於網格中心點的初始偏差（保留高度 Y 或深度 Z）
        CalculateOffset();

        // 啟動時自動對齊最近的網格中心（加上偏差）
        SnapToGrid();
    }

    private void Update()
    {
        // 如果正在移動中，不接受新的輸入
        if (isMoving) return;

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        // 限制只能十字移動（優先處理水平輸入）
        if (horizontal != 0)
        {
            vertical = 0;
        }

        if (horizontal != 0 || vertical != 0)
        {
            // 因為 Tilemap 本質上是 2D 的，網格座標移動一定是 X 與 Y 軸！
            // 不管你的 Grid 在 3D 空間中如何旋轉，網格內移動都是增加 cell 的 X 或 Y。
            Vector3Int direction = new Vector3Int((int)horizontal, (int)vertical, 0);
            TryMove(direction);
        }
    }

    private void TryMove(Vector3Int direction)
    {
        // 1. 計算目前的網格座標
        Vector3Int currentCell = grid.WorldToCell(transform.position - gridOffset);
        
        // 2. 計算目標的網格座標
        Vector3Int targetCell = currentCell + direction;
        
        // 3. 取得目標格子中心的 World Position，並加上初始偏差（如高度）
        Vector3 targetWorldPos = grid.GetCellCenterWorld(targetCell) + gridOffset;

        // 4. 檢查是否有障礙物
        if (CanMove(targetCell, targetWorldPos))
        {
            StartCoroutine(MoveRoutine(targetWorldPos));
        }
    }

    private IEnumerator MoveRoutine(Vector3 destination)
    {
        isMoving = true;

        while (Vector3.Distance(transform.position, destination) > 0.001f)
        {
            transform.position = Vector3.MoveTowards(transform.position, destination, moveSpeed * Time.deltaTime);
            yield return null;
        }

        transform.position = destination;
        isMoving = false;
    }

    private bool CanMove(Vector3Int targetCell, Vector3 targetWorldPos)
    {
        // 方式 A: 檢查障礙物 Tilemap 是否在該格子有 Tile
        if (obstacleTilemap != null && obstacleTilemap.HasTile(targetCell))
        {
            Debug.Log($"[移動受阻] 目標格 {targetCell} 在 Tilemap 中有障礙物 Tile。");
            return false;
        }

        // 方式 B: 使用 3D 物理球體檢測
        if (obstacleLayer != 0)
        {
            Collider[] hits = Physics.OverlapSphere(targetWorldPos, collisionRadius, obstacleLayer);
            if (hits.Length > 0)
            {
                Debug.Log($"[移動受阻] 目標位置 {targetWorldPos} 偵測到物理障礙物：{hits[0].name} (Layer: {LayerMask.LayerToName(hits[0].gameObject.layer)})");
                return false;
            }
        }

        return true;
    }

    private void CalculateOffset()
    {
        if (grid != null)
        {
            Vector3Int currentCell = grid.WorldToCell(transform.position);
            Vector3 cellCenter = grid.GetCellCenterWorld(currentCell);
            gridOffset = transform.position - cellCenter;
        }
    }

    private void SnapToGrid()
    {
        if (grid != null)
        {
            Vector3Int currentCell = grid.WorldToCell(transform.position - gridOffset);
            transform.position = grid.GetCellCenterWorld(currentCell) + gridOffset;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (grid != null)
        {
            Gizmos.color = Color.red;
            Vector3Int currentCell = grid.WorldToCell(transform.position - gridOffset);
            Vector3 center = grid.GetCellCenterWorld(currentCell) + gridOffset;
            Gizmos.DrawWireSphere(center, collisionRadius);
        }
    }
}
