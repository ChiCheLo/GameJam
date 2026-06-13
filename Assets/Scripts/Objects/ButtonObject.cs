using UnityEngine;

public class ButtonObject : KeepableBase, IInteractable
{
    [SerializeField] private Renderer buttonRenderer;
    [SerializeField] private DoorObject linkedDoor;

    private static readonly Color OpenColor  = Color.green;
    private static readonly Color CloseColor = Color.red;

    private bool _isOpen;
    private bool _snapshotIsOpen;

    void Start()
    {
        ApplyState();
    }

    public string InteractLabel => "按下";

    public void Interact()
    {
        _isOpen = !_isOpen;
        ApplyState();
    }

    protected override void SaveSnapshot() => _snapshotIsOpen = _isOpen;

    protected override void RestoreSnapshot()
    {
        _isOpen = _snapshotIsOpen;
        ApplyState();
    }

    protected override void OnResetInternal()
    {
        _isOpen = false;
        ApplyState();
    }

    void ApplyState()
    {
        if (buttonRenderer != null)
            buttonRenderer.material.color = _isOpen ? OpenColor : CloseColor;

        if (linkedDoor != null)
            linkedDoor.SetOpen(_isOpen);
    }
}
