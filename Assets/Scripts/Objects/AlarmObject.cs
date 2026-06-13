using System;
using UnityEngine;

public class AlarmObject : KeepableBase, IInteractable
{
    public event Action OnAlarmRung;
    public event Action OnAlarmTurnedOff;

    [Header("Alarm Settings")]
    [SerializeField] private bool ringOnStart = false;
    
    [Tooltip("玩家移動幾步後，鬧鐘會自動響起（設為 0 代表需要外部觸發）")]
    [SerializeField] private int ringAfterPlayerSteps = 0;

    private bool _isRinging = false;
    private int _stepCounter = 0;

    // 快照狀態（Keep 機制使用）
    private bool _snapshotIsRinging;
    private int _snapshotStepCounter;

    public bool IsRinging => _isRinging;

    private void Start()
    {
        if (ringOnStart)
        {
            Ring();
        }
    }

    private void OnEnable()
    {
        PlayerGridMovement.OnActionTaken += HandlePlayerAction;
    }

    private void OnDisable()
    {
        PlayerGridMovement.OnActionTaken -= HandlePlayerAction;
    }

    private void HandlePlayerAction()
    {
        if (_isRinging) return;

        if (ringAfterPlayerSteps > 0)
        {
            _stepCounter++;
            if (_stepCounter >= ringAfterPlayerSteps)
            {
                Ring();
            }
        }
    }

    public void Interact()
    {
        // 玩家互動：切換鬧鐘狀態（響 -> 關，不響 -> 開）
        if (_isRinging)
        {
            TurnOff();
        }
        else
        {
            Ring();
        }
    }

    public void Ring()
    {
        if (_isRinging) return;
        _isRinging = true;
        OnAlarmRung?.Invoke();
        Debug.Log($"[AlarmObject] {gameObject.name} 鬧鐘響起！");
    }

    public void TurnOff()
    {
        if (!_isRinging) return;
        _isRinging = false;
        OnAlarmTurnedOff?.Invoke();
        Debug.Log($"[AlarmObject] {gameObject.name} 鬧鐘被關閉。");
    }

    // --- KeepableBase 實作 ---

    protected override void SaveSnapshot()
    {
        _snapshotIsRinging = _isRinging;
        _snapshotStepCounter = _stepCounter;
        Debug.Log($"[AlarmObject] {gameObject.name} 保存快照：IsRinging={_snapshotIsRinging}, StepCounter={_snapshotStepCounter}");
    }

    protected override void RestoreSnapshot()
    {
        _stepCounter = _snapshotStepCounter;
        
        // 恢復鬧鐘狀態
        if (_snapshotIsRinging)
        {
            Ring();
        }
        else
        {
            TurnOff();
        }
        Debug.Log($"[AlarmObject] {gameObject.name} 恢復快照：IsRinging={_isRinging}, StepCounter={_stepCounter}");
    }

    protected override void OnResetInternal()
    {
        _isRinging = false;
        _stepCounter = 0;
        if (ringOnStart)
        {
            Ring();
        }
        else
        {
            // 確保觸發關閉事件，通知 NPC
            OnAlarmTurnedOff?.Invoke();
        }
        Debug.Log($"[AlarmObject] {gameObject.name} 重置為初始狀態。");
    }
}
