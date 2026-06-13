using System;
using UnityEngine;

public class PlayerGridMovement : MonoBehaviour, IResettable
{
    [SerializeField] private float tileSize = 0.5f;
    [SerializeField] private float moveSpeed = 12f;
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private Vector3 spawnPosition;

    public static event Action OnActionTaken;

    private Vector3 _targetPosition;
    private bool _isMoving;

    void Start()
    {
        transform.position = spawnPosition;
        _targetPosition = spawnPosition;
    }

    void Update()
    {
        if (_isMoving)
        {
            StepTowardTarget();
            return;
        }

        // 如果 NPC 還在走，不允許玩家行動
        if (TurnManager.Instance != null && TurnManager.Instance.IsNpcMoving)
        {
            return;
        }

        HandleInput();
    }

    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            LevelManager.ResetAll();
        }
        
        Vector3 dir = Vector3.zero;

        if (Input.GetKeyDown(KeyCode.W)) dir = Vector3.forward;
        else if (Input.GetKeyDown(KeyCode.S)) dir = Vector3.back;
        else if (Input.GetKeyDown(KeyCode.A)) dir = Vector3.left;
        else if (Input.GetKeyDown(KeyCode.D)) dir = Vector3.right;

        if (dir == Vector3.zero) return;

        TryMove(dir);
    }

    void TryMove(Vector3 dir)
    {
        if (!LevelManager.Instance.canAction) return;

        Vector3 next = _targetPosition + dir * tileSize;

        if (!CanMoveTo(next)) return;

        _targetPosition = next;
        _isMoving = true;
        OnActionTaken?.Invoke();
    }

    bool CanMoveTo(Vector3 pos)
    {
        // 用稍小的 half-extent 避免邊界誤判
        Vector3 halfExtents = Vector3.one * (tileSize * 0.45f);
        return Physics.OverlapBox(pos, halfExtents, Quaternion.identity, wallLayer).Length == 0;
    }

    void StepTowardTarget()
    {
        transform.position = Vector3.MoveTowards(transform.position, _targetPosition, moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, _targetPosition) < 0.001f)
        {
            transform.position = _targetPosition;
            _isMoving = false;
        }
    }

    public void OnReset()
    {
        transform.position = spawnPosition;
        _targetPosition = spawnPosition;
        _isMoving = false;
    }
}