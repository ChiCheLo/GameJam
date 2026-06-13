using System.Collections;
using UnityEngine;

public class NpcPatrolMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float tileSize = 1f;
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private LayerMask wallLayer;

    [Header("Patrol Settings")]
    [SerializeField] private Transform[] patrolWaypoints;
    
    private int _waypointIndex = 0;
    private Vector3 _targetPosition;
    private bool _isMoving;

    private void Start()
    {
        SnapToGrid();
        if (patrolWaypoints == null || patrolWaypoints.Length == 0)
        {
            Debug.LogError($"[NpcPatrolMovement] {gameObject.name} 沒有設定任何巡邏點 (Patrol Waypoints)！");
        }
    }

    private void OnEnable()
    {
        // 註冊 TurnManager 的行動事件
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnNpcTakeTurn += StartNpcTurn;
        }
        else
        {
            // 延遲註冊以防 Start/Awake 順序問題
            StartCoroutine(RegisterToTurnManager());
        }
    }

    private void OnDisable()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnNpcTakeTurn -= StartNpcTurn;
        }
    }

    private IEnumerator RegisterToTurnManager()
    {
        yield return new WaitUntil(() => TurnManager.Instance != null);
        TurnManager.Instance.OnNpcTakeTurn += StartNpcTurn;
        Debug.Log($"[NpcPatrolMovement] {gameObject.name} 成功向 TurnManager 註冊！");
    }

    private void Update()
    {
        if (_isMoving)
        {
            StepTowardTarget();
        }
    }

    private void StartNpcTurn(int steps)
    {
        Debug.Log($"[NpcPatrolMovement] {gameObject.name} 開始行動回合，預計走 {steps} 步。");
        StartCoroutine(NpcTurnCoroutine(steps));
    }

    private IEnumerator NpcTurnCoroutine(int steps)
    {
        for (int i = 0; i < steps; i++)
        {
            if (patrolWaypoints == null || patrolWaypoints.Length == 0)
            {
                Debug.LogWarning($"[NpcPatrolMovement] {gameObject.name} 因無巡邏點跳過此步。");
                yield return null;
                break;
            }

            Vector3 targetWaypoint = patrolWaypoints[_waypointIndex].position;

            // 檢查是否抵達當前的巡邏點（以網格座標比對，防範偏差）
            if (WorldToGrid(_targetPosition) == WorldToGrid(targetWaypoint))
            {
                int oldIndex = _waypointIndex;
                _waypointIndex = (_waypointIndex + 1) % patrolWaypoints.Length;
                targetWaypoint = patrolWaypoints[_waypointIndex].position;
                Debug.Log($"[NpcPatrolMovement] {gameObject.name} 抵達巡邏點 {oldIndex}，切換到巡邏點 {_waypointIndex}。");
            }

            Vector3 nextPos = DetermineNextStep(targetWaypoint);

            if (nextPos == _targetPosition)
            {
                Debug.LogWarning($"[NpcPatrolMovement] {gameObject.name} 在第 {i+1} 步被卡住了，原地不動！");
                // 卡住時等待一小段時間避免瞬間跑完 Loop
                yield return new WaitForSeconds(0.1f);
                continue;
            }

            _targetPosition = nextPos;
            _isMoving = true;

            // 等待本次移動完成（由 StepTowardTarget 負責完成）
            while (_isMoving)
            {
                yield return null;
            }
        }

        // NPC 行動完畢，釋放鎖定
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.IsNpcMoving = false;
        }
    }

    private Vector3 DetermineNextStep(Vector3 targetWaypoint)
    {
        return FindNextStepAStar(targetWaypoint);
    }

    private Vector3 FindNextStepAStar(Vector3 targetWaypoint)
    {
        Vector3Int startCell = WorldToGrid(_targetPosition);
        Vector3Int targetCell = WorldToGrid(targetWaypoint);

        if (startCell == targetCell) return _targetPosition;

        System.Collections.Generic.List<Vector3Int> openSet = new System.Collections.Generic.List<Vector3Int> { startCell };
        System.Collections.Generic.HashSet<Vector3Int> closedSet = new System.Collections.Generic.HashSet<Vector3Int>();

        System.Collections.Generic.Dictionary<Vector3Int, Vector3Int> parentMap = new System.Collections.Generic.Dictionary<Vector3Int, Vector3Int>();
        System.Collections.Generic.Dictionary<Vector3Int, float> gScore = new System.Collections.Generic.Dictionary<Vector3Int, float> { { startCell, 0f } };
        System.Collections.Generic.Dictionary<Vector3Int, float> fScore = new System.Collections.Generic.Dictionary<Vector3Int, float> { { startCell, GetHeuristic(startCell, targetCell) } };

        Vector3Int[] directions = new Vector3Int[]
        {
            new Vector3Int(0, 0, 1),   // 前 (Z+)
            new Vector3Int(0, 0, -1),  // 後 (Z-)
            new Vector3Int(-1, 0, 0),  // 左 (X-)
            new Vector3Int(1, 0, 0)    // 右 (X+)
        };

        bool found = false;
        int maxIterations = 1500; // A* 搜尋非常精準，1500 次足夠覆蓋超大地圖
        int iterations = 0;

        while (openSet.Count > 0 && iterations < maxIterations)
        {
            iterations++;
            
            // 尋找 fScore 最小的節點
            Vector3Int current = openSet[0];
            float lowestF = fScore.ContainsKey(current) ? fScore[current] : float.MaxValue;
            for (int i = 1; i < openSet.Count; i++)
            {
                float f = fScore.ContainsKey(openSet[i]) ? fScore[openSet[i]] : float.MaxValue;
                if (f < lowestF)
                {
                    lowestF = f;
                    current = openSet[i];
                }
            }

            if (current == targetCell)
            {
                found = true;
                break;
            }

            openSet.Remove(current);
            closedSet.Add(current);

            foreach (Vector3Int dir in directions)
            {
                Vector3Int neighbor = current + dir;
                if (closedSet.Contains(neighbor)) continue;

                Vector3 checkPos = GridToWorld(neighbor);
                if (!CanMoveTo(checkPos)) continue;

                float tentativeG = gScore[current] + 1f;

                if (!openSet.Contains(neighbor))
                {
                    openSet.Add(neighbor);
                }
                else if (tentativeG >= (gScore.ContainsKey(neighbor) ? gScore[neighbor] : float.MaxValue))
                {
                    continue;
                }

                parentMap[neighbor] = current;
                gScore[neighbor] = tentativeG;
                fScore[neighbor] = tentativeG + GetHeuristic(neighbor, targetCell);
            }
        }

        if (found)
        {
            Vector3Int curr = targetCell;
            while (parentMap.ContainsKey(curr) && parentMap[curr] != startCell)
            {
                curr = parentMap[curr];
            }
            return GridToWorld(curr);
        }

        // 如果找不到完整路徑（例如被完全堵死），則退回到貪婪的 4 方向搜尋
        Debug.LogWarning($"[NpcPatrolMovement] A* 找不到前往 {targetWaypoint} 的路徑，退回四方向貪婪搜尋。");
        return DetermineNextStepGreedy(targetWaypoint);
    }

    private float GetHeuristic(Vector3Int a, Vector3Int b)
    {
        // 使用曼哈頓距離 (Manhattan Distance) 作為 A* 啟發值
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.z - b.z);
    }

    private Vector3 DetermineNextStepGreedy(Vector3 targetWaypoint)
    {
        Vector3[] directions = new Vector3[]
        {
            Vector3.forward,
            Vector3.back,
            Vector3.left,
            Vector3.right
        };

        Vector3 bestStep = _targetPosition;
        float minDistance = float.MaxValue;

        foreach (Vector3 dir in directions)
        {
            Vector3 candidatePos = _targetPosition + dir * tileSize;

            if (CanMoveTo(candidatePos))
            {
                float dist = Vector3.Distance(candidatePos, targetWaypoint);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    bestStep = candidatePos;
                }
            }
        }

        return bestStep;
    }

    private Vector3Int WorldToGrid(Vector3 worldPos)
    {
        int x = Mathf.RoundToInt((worldPos.x + 0.5f * tileSize) / tileSize);
        int z = Mathf.RoundToInt((worldPos.z + 0.5f * tileSize) / tileSize);
        return new Vector3Int(x, 0, z);
    }

    private Vector3 GridToWorld(Vector3Int gridPos)
    {
        float x = gridPos.x * tileSize - 0.5f * tileSize;
        float z = gridPos.z * tileSize - 0.5f * tileSize;
        return new Vector3(x, _targetPosition.y, z);
    }

    private bool CanMoveTo(Vector3 pos)
    {
        Vector3 halfExtents = Vector3.one * (tileSize * 0.45f);
        Collider[] hits = Physics.OverlapBox(pos, halfExtents, Quaternion.identity, wallLayer);
        if (hits.Length > 0)
        {
            Debug.Log($"[NpcPatrolMovement] 碰撞檢測阻擋物: {hits[0].name} (Layer: {LayerMask.LayerToName(hits[0].gameObject.layer)})");
            return false;
        }
        return true;
    }

    private void StepTowardTarget()
    {
        transform.position = Vector3.MoveTowards(transform.position, _targetPosition, moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, _targetPosition) < 0.001f)
        {
            transform.position = _targetPosition;
            _isMoving = false;
        }
    }

    private void SnapToGrid()
    {
        Vector3 p = transform.position;
        p.x = Mathf.Round(p.x / tileSize) * tileSize;
        p.z = Mathf.Round(p.z / tileSize) * tileSize;
        p.x -= 0.5f;
        p.z -= 0.5f;
        transform.position = p;
        _targetPosition = transform.position;
    }
}
