using UnityEditor;
using UnityEngine;
using CustomAttributes;

[CustomPropertyDrawer(typeof(EnableIfAttribute))]
public class EnableIfDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.serializedObject == null || property.serializedObject.targetObject == null)
        {
            return;
        }
        
        EnableIfAttribute enableIf = (EnableIfAttribute)attribute;
        
        bool enabled = GetConditionValue(property, enableIf.ConditionFieldName);

        bool previousEnabledState = GUI.enabled;

        GUI.enabled = enabled;

        EditorGUI.PropertyField(position, property, label, true);

        GUI.enabled = previousEnabledState;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }

    private bool GetConditionValue(SerializedProperty property, string conditionName)
    {
        string path = property.propertyPath;
        string conditionPath = path.Contains(".") 
            ? path.Substring(0, path.LastIndexOf('.')) + "." + conditionName 
            : conditionName;

        SerializedProperty conditionProperty = property.serializedObject.FindProperty(conditionPath);

        if (conditionProperty != null && conditionProperty.propertyType == SerializedPropertyType.Boolean)
        {
            return conditionProperty.boolValue;
        }
        Object target = property.serializedObject.targetObject;
        if(target != null)
            Debug.LogWarning($"[EnableIf] could not find a property with the name '{conditionName}' in {target.name}");
        return true;
    }
}
