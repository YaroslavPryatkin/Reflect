using UnityEditor;
using UnityEngine;
using CustomAttributes;
using System.Collections.Generic;
using System.Text.RegularExpressions;

// [CustomPropertyDrawer(typeof(EnableIfAttribute))]
// public class EnableIfDrawer : PropertyDrawer
// {
//     public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
//     {
//         if (property.serializedObject == null || property.serializedObject.targetObject == null)
//         {
//             return;
//         }
//         
//         EnableIfAttribute enableIf = (EnableIfAttribute)attribute;
//         
//         bool enabled = GetConditionValue(property, enableIf.ConditionFieldName);
//
//         bool previousEnabledState = GUI.enabled;
//
//         GUI.enabled = enabled;
//
//         EditorGUI.PropertyField(position, property, label, true);
//
//         GUI.enabled = previousEnabledState;
//     }
//
//     public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
//     {
//         return EditorGUI.GetPropertyHeight(property, label, true);
//     }
//
//     private bool GetConditionValue(SerializedProperty property, string conditionName)
//     {
//         string path = property.propertyPath;
//         string conditionPath = path.Contains(".") 
//             ? path.Substring(0, path.LastIndexOf('.')) + "." + conditionName 
//             : conditionName;
//
//         SerializedProperty conditionProperty = property.serializedObject.FindProperty(conditionPath);
//
//         if (conditionProperty != null && conditionProperty.propertyType == SerializedPropertyType.Boolean)
//         {
//             return conditionProperty.boolValue;
//         }
//         Object target = property.serializedObject.targetObject;
//         if(target != null)
//             Debug.LogWarning($"[EnableIf] could not find a property with the name '{conditionName}' in {target.name}");
//         return true;
//     }
// }

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
        
        bool enabled = EvaluateExpression(property, enableIf.ConditionFieldName);

        bool previousEnabledState = GUI.enabled;
        GUI.enabled = enabled;

        EditorGUI.PropertyField(position, property, label, true);

        GUI.enabled = previousEnabledState;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }

    private bool EvaluateExpression(SerializedProperty property, string expression)
    {
        if (string.IsNullOrEmpty(expression)) return true;

        // Разделяем строку по операторам && и ||, сохраняя сами операторы в массиве
        string[] tokens = Regex.Split(expression, @"(&&|\|\|)");
        
        List<bool> values = new List<bool>();
        List<string> operators = new List<string>();

        for (int i = 0; i < tokens.Length; i++)
        {
            string token = tokens[i].Trim();
            if (string.IsNullOrEmpty(token)) continue;

            if (token == "&&" || token == "||")
            {
                operators.Add(token);
            }
            else
            {
                values.Add(GetSingleConditionValue(property, token));
            }
        }

        // Проверка на корректность синтаксиса выражения
        if (values.Count == 0) return true;
        if (values.Count != operators.Count + 1)
        {
            Debug.LogWarning($"[EnableIf] Incorrect format of the expression: '{expression}' in object {property.serializedObject.targetObject.name}");
            return true;
        }

        for (int i = 0; i < operators.Count; i++)
        {
            if (operators[i] == "&&")
            {
                values[i] = values[i] && values[i + 1];
                values.RemoveAt(i + 1);
                operators.RemoveAt(i);
                i--;
            }
        }

        bool result = values[0];
        for (int i = 0; i < operators.Count; i++)
        {
            if (operators[i] == "||")
            {
                result = result || values[i + 1];
            }
        }

        return result;
    }

    private bool GetSingleConditionValue(SerializedProperty property, string conditionName)
    {
        bool negate = false;

        if (conditionName.StartsWith("!"))
        {
            negate = true;
            conditionName = conditionName.Substring(1).Trim();
        }

        string path = property.propertyPath;
        
        string conditionPath = path.Contains(".") 
            ? path.Substring(0, path.LastIndexOf('.')) + "." + conditionName 
            : conditionName;

        SerializedProperty conditionProperty = property.serializedObject.FindProperty(conditionPath);

        if (conditionProperty == null)
        {
            conditionProperty = property.serializedObject.FindProperty(conditionName);
        }

        bool value = true;

        if (conditionProperty != null && conditionProperty.propertyType == SerializedPropertyType.Boolean)
        {
            value = conditionProperty.boolValue;
        }
        else
        {
            Object target = property.serializedObject.targetObject;
            if (target != null)
            {
                Debug.LogWarning($"[EnableIf] Haven't found '{conditionName}' in object {target.name}");
            }
        }

        return negate ? !value : value;
    }
}
