using UnityEngine;

public abstract class KeepableBase : MonoBehaviour, IKeepable, IResettable
{
    [SerializeField] private GameObject targetUI;

    protected bool IsKept;

    public void OnKeep()
    {
        IsKept = !IsKept;

        if (IsKept)
        {
            targetUI?.SetActive(true);
            SaveSnapshot();
            OnKeepInternal();
        }
        else
        {
            targetUI?.SetActive(false);
            OnUnkeepInternal();
        }
    }

    public void OnReset()
    {
        if (IsKept)
        {
            RestoreSnapshot();
            return;
        }

        targetUI?.SetActive(false);
        OnResetInternal();
    }

    protected virtual void OnKeepInternal() { }
    protected virtual void OnUnkeepInternal() { }
    protected abstract void SaveSnapshot();
    protected abstract void RestoreSnapshot();
    protected abstract void OnResetInternal();
}
