using System.Collections.Generic;
using UnityEngine;

public class InteractionTrigger : MonoBehaviour
{
    private readonly List<IInteractable> _interactables = new();
    private readonly List<IKeepable> _keepables = new();

    void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<IInteractable>(out var i))
            _interactables.Add(i);

        if (other.TryGetComponent<IKeepable>(out var k))
            _keepables.Add(k);
    }

    void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<IInteractable>(out var i))
            _interactables.Remove(i);

        if (other.TryGetComponent<IKeepable>(out var k))
            _keepables.Remove(k);
    }

    public bool TriggerInteract()
    {
        if (_interactables.Count == 0) return false;

        foreach (var i in _interactables)
            i.Interact();

        return true;
    }

    public bool TriggerKeep()
    {
        if (_keepables.Count == 0) return false;

        foreach (var k in _keepables)
            k.OnKeep();

        return true;
    }
}
