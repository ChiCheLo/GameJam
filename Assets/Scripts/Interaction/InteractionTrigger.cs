using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class InteractionTrigger : MonoBehaviour
{
    [SerializeField] private GameObject interactUI;
    [SerializeField] private GameObject keepUI;

    private readonly List<IInteractable> _interactables = new();
    private readonly List<IKeepable> _keepables = new();

    void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<IInteractable>(out var i))
            _interactables.Add(i);

        if (other.TryGetComponent<IKeepable>(out var k))
            _keepables.Add(k);

        UpdateUI();
    }

    void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<IInteractable>(out var i))
            _interactables.Remove(i);

        if (other.TryGetComponent<IKeepable>(out var k))
            _keepables.Remove(k);

        UpdateUI();
    }

    public bool TriggerInteract()
    {
        var valid = _interactables.Where(i => !IsKept(i)).ToList();
        if (valid.Count == 0) return false;

        foreach (var i in valid)
            i.Interact();

        UpdateUI();
        return true;
    }

    public bool TriggerKeep()
    {
        if (_keepables.Count == 0) return false;

        foreach (var k in _keepables)
            k.OnKeep();

        UpdateUI();
        return true;
    }

    void UpdateUI()
    {
        bool hasInteractable = _interactables.Any(i => !IsKept(i));
        bool hasKeepable     = _keepables.Count > 0;

        interactUI?.SetActive(hasInteractable);
        keepUI?.SetActive(hasKeepable);
    }

    bool IsKept(IInteractable i)
    {
        return i is KeepableBase kb && kb.IsKept;
    }
}
