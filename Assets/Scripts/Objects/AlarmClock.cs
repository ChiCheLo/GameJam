using System;
using UnityEngine;

public class AlarmClock : KeepableBase, IInteractable
{
    public event Action OnAlarmRung;
    public event Action OnAlarmTurnedOff;

    [SerializeField] private FloatingText floatingText;

    [Header("Alarm Settings")]
    [SerializeField] private bool ringOnStart = false;
    [Tooltip("玩家移動幾步後，鬧鐘會自動響起（設為 0 代表需要外部觸發）")]
    [SerializeField] private int ringAfterPlayerSteps = 0;

    private bool _isRinging;
    private int _stepCounter;

    private bool _snapshotIsRinging;
    private int _snapshotStepCounter;

    public bool IsRinging => _isRinging;
    public string InteractLabel => _isRinging ? "關閉" : "啟動";

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
        if (_isRinging)
            TurnOff();
        else
            Ring();
    }

    public void Ring()
    {
        if (_isRinging) return;
        _isRinging = true;
        ApplyState();
        OnAlarmRung?.Invoke();
        AudioManager.Instance?.PlayClockRing();
        Debug.Log($"[AlarmClock] {gameObject.name} 鬧鐘響起！");
    }

    public void TurnOff()
    {
        if (!_isRinging) return;
        _isRinging = false;
        ApplyState();
        OnAlarmTurnedOff?.Invoke();
        AudioManager.Instance?.StopClockRing();
        Debug.Log($"[AlarmClock] {gameObject.name} 鬧鐘被關閉。");
    }

    void ApplyState()
    {
        if (_isRinging)
            floatingText?.Play();
        else
            floatingText?.Stop();
    }

    protected override void SaveSnapshot()
    {
        _snapshotIsRinging = _isRinging;
        _snapshotStepCounter = _stepCounter;
    }

    protected override void RestoreSnapshot()
    {
        _stepCounter = _snapshotStepCounter;
        _isRinging = _snapshotIsRinging;
        ApplyState();
        if (_isRinging)
        {
            AudioManager.Instance?.PlayClockRing();
            OnAlarmRung?.Invoke();
        }
        else
        {
            AudioManager.Instance?.StopClockRing();
            OnAlarmTurnedOff?.Invoke();
        }
    }

    protected override void OnResetInternal()
    {
        _isRinging = false;
        _stepCounter = 0;
        AudioManager.Instance?.StopClockRing();
        ApplyState();
        if (ringOnStart)
        {
            Ring();
        }
        else
        {
            OnAlarmTurnedOff?.Invoke();
        }
        Debug.Log($"[AlarmClock] {gameObject.name} 重置為初始狀態。");
    }
}
