using UnityEngine;
using UnityEngine.Events;

public class CannonProjectileTrigger : MonoBehaviour
{
    [SerializeField] private UnityEvent onHitNpc;

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerGridMovement>() != null)
        {
            LevelManager.ResetAll();
            return;
        }

        if (other.GetComponent<NpcPatrolMovement>() != null)
            onHitNpc.Invoke();
    }
}
