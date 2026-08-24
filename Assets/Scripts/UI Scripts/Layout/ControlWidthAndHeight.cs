using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
[DefaultExecutionOrder(1)]
public class ControlWidthAndHeight : MonoBehaviour
{
    [SerializeField] private bool shouldInvert = false;
    [SerializeField] private RectTransform parentRect;

    [Header("Layout")] 
    [SerializeField] private Vector2 applyFractions = new(1f, 1f);
    [SerializeField] private Vector2 fractions = new (1f, 1f);
    [SerializeField] private Vector2 staticChange = new (0f, 0f);
    [SerializeField] private Vector2 crossAddFractions = new (0f, 0f);
    
    private RectTransform _rectTransform;
    private RectTransform _currentParentRect;

    private void OnEnable()=>UpdateLayout();

    private void OnRectTransformDimensionsChange()=>UpdateLayout();
    
    private void OnValidate()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.delayCall += QueueUpdateLayout;
#endif
    }

    private void QueueUpdateLayout()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.delayCall -= QueueUpdateLayout;
        if (this == null) return;

        UpdateLayout();
#endif
    }

    [ContextMenu("Update Layout")]
    private void UpdateLayout()
    {
        _rectTransform = GetComponent<RectTransform>();
        if (parentRect == null)
        {
            if (transform.parent != null)
                _currentParentRect = transform.parent as RectTransform;
            else
            {
                _currentParentRect = null;
                return;
            }
        }
        else
        {
            _currentParentRect = parentRect;
        }


        if (_currentParentRect == null || _rectTransform == null) return;
        
        var almostFinalWidth = _currentParentRect.rect.width * fractions.x + staticChange.x;
        var almostFinalHeight = _currentParentRect.rect.height * fractions.y + staticChange.y;

        var finalWidth = almostFinalWidth + crossAddFractions.x * almostFinalHeight;
        var finalHeight = almostFinalHeight + crossAddFractions.y * almostFinalWidth;

        if(shouldInvert)
            _rectTransform.sizeDelta = new Vector2(finalHeight * applyFractions.x, finalWidth * applyFractions.y);
        else
            _rectTransform.sizeDelta = new Vector2(finalWidth * applyFractions.x, finalHeight * applyFractions.y);
    }
}
