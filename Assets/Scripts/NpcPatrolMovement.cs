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
        StartCoroutine(NpcTurnCoroutine(steps));
    }

    private IEnumerator NpcTurnCoroutine(int steps)
    {
        for (int i = 0; i < steps; i++)
        {
            if (patrolWaypoints == null || patrolWaypoints.Length == 0)
            {
                yield return null;
                break;
            }

            Vector3 targetWaypoint = patrolWaypoints[_waypointIndex].position;

            // 檢查是否抵達當前的巡邏點
            if (Vector3.Distance(_targetPosition, targetWaypoint) < 0.1f)
            {
                _waypointIndex = (_waypointIndex + 1) % patrolWaypoints.Length;
                targetWaypoint = patrolWaypoints[_waypointIndex].position;
            }

            Vector3 nextPos = DetermineNextStep(targetWaypoint);

            if (nextPos == _targetPosition)
            {
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
        TurnManager.Instance.IsNpcMoving = false;
    }

    private Vector3 DetermineNextStep(Vector3 targetWaypoint)
    {
        Vector3 diff = targetWaypoint - _targetPosition;
        Vector3 primaryDir = Vector3.zero;
        Vector3 fallbackDir = Vector3.zero;

        // 優先走差值較大的軸向
        if (Mathf.Abs(diff.x) > Mathf.Abs(diff.z))
        {
            if (Mathf.Abs(diff.x) > 0.01f) primaryDir = new Vector3(Mathf.Sign(diff.x), 0, 0);
            if (Mathf.Abs(diff.z) > 0.01f) fallbackDir = new Vector3(0, 0, Mathf.Sign(diff.z));
        }
        else
        {
            if (Mathf.Abs(diff.z) > 0.01f) primaryDir = new Vector3(0, 0, Mathf.Sign(diff.z));
            if (Mathf.Abs(diff.x) > 0.01f) fallbackDir = new Vector3(Mathf.Sign(diff.x), 0, 0);
        }

        // 嘗試主要前進方向
        if (primaryDir != Vector3.zero)
        {
            Vector3 next = _targetPosition + primaryDir * tileSize;
            if (CanMoveTo(next)) return next;
        }

        // 嘗試次要前進方向 (簡易避障)
        if (fallbackDir != Vector3.zero)
        {
            Vector3 next = _targetPosition + fallbackDir * tileSize;
            if (CanMoveTo(next)) return next;
        }

        // 無路可走則原地踏步
        return _targetPosition;
    }

    private bool CanMoveTo(Vector3 pos)
    {
        Vector3 halfExtents = Vector3.one * (tileSize * 0.45f);
        return Physics.OverlapBox(pos, halfExtents, Quaternion.identity, wallLayer).Length == 0;
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
