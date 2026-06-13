using System;
using UnityEngine;

public class PlayerGridMovement : MonoBehaviour
{
    [SerializeField] private float tileSize = 1f;
    [SerializeField] private float moveSpeed = 12f;
    [SerializeField] private LayerMask wallLayer;

    public static event Action OnActionTaken;

    private Vector3 _targetPosition;
    private bool _isMoving;

    void Start()
    {
       SnapToGrid();
    }

    void Update()
    {
        if (_isMoving)
        {
            StepTowardTarget();
            return;
        }

        HandleInput();
    }

    void HandleInput()
    {
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

    void SnapToGrid()
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
