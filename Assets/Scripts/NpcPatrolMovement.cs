using System.Collections;
using UnityEngine;

public class NpcPatrolMovement : MonoBehaviour, IResettable
{
    public enum NpcAiState
    {
        IdleBeforeAlarm, // 鬧鐘響前：停在原位，但如果玩家進九宮格則轉為 ChasePlayer
        GoToAlarm,       // 鬧鐘響了：前往關鬧鐘，關完轉為 ChasePlayer
        ChasePlayer,     // 追逐玩家：直接朝玩家移動
        Patrol           // 巡邏（無鬧鐘關卡預設此狀態，或作為備用）
    }

    [Header("Movement Settings")]
    [SerializeField] private float tileSize = 1f;
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private LayerMask wallLayer;
    [Tooltip("每步之間的停頓時間（可在 Editor 中設定，例如 0.2 秒）")]
    [SerializeField] private float pauseBetweenSteps = 0.2f;

    [Header("Patrol Settings")]
    [SerializeField] private Transform[] patrolWaypoints;
    
    [Header("AI Settings")]
    [SerializeField] private AlarmClock alarmObject;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private NpcAiState defaultState = NpcAiState.IdleBeforeAlarm;

    private int _waypointIndex = 0;
    private Vector3 _targetPosition;
    private bool _isMoving;

    private NpcAiState _currentState;
    private Vector3 _startPosition;
    private bool _skipChaseThisTurn = false; // 標記是否需要在本回合跳過追逐移動
    private PlayerGridMovement _playerMovement;

    private Vector3 _areaSnapshotPosition;
    private Vector3 _areaSnapshotTargetPosition;
    private NpcAiState _areaSnapshotState;
    private int _areaSnapshotWaypointIndex;

    public NpcAiState CurrentState => _currentState;

    private void Awake()
    {
        _startPosition  = transform.position;
        _targetPosition = transform.position;
    }

    private void Start()
    {
        CachePlayerMovement();
        ResetAiState();
        SaveAreaSnapshot();

        if (_currentState == NpcAiState.Patrol && (patrolWaypoints == null || patrolWaypoints.Length == 0))
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
            StartCoroutine(RegisterToTurnManager());
        }

