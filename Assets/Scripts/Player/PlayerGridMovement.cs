using System;
using UnityEngine;

public class PlayerGridMovement : MonoBehaviour, IResettable
{
    [SerializeField] private float tileSize = 1f;
    [SerializeField] private float moveSpeed = 12f;
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private Vector3 spawnPosition;
    [SerializeField] private InteractionTrigger interactionTrigger;

    public static event Action OnActionTaken;

    private Vector3 _targetPosition;
    private Quaternion _targetRotation;
    private Quaternion _spawnRotation;
    private bool _isMoving;

    void Start()
    {
        transform.position = spawnPosition;
        _targetPosition = spawnPosition;
        _spawnRotation = transform.rotation;
        _targetRotation = transform.rotation;
    }

    void Update()
    {
        transform.rotation = _targetRotation;

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

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (LevelManager.Instance.canAction && interactionTrigger != null && interactionTrigger.TriggerInteract())
                OnActionTaken?.Invoke();
            return;
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            if (LevelManager.Instance.canAction && interactionTrigger != null && interactionTrigger.TriggerKeep())
                OnActionTaken?.Invoke();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            _targetRotation *= Quaternion.Euler(0, -90f, 0);
            return;
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            _targetRotation *= Quaternion.Euler(0, 90f, 0);
            return;
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

        _targetRotation = Quaternion.LookRotation(dir);
        _targetPosition = next;
        _isMoving = true;
        OnActionTaken?.Invoke();
    }

    bool CanMoveTo(Vector3 pos)
    {
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

    public void SetSpawn(Vector3 position, Vector3 euler)
    {
        spawnPosition  = position;
        _spawnRotation = Quaternion.Euler(euler);
    }

    public void OnReset()
    {
        transform.position = spawnPosition;
        _targetPosition    = spawnPosition;
        _targetRotation    = _spawnRotation;
        transform.rotation = _spawnRotation;
        _isMoving          = false;
    }
}
