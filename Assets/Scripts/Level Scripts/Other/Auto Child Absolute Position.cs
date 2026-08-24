using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class AutoChildAbsolutePosition : MonoBehaviour
{
    [HideInInspector] [SerializeField] private bool useTargetX;
    [HideInInspector] [SerializeField] private float targetX = 1f;

    [HideInInspector] [SerializeField] private bool useTargetY;
    [HideInInspector] [SerializeField] private float targetY = 1f;

    [HideInInspector] [SerializeField] private bool useTargetZ;
    [HideInInspector] [SerializeField] private float targetZ = 1f;
    
    private Vector3 _lastParentScale;
    private bool _hasParent = false;

    private void Awake()
    {
        _hasParent = (transform.parent != null);
    }
        
    private void Start()
    {
        if(_hasParent)
            AdjustPosition(transform.parent);
    }

    private void OnValidate()
    {
        UpdateAnyway();
    }

    public void UpdateAnyway()
    {
        if (transform.parent != null)
        {
            AdjustPosition(transform.parent);
            _hasParent = true;
        }
        else
        {
            _hasParent = false;
        }
    }

    private void LateUpdate()
    {
        if (!_hasParent) return;

        var parent = transform.parent;
        if (parent.lossyScale != _lastParentScale)
        {
            AdjustPosition(parent);
        }
    }
    
    public void AdjustPosition(Transform parent)
    {
        _lastParentScale = parent.lossyScale;

        var newLocalPosition = transform.localPosition;

        if (useTargetX && Mathf.Abs(_lastParentScale.x) > 0.0001f)
        {
            newLocalPosition.x = targetX / _lastParentScale.x;
        }

        if (useTargetY && Mathf.Abs(_lastParentScale.y) > 0.0001f)
        {
            newLocalPosition.y = targetY / _lastParentScale.y;
        }

        if (useTargetZ && Mathf.Abs(_lastParentScale.z) > 0.0001f)
        {
            newLocalPosition.z = targetZ / _lastParentScale.z;
        }

        if (transform.localPosition != newLocalPosition)
        {
            transform.localPosition = newLocalPosition;
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(AutoChildAbsolutePosition))]
public class AutoChildAbsolutePositionEditor : Editor
{
    private SerializedProperty useTargetX, targetX;
    private SerializedProperty useTargetY, targetY;
    private SerializedProperty useTargetZ, targetZ;

    private void OnEnable()
    {
        useTargetX = serializedObject.FindProperty("useTargetX");
        targetX = serializedObject.FindProperty("targetX");

        useTargetY = serializedObject.FindProperty("useTargetY");
        targetY = serializedObject.FindProperty("targetY");

        useTargetZ = serializedObject.FindProperty("useTargetZ");
        targetZ = serializedObject.FindProperty("targetZ");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Absolute World Position Modifiers", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Turn on the axis and set absolute position:", EditorStyles.miniLabel);
        EditorGUILayout.Space(5);

        DrawAxisRow("X", useTargetX, targetX);
        DrawAxisRow("Y", useTargetY, targetY);
        DrawAxisRow("Z", useTargetZ, targetZ);

        EditorGUILayout.Space(10);
        
        if (GUILayout.Button("Force Recalculate Position", GUILayout.Height(25)))
        {
            (target as AutoChildAbsolutePosition)?.UpdateAnyway();
        }
        
        if (serializedObject.ApplyModifiedProperties())
        {
            (target as AutoChildAbsolutePosition)?.UpdateAnyway();
        }
    }

    private void DrawAxisRow(string axisName, SerializedProperty useTarget, SerializedProperty targetValue)
    {
        EditorGUILayout.BeginHorizontal();

        useTarget.boolValue = GUILayout.Toggle(
            useTarget.boolValue, 
            axisName, 
            "Button", 
            GUILayout.Width(35), 
            GUILayout.Height(20)
        );

        EditorGUI.BeginDisabledGroup(!useTarget.boolValue);
        
        EditorGUILayout.PropertyField(targetValue, GUIContent.none, GUILayout.Height(20));
        
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(2);
    }
}
#endif
