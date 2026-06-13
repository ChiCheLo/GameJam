using System.Collections.Generic;
using UnityEngine;

public class InteractionTrigger : MonoBehaviour
{
    private readonly List<IInteractable> _interactables = new();

    void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<IInteractable>(out var i))
            _interactables.Add(i);
    }

    void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<IInteractable>(out var i))
            _interactables.Remove(i);
    }

    public bool TriggerInteract()
    {
        if (_interactables.Count == 0) return false;

        foreach (var i in _interactables)
            i.Interact();

        return true;
    }
}
