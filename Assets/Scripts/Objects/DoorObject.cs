using UnityEngine;

public class DoorObject : MonoBehaviour
{
    public void SetOpen(bool isOpen)
    {
        gameObject.SetActive(!isOpen);
    }
}
