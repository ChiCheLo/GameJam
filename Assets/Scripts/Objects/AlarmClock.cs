using UnityEngine;

public class AlarmClock : KeepableBase, IInteractable
{
    [SerializeField] private FloatingText floatingText;

    private bool _isRinging;
    private bool _snapshotIsRinging;

    public void Interact()
    {
        _isRinging = !_isRinging;
        ApplyState();
    }

    void ApplyState()
    {
        if (_isRinging)
            floatingText?.Play();
        else
            floatingText?.Stop();
    }

    protected override void SaveSnapshot() => _snapshotIsRinging = _isRinging;

    protected override void RestoreSnapshot()
    {
        _isRinging = _snapshotIsRinging;
        ApplyState();
    }

    protected override void OnResetInternal()
    {
        _isRinging = false;
        ApplyState();
    }
}
