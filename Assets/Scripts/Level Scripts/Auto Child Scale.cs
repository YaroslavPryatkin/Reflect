using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class AutoChildScale : MonoBehaviour
{
    [HideInInspector] [SerializeField] private bool useDeltaX;
    [HideInInspector] [SerializeField] private float deltaX;

    [HideInInspector] [SerializeField] private bool useDeltaY;
    [HideInInspector] [SerializeField] private float deltaY;

    [HideInInspector] [SerializeField] private bool useDeltaZ;
    [HideInInspector] [SerializeField] private float deltaZ;

    private void Start()
    {
        AdjustScale();
    }

    private void OnValidate()
    {
        if (transform.parent != null)
        {
            AdjustScale();
        }
    }

    [ContextMenu("Adjust Scale Now")]
    public void AdjustScale()
    {
        Transform parent = transform.parent;
        if (parent == null) return;

        var parentWorldScale = parent.lossyScale;
        var newLocalScale = transform.localScale;

        if (useDeltaX && parentWorldScale.x > 0.001f)
        {
            var targetWorldX = Mathf.Max(0.01f, parentWorldScale.x + deltaX);
            newLocalScale.x = targetWorldX / parentWorldScale.x;
        }

        if (useDeltaY && parentWorldScale.y > 0.001f)
        {
            var targetWorldY = Mathf.Max(0.01f, parentWorldScale.y + deltaY);
            newLocalScale.y = targetWorldY / parentWorldScale.y;
        }

        if (useDeltaZ && parentWorldScale.z > 0.001f)
        {
            var targetWorldZ = Mathf.Max(0.01f, parentWorldScale.z + deltaZ);
            newLocalScale.z = targetWorldZ / parentWorldScale.z;
        }

        transform.localScale = newLocalScale;
    }
}


#if UNITY_EDITOR
[CustomEditor(typeof(AutoChildScale))]
public class AutoChildScaleEditor : Editor
{
    private SerializedProperty useDeltaX, deltaX;
    private SerializedProperty useDeltaY, deltaY;
    private SerializedProperty useDeltaZ, deltaZ;

    private void OnEnable()
    {
        useDeltaX = serializedObject.FindProperty("useDeltaX");
        deltaX = serializedObject.FindProperty("deltaX");

        useDeltaY = serializedObject.FindProperty("useDeltaY");
        deltaY = serializedObject.FindProperty("deltaY");

        useDeltaZ = serializedObject.FindProperty("useDeltaZ");
        deltaZ = serializedObject.FindProperty("deltaZ");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Axis Scale Modifiers", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Turn on the axis and set absolute delta:", EditorStyles.miniLabel);
        EditorGUILayout.Space(5);

        DrawAxisRow("X", useDeltaX, deltaX);
        DrawAxisRow("Y", useDeltaY, deltaY);
        DrawAxisRow("Z", useDeltaZ, deltaZ);

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Force Recalculate Scale", GUILayout.Height(25)))
        {
            (target as AutoChildScale)?.AdjustScale();
        }

        if (serializedObject.ApplyModifiedProperties())
        {
            (target as AutoChildScale)?.AdjustScale();
        }
    }

    private void DrawAxisRow(string axisName, SerializedProperty useDelta, SerializedProperty deltaValue)
    {
        EditorGUILayout.BeginHorizontal();

        useDelta.boolValue = GUILayout.Toggle(
            useDelta.boolValue, 
            axisName, 
            "Button", 
            GUILayout.Width(35), 
            GUILayout.Height(20)
        );

        EditorGUI.BeginDisabledGroup(!useDelta.boolValue);
        
        EditorGUILayout.PropertyField(deltaValue, GUIContent.none, GUILayout.Height(20));
        
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(2);
    }
}
#endif