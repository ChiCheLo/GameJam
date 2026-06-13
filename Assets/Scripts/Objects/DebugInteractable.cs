using UnityEngine;

public class DebugInteractable : MonoBehaviour, IInteractable
{
    public void Interact()
    {
        Debug.Log($"[Interact] {gameObject.name}");
    }
}
