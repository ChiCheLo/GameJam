using UnityEngine;

public class InteractableHighlight : MonoBehaviour
{
    [SerializeField] private Renderer highlightRenderer;

    private KeepableBase _keepable;
    private Material _material;
    private static readonly int HighlightActiveId = Shader.PropertyToID("_HighlightActive");

    void Awake()
    {
        _keepable = GetComponent<KeepableBase>();
        if (highlightRenderer != null)
            _material = highlightRenderer.material;
    }

    void Update()
    {
        if (_material == null) return;

        bool isInteractable = _keepable == null || !_keepable.IsKept;
        _material.SetFloat(HighlightActiveId, isInteractable ? 1f : 0f);
    }
}
