using UnityEngine;

public class NextLevelTrigger : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<PlayerGridMovement>(out _))
        {
            LevelManager.Instance.NextLevel();
            gameObject.SetActive(false);
        }
    }
}
