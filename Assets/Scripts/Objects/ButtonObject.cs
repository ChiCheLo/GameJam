using UnityEngine;

public class ButtonObject : KeepableBase, IInteractable
{
    [SerializeField] private Light pointLight;
    [SerializeField] private Color openColor  = Color.green;
    [SerializeField] private Color closeColor = Color.red;
    [SerializeField] private DoorObject linkedDoor;
    [SerializeField] private Sprite interactSprite;

    private bool _isOpen;
    private bool _snapshotIsOpen;

    public Sprite InteractSprite => interactSprite;

    void Start()
    {
        ApplyState();
    }

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
        if (pointLight != null)
            pointLight.color = _isOpen ? openColor : closeColor;

        if (linkedDoor != null)
            linkedDoor.SetOpen(_isOpen);
    }
}
