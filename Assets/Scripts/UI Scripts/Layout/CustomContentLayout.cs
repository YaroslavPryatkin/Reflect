using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
[DefaultExecutionOrder(-100)]
public class CustomContentLayout : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform parentRect;

    [Header("Layout Settings")]
    [SerializeField, Range(0.01f, 1f)] private float heightFraction = 0.2f;
    [SerializeField] private float spacing = 5f;

    private RectTransform _myRectTransform;
    private readonly List<RectTransform> _children = new ();

    private void Awake()
    {
        _myRectTransform = GetComponent<RectTransform>();

        if (parentRect == null && transform.parent != null)
        {
            parentRect = transform.parent as RectTransform;
        }
        CollectChildren();
    }

    private void OnEnable()
    {
        UpdateLayout();
    }

    private void OnRectTransformDimensionsChange()
    {
        UpdateLayout();
    }

    public void CollectChildren()
    {
        _children.Clear();
        for (int i = 0; i < transform.childCount; i++)
        {
            var child = transform.GetChild(i) as RectTransform;
            if (child != null && child.gameObject.activeSelf)
            {
                _children.Add(child);
            }
        }
    }


    [ContextMenu("Update Layout")]
    public void EditorUpdateLayout()
    {
        CollectChildren();
        UpdateLayout();
    }
    
    public void UpdateLayout()
    {
        if (parentRect == null || _myRectTransform == null) return;

        if (_children.Count == 0 && transform.childCount > 0)
        {
            CollectChildren();
        }

        float parentHeight = parentRect.rect.height;
        float itemHeight = parentHeight * heightFraction;
        float currentY = 0f;

        for (int i = 0; i < _children.Count; i++)
        {
            var child = _children[i];
            if (child == null || !child.gameObject.activeSelf) continue;

            child.anchorMin = new Vector2(0f, 1f);
            child.anchorMax = new Vector2(1f, 1f);
            child.pivot = new Vector2(0.5f, 1f);

            child.sizeDelta = new Vector2(0f, itemHeight);
            child.anchoredPosition = new Vector2(0f, -currentY);

            currentY += itemHeight + spacing;
        }

        float totalContentHeight = Mathf.Max(0f, currentY - spacing);

        _myRectTransform.anchorMin = new Vector2(0f, 1f);
        _myRectTransform.anchorMax = new Vector2(1f, 1f);
        _myRectTransform.pivot = new Vector2(0.5f, 1f);
        _myRectTransform.sizeDelta = new Vector2(_myRectTransform.sizeDelta.x, totalContentHeight);
    }

    private void OnValidate()
    {
        if (_myRectTransform == null) _myRectTransform = GetComponent<RectTransform>();
        if (parentRect == null && transform.parent != null) parentRect = transform.parent as RectTransform;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.delayCall += QueueUpdateLayout;
#endif
    }

#if UNITY_EDITOR
    private void QueueUpdateLayout()
    {
        UnityEditor.EditorApplication.delayCall -= QueueUpdateLayout;
        if (this == null) return;
        CollectChildren();
        UpdateLayout();
    }
#endif
}