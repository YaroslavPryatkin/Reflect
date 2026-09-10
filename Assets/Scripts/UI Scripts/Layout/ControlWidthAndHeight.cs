using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
[DefaultExecutionOrder(5)]
public class ControlWidthAndHeight : MonoBehaviour
{
    [SerializeField] private bool shouldInvert = false;
    [SerializeField] private RectTransform parentRect;

    [Header("Layout")] 
    [SerializeField] private Vector2 fractionsFromParent = new (1f, 1f);
    [SerializeField] private Vector2 staticChange = new (0f, 0f);
    [SerializeField] private Vector2 crossAddFractions = new (0f, 0f);
    [SerializeField] private Vector2 applyFractions = new (1f, 1f);
    
    private RectTransform _rectTransform;
    private RectTransform _currentParentRect;
    private void OnEnable()
    {
        Canvas.willRenderCanvases += ScheduledUpdate;
    }

    private void OnDisable()
    {
        Canvas.willRenderCanvases -= ScheduledUpdate;
    }

    private void ScheduledUpdate()
    {
        Canvas.willRenderCanvases -= ScheduledUpdate;
        UpdateLayout();
    }

    private void OnRectTransformDimensionsChange() => ScheduledUpdate();

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


        if (_currentParentRect == null || _rectTransform == null)
        {
            return;
        }
        
        var almostFinalWidth = _currentParentRect.rect.width * fractionsFromParent.x + staticChange.x;
        var almostFinalHeight = _currentParentRect.rect.height * fractionsFromParent.y + staticChange.y;

        var finalWidth = (almostFinalWidth + crossAddFractions.x * almostFinalHeight)*applyFractions.x;
        var finalHeight = (almostFinalHeight + crossAddFractions.y * almostFinalWidth)*applyFractions.y;

        _rectTransform.sizeDelta = shouldInvert ? 
            new Vector2(finalHeight, finalWidth) : new Vector2(finalWidth, finalHeight);
    }
}