        // 註冊鬧鐘響起事件
        if (alarmObject != null)
        {
            alarmObject.OnAlarmRung += HandleAlarmRung;
        }
    }

    private void OnDisable()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnNpcTakeTurn -= StartNpcTurn;
        }
        if (alarmObject != null)
        {
            alarmObject.OnAlarmRung -= HandleAlarmRung;
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

        // 隨時檢查玩家是否與 NPC 踏入同一格
        CheckPlayerCaughtInstant();
    }

    private void CheckPlayerCaughtInstant()
    {
        if (playerTransform != null)
        {
            if (WorldToGrid(transform.position) == WorldToGrid(playerTransform.position))
            {
                Debug.Log($"[NpcPatrolMovement] 偵測到玩家與 NPC 重疊！重置關卡。");
                LevelManager.ResetAll();
            }
        }
    }

    private void StartNpcTurn(int steps)
    {
        Debug.Log($"[NpcPatrolMovement] {gameObject.name} 開始行動回合，目前狀態為 {_currentState}，預計走 {steps} 步。");
        StartCoroutine(NpcTurnCoroutine(steps));
    }

    private IEnumerator NpcTurnCoroutine(int steps)
    {
        // 進入回合時，先檢測玩家是否踏入了九宮格
        UpdateAiState();

        // 如果本回合需要跳過追逐（玩家剛走進九宮格的當回合），NPC 留在原地不動
        if (_skipChaseThisTurn)
        {
            _skipChaseThisTurn = false; // 重設標記
            Debug.Log($"[NpcPatrolMovement] {gameObject.name} 本回合被玩家驚擾，停在原位，等玩家下一步再開始追。");
            
            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.IsNpcMoving = false; // 釋放行動鎖定
            }
            yield break; // 結束本回合行動
        }

        for (int i = 0; i < steps; i++)
        {
            // 每次移動前，檢查狀態轉換條件（例如玩家是否在 9 宮格內）
            UpdateAiState();

            // 如果處於待機且未受驚擾狀態，不進行任何物理移動
            if (_currentState == NpcAiState.IdleBeforeAlarm)
            {
                yield return null;
                continue;
            }

            // 檢查是否能關閉鬧鐘，若此步用來關鬧鐘則消耗該步，不進行物理移動
            if (TryTurnOffAlarm())
            {
                if (i < steps - 1)
                {
                    yield return new WaitForSeconds(pauseBetweenSteps);
                }
                continue;
            }

            // 取得目前的目標位置
            Vector3 targetPos = GetTargetPositionForState();

            Vector3 nextPos = DetermineNextStep(targetPos);

            if (nextPos == _targetPosition)
            {
                Debug.LogWarning($"[NpcPatrolMovement] {gameObject.name} 在第 {i+1} 步被卡住了，原地不動！");
                yield return new WaitForSeconds(0.1f);
                continue;
            }

            _targetPosition = nextPos;
            _isMoving = true;

            // 等待本次移動滑動完成
            while (_isMoving)
            {
                yield return null;
            }

            // 如果還有下一步要走，就稍微停頓一下
            if (i < steps - 1)
            {
                yield return new WaitForSeconds(pauseBetweenSteps);
            }
        }

        // NPC 行動完畢，釋放鎖定
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.IsNpcMoving = false;
        }
    }

    private void UpdateAiState()
    {
        if (playerTransform == null) return;

        // 1. 鬧鐘響前，若玩家進入九宮格 (X、Z 座標差皆 <= 1)，轉為 Chase 模式
        if (_currentState == NpcAiState.IdleBeforeAlarm)
        {
            Vector3 targetToCheck = _playerMovement != null ? _playerMovement.TargetPosition : playerTransform.position;
            Vector3Int playerGrid = WorldToGrid(targetToCheck);
            Vector3Int npcGrid = WorldToGrid(_targetPosition);

            if (Mathf.Abs(playerGrid.x - npcGrid.x) <= 1 && Mathf.Abs(playerGrid.z - npcGrid.z) <= 1)
            {
                _currentState = NpcAiState.ChasePlayer;
                _skipChaseThisTurn = true; // 鎖定這回合，等玩家下一步再追
                Debug.Log($"[NpcPatrolMovement] {gameObject.name} 偵測到玩家在九宮格內！切換到 ChasePlayer 狀態，下回合開始追擊。");
            }
        }
    }

    private bool TryTurnOffAlarm()
    {
        if (_currentState == NpcAiState.GoToAlarm && alarmObject != null)
        {
            Vector3Int alarmGrid = WorldToGrid(alarmObject.transform.position);
            Vector3Int npcGrid = WorldToGrid(_targetPosition);

            int dist = Mathf.Abs(alarmGrid.x - npcGrid.x) + Mathf.Abs(alarmGrid.z - npcGrid.z);
            if (dist <= 1)
            {
                alarmObject.TurnOff();
                _currentState = NpcAiState.ChasePlayer;
                Debug.Log($"[NpcPatrolMovement] {gameObject.name} 已抵達鬧鐘旁 (距離 {dist})，關閉鬧鐘消耗 1 步，並切換為 Chase 模式。");
                return true;
            }
        }
        return false;
    }

    private Vector3 GetTargetPositionForState()
    {
        switch (_currentState)
        {
            case NpcAiState.ChasePlayer:
                if (_playerMovement != null)
                {
                    return _playerMovement.TargetPosition;
                }
                if (playerTransform != null)
                {
                    return playerTransform.position;
                }
                return _targetPosition;

            case NpcAiState.GoToAlarm:
                if (alarmObject != null)
                {
                    return alarmObject.transform.position;
                }
                return _targetPosition;

            case NpcAiState.Patrol:
                if (patrolWaypoints != null && patrolWaypoints.Length > 0)
                {
                    Vector3 targetWaypoint = patrolWaypoints[_waypointIndex].position;
                    // 檢查是否抵達當前的巡邏點（以網格座標比對，防範偏差）
                    if (WorldToGrid(_targetPosition) == WorldToGrid(targetWaypoint))
                    {
                        int oldIndex = _waypointIndex;
                        _waypointIndex = (_waypointIndex + 1) % patrolWaypoints.Length;
                        targetWaypoint = patrolWaypoints[_waypointIndex].position;
                        Debug.Log($"[NpcPatrolMovement] {gameObject.name} 抵達巡邏點 {oldIndex}，切換到巡邏點 {_waypointIndex}。");
                    }
                    return targetWaypoint;
                }
                return _targetPosition;

            case NpcAiState.IdleBeforeAlarm:
            default:
                return _targetPosition;
        }
    }

    private void HandleAlarmRung()
    {
        // 聽到鬧鐘響時，不論是在 Idle 還是 Patrol 狀態，都前往關鬧鐘
        if (_currentState == NpcAiState.IdleBeforeAlarm || _currentState == NpcAiState.Patrol)
        {
            _currentState = NpcAiState.GoToAlarm;
            Debug.Log($"[NpcPatrolMovement] {gameObject.name} 聽到鬧鐘響！開始前往關鬧鐘！");
        }
    }

    private void CachePlayerMovement()
    {
        if (playerTransform == null)
        {
            _playerMovement = FindFirstObjectByType<PlayerGridMovement>();
            if (_playerMovement != null)
            {
                playerTransform = _playerMovement.transform;
            }
        }
        else if (_playerMovement == null)
        {
            _playerMovement = playerTransform.GetComponent<PlayerGridMovement>();
        }
    }

    private void CheckPlayerIn9GridAtStart()
    {
        if (_currentState == NpcAiState.IdleBeforeAlarm && playerTransform != null)
        {
            Vector3 targetToCheck = _playerMovement != null ? _playerMovement.TargetPosition : playerTransform.position;
            Vector3Int playerGrid = WorldToGrid(targetToCheck);
            Vector3Int npcGrid = WorldToGrid(_startPosition);

            if (Mathf.Abs(playerGrid.x - npcGrid.x) <= 1 && Mathf.Abs(playerGrid.z - npcGrid.z) <= 1)
            {
                _currentState = NpcAiState.ChasePlayer;
                _skipChaseThisTurn = false; // 初始就在九宮格內，因此玩家第一步移動時就要開始追，不跳過
                Debug.Log($"[NpcPatrolMovement] {gameObject.name} 偵測到玩家初始就在九宮格內！直接切換為 ChasePlayer 狀態，玩家下一步即開始追擊。");
            }
        }
    }

    private void ResetAiState()
    {
        _waypointIndex = 0;
        _isMoving = false;
        _skipChaseThisTurn = false;

        CachePlayerMovement();

        if (alarmObject != null)
        {
            _currentState = NpcAiState.IdleBeforeAlarm;
            if (alarmObject.IsRinging)
            {
                _currentState = NpcAiState.GoToAlarm;
            }
        }
        else
        {
            _currentState = defaultState;
        }

        CheckPlayerIn9GridAtStart();

        Debug.Log($"[NpcPatrolMovement] {gameObject.name} AI 狀態初始化/重置為: {_currentState}");
    }

    public void SaveAreaSnapshot()
    {
        _areaSnapshotPosition       = transform.position;
        _areaSnapshotTargetPosition = _targetPosition;
        _areaSnapshotState          = _currentState;
        _areaSnapshotWaypointIndex  = _waypointIndex;
    }

    public void OnReset()
    {
        _isMoving           = false;
        _skipChaseThisTurn  = false;
        _waypointIndex      = _areaSnapshotWaypointIndex;
        _currentState       = _areaSnapshotState;
        transform.position  = _areaSnapshotPosition;
        _targetPosition     = _areaSnapshotTargetPosition;
        CachePlayerMovement();

        if (alarmObject != null && alarmObject.IsRinging &&
            (_currentState == NpcAiState.IdleBeforeAlarm || _currentState == NpcAiState.Patrol))
        {
            _currentState = NpcAiState.GoToAlarm;
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
        int maxIterations = 1500;
        int iterations = 0;

        while (openSet.Count > 0 && iterations < maxIterations)
        {
            iterations++;
            
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

        Debug.LogWarning($"[NpcPatrolMovement] A* 找不到前往 {targetWaypoint} 的路徑，退回四方向貪婪搜尋。");
        return DetermineNextStepGreedy(targetWaypoint);
    }

    private float GetHeuristic(Vector3Int a, Vector3Int b)
    {
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
        Vector3 checkPos = pos;
        checkPos.y += 0.5f; // 提升檢測點的 Y 軸高度，防止因 NPC Y 軸較低而與地板（Road）產生碰撞判定

        Vector3 halfExtents = new Vector3(tileSize * 0.45f, 0.45f, tileSize * 0.45f);
        Collider[] hits = Physics.OverlapBox(checkPos, halfExtents, Quaternion.identity, wallLayer);
        
        foreach (Collider hit in hits)
        {
            // 忽略 NPC 自己（或子物件）與 Player（或子物件）的碰撞體，避免誤判
            if (hit.gameObject == gameObject || hit.transform.root == transform.root) continue;
            if (playerTransform != null && (hit.gameObject == playerTransform.gameObject || hit.transform.root == playerTransform.root)) continue;

            Debug.Log($"[NpcPatrolMovement] 碰撞檢測阻擋物: {hit.name} (Layer: {LayerMask.LayerToName(hit.gameObject.layer)})");
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
