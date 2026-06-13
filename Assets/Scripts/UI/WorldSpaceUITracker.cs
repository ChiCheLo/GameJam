using UnityEngine;

public class WorldSpaceUITracker : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0, 1f, 0);

    private RectTransform _rect;
    private Camera _cam;

    void Awake()
    {
        _rect = GetComponent<RectTransform>();
        _cam = Camera.main;
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 screenPos = _cam.WorldToScreenPoint(target.position + offset);

        if (screenPos.z < 0)
        {
            _rect.gameObject.SetActive(false);
            return;
        }

        _rect.gameObject.SetActive(true);
        _rect.position = screenPos;
    }
}
