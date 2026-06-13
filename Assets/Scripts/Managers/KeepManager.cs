using UnityEngine;

public class KeepManager : MonoBehaviour
{
    public static KeepManager Instance { get; private set; }

    private KeepableBase _currentKept;

    void Awake()
    {
        Instance = this;
    }

    public void RegisterKept(KeepableBase newKept)
    {
        if (_currentKept != null && _currentKept != newKept)
            _currentKept.ForceUnkeep();

        _currentKept = newKept;
    }

    public void UnregisterKept(KeepableBase kept)
    {
        if (_currentKept == kept)
            _currentKept = null;
    }
}
