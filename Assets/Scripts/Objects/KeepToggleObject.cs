using UnityEngine;

public class KeepToggleObject : KeepableBase
{
    [SerializeField] private GameObject targetObject;

    private bool _snapshotActive;

    void Start()
    {
        targetObject?.SetActive(false);
    }

    protected override void SaveSnapshot()
        => _snapshotActive = targetObject != null && targetObject.activeSelf;

    protected override void RestoreSnapshot()
        => targetObject?.SetActive(_snapshotActive);

    protected override void OnKeepInternal()
        => targetObject?.SetActive(true);

    protected override void OnUnkeepInternal()
        => targetObject?.SetActive(false);

    protected override void OnResetInternal()
        => targetObject?.SetActive(false);
}
