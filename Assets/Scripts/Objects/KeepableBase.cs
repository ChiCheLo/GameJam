using UnityEngine;

public abstract class KeepableBase : MonoBehaviour, IKeepable, IResettable
{
    [SerializeField] private GameObject targetUI;

    public bool IsKept { get; private set; }

    public void OnKeep()
    {
        IsKept = !IsKept;

        if (IsKept)
        {
            KeepManager.Instance?.RegisterKept(this);
            targetUI?.SetActive(true);
            SaveSnapshot();
            OnKeepInternal();
        }
        else
        {
            KeepManager.Instance?.UnregisterKept(this);
            targetUI?.SetActive(false);
            OnUnkeepInternal();
        }
    }

    // 給 KeepManager 呼叫，強制取消 Keep 而不觸發 RegisterKept
    internal void ForceUnkeep()
    {
        IsKept = false;
        targetUI?.SetActive(false);
        OnUnkeepInternal();
    }

    public void OnReset()
    {
        Debug.Log($"[KeepableBase] {gameObject.name} OnReset | IsKept={IsKept}");
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
