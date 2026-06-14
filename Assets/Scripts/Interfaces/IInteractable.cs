using UnityEngine;

public interface IInteractable
{
    void Interact();
    Sprite InteractSprite { get; }
}
