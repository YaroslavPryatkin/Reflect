// using UnityEditor;
// using UnityEngine;
// using CustomAttributes;
// using System.Collections.Generic;
// using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using CustomAttributes; 
using System;
using System.Globalization;

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
//         bool enabled = EvaluateExpression(property, enableIf.ConditionFieldName);
//
//         bool previousEnabledState = GUI.enabled;
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
//     private bool EvaluateExpression(SerializedProperty property, string expression)
//     {
//         if (string.IsNullOrEmpty(expression)) return true;
//
//         // Разделяем строку по операторам && и ||, сохраняя сами операторы в массиве
//         string[] tokens = Regex.Split(expression, @"(&&|\|\|)");
//         
//         List<bool> values = new List<bool>();
//         List<string> operators = new List<string>();
//
//         for (int i = 0; i < tokens.Length; i++)
//         {
//             string token = tokens[i].Trim();
//             if (string.IsNullOrEmpty(token)) continue;
//
//             if (token == "&&" || token == "||")
//             {
//                 operators.Add(token);
//             }
//             else
//             {
//                 values.Add(GetSingleConditionValue(property, token));
//             }
//         }
//
//         if (values.Count == 0) return true;
//         if (values.Count != operators.Count + 1)
//         {
//             Debug.LogWarning($"[EnableIf] Incorrect format of the expression: '{expression}' in object {property.serializedObject.targetObject.name}");
//             return true;
//         }
//
//         for (int i = 0; i < operators.Count; i++)
//         {
//             if (operators[i] == "&&")
//             {
//                 values[i] = values[i] && values[i + 1];
//                 values.RemoveAt(i + 1);
//                 operators.RemoveAt(i);
//                 i--;
//             }
//         }
//
//         bool result = values[0];
//         for (int i = 0; i < operators.Count; i++)
//         {
//             if (operators[i] == "||")
//             {
//                 result = result || values[i + 1];
//             }
//         }
//
//         return result;
//     }
//
//     private bool GetSingleConditionValue(SerializedProperty property, string conditionName)
//     {
//         bool negate = false;
//
//         if (conditionName.StartsWith("!"))
//         {
//             negate = true;
//             conditionName = conditionName.Substring(1).Trim();
//         }
//
//         string path = property.propertyPath;
//         
//         string conditionPath = path.Contains(".") 
//             ? path.Substring(0, path.LastIndexOf('.')) + "." + conditionName 
//             : conditionName;
//
//         SerializedProperty conditionProperty = property.serializedObject.FindProperty(conditionPath);
//
//         if (conditionProperty == null)
//         {
//             conditionProperty = property.serializedObject.FindProperty(conditionName);
//         }
//
//         bool value = true;
//
//         if (conditionProperty != null && conditionProperty.propertyType == SerializedPropertyType.Boolean)
//         {
//             value = conditionProperty.boolValue;
//         }
//         else
//         {
//             Object target = property.serializedObject.targetObject;
//             if (target != null)
//             {
//                 Debug.LogWarning($"[EnableIf] Haven't found '{conditionName}' in object {target.name}");
//             }
//         }
//
//         return negate ? !value : value;
//     }
// }

[CustomPropertyDrawer(typeof(EnableIfAttribute))]
public class EnableIfDrawer : PropertyDrawer
{
    private ExpressionParser _parser = new ExpressionParser();

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.serializedObject == null || property.serializedObject.targetObject == null)
        {
            return;
        }
        
        EnableIfAttribute enableIf = (EnableIfAttribute)attribute;
        
        bool enabled = _parser.Evaluate(enableIf.ConditionFieldName, property);

        bool previousEnabledState = GUI.enabled;
        GUI.enabled = enabled;

        EditorGUI.PropertyField(position, property, label, true);

        GUI.enabled = previousEnabledState;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }

    private class ExpressionParser
    {
        private enum CompareOp
        {
            Equal, NotEqual, Less, Greater, LessOrEqual, GreaterOrEqual
        }

        private string _text;
        private int _pos;
        private SerializedProperty _rootProperty;

        public bool Evaluate(string expression, SerializedProperty property)
        {
            if (string.IsNullOrEmpty(expression)) return true;
            
            _text = expression;
            _pos = 0;
            _rootProperty = property;
            
            try
            {
                return ParseExpression();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[EnableIf] Error parsing expression: '{expression}' in object {property.serializedObject.targetObject.name}. Error: {e.Message}");
                return true;
            }
        }

        private void SkipWhitespace()
        {
            while (_pos < _text.Length && char.IsWhiteSpace(_text[_pos]))
                _pos++;
        }

        private bool Match(string s)
        {
            SkipWhitespace();
            if (_pos + s.Length <= _text.Length && _text.Substring(_pos, s.Length) == s)
            {
                _pos += s.Length;
                return true;
            }
            return false;
        }

        private string ReadIdentifier()
        {
            SkipWhitespace();
            int start = _pos;
            // Added '-' to support negative numbers in values (e.g., Level > -1)
            while (_pos < _text.Length && (char.IsLetterOrDigit(_text[_pos]) || _text[_pos] == '_' || _text[_pos] == '.' || _text[_pos] == '-'))
            {
                _pos++;
            }
            return _text.Substring(start, _pos - start);
        }
        
        private string ReadValue()
        {
            SkipWhitespace();
            if (_pos >= _text.Length) return "";

            if (_text[_pos] == '"' || _text[_pos] == '\'')
            {
                char quote = _text[_pos];
                _pos++;
                int start = _pos;
                while (_pos < _text.Length && _text[_pos] != quote) _pos++;
                string val = _text.Substring(start, _pos - start);
                if (_pos < _text.Length) _pos++; 
                return val;
            }
            return ReadIdentifier();
        }

        private bool ParseExpression()
        {
            bool result = ParseTerm();
            while (Match("||"))
            {
                bool right = ParseTerm();
                result = result || right;
            }
            return result;
        }

        private bool ParseTerm()
        {
            bool result = ParseFactor();
            while (Match("&&"))
            {
                bool right = ParseFactor();
                result = result && right;
            }
            return result;
        }

        private bool ParseFactor()
        {
            SkipWhitespace();
            if (Match("!"))
            {
                return !ParseFactor();
            }
            if (Match("("))
            {
                bool result = ParseExpression();
                Match(")"); // Expecting closing parenthesis
                return result;
            }
            return ParseCondition();
        }

        private bool ParseCondition()
        {
            string left = ReadIdentifier();
            
            // Order matters: must check two-char operators before one-char operators
            if (Match("==")) return EvaluateComparison(left, ReadValue(), CompareOp.Equal);
            if (Match("!=")) return EvaluateComparison(left, ReadValue(), CompareOp.NotEqual);
            if (Match("<=")) return EvaluateComparison(left, ReadValue(), CompareOp.LessOrEqual);
            if (Match(">=")) return EvaluateComparison(left, ReadValue(), CompareOp.GreaterOrEqual);
            if (Match("<"))  return EvaluateComparison(left, ReadValue(), CompareOp.Less);
            if (Match(">"))  return EvaluateComparison(left, ReadValue(), CompareOp.Greater);
            
            return EvaluateBoolean(left);
        }

        private SerializedProperty FindProperty(string name)
        {
            string path = _rootProperty.propertyPath;
            string conditionPath = path.Contains(".") 
                ? path.Substring(0, path.LastIndexOf('.')) + "." + name 
                : name;

            SerializedProperty conditionProperty = _rootProperty.serializedObject.FindProperty(conditionPath);
            if (conditionProperty == null)
            {
                conditionProperty = _rootProperty.serializedObject.FindProperty(name);
            }
            return conditionProperty;
        }

        private bool EvaluateBoolean(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            if (name.ToLower() == "true") return true;
            if (name.ToLower() == "false") return false;

            SerializedProperty prop = FindProperty(name);
            if (prop != null && prop.propertyType == SerializedPropertyType.Boolean)
            {
                return prop.boolValue;
            }
            
            UnityEngine.Object target = _rootProperty.serializedObject.targetObject;
            Debug.LogWarning($"[EnableIf] Boolean variable '{name}' not found in object {target.name}.");
            return false;
        }

        private bool EvaluateComparison(string fieldName, string valueString, CompareOp op)
        {
            SerializedProperty prop = FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogWarning($"[EnableIf] Variable '{fieldName}' not found for comparison.");
                return false;
            }

            // Numeric comparisons
            if (prop.propertyType == SerializedPropertyType.Integer)
            {
                if (int.TryParse(valueString, out int rightInt))
                {
                    int leftInt = prop.intValue;
                    switch (op)
                    {
                        case CompareOp.Equal: return leftInt == rightInt;
                        case CompareOp.NotEqual: return leftInt != rightInt;
                        case CompareOp.Less: return leftInt < rightInt;
                        case CompareOp.Greater: return leftInt > rightInt;
                        case CompareOp.LessOrEqual: return leftInt <= rightInt;
                        case CompareOp.GreaterOrEqual: return leftInt >= rightInt;
                    }
                }
            }
            else if (prop.propertyType == SerializedPropertyType.Float)
            {
                if (float.TryParse(valueString, NumberStyles.Float, CultureInfo.InvariantCulture, out float rightFloat))
                {
                    float leftFloat = prop.floatValue;
                    switch (op)
                    {
                        case CompareOp.Equal: return Mathf.Approximately(leftFloat, rightFloat);
                        case CompareOp.NotEqual: return !Mathf.Approximately(leftFloat, rightFloat);
                        case CompareOp.Less: return leftFloat < rightFloat;
                        case CompareOp.Greater: return leftFloat > rightFloat;
                        case CompareOp.LessOrEqual: return leftFloat <= rightFloat;
                        case CompareOp.GreaterOrEqual: return leftFloat >= rightFloat;
                    }
                }
            }
            // Equality-only comparisons (Enum, String, Bool)
            else if (op == CompareOp.Equal || op == CompareOp.NotEqual)
            {
                bool match = false;

                if (prop.propertyType == SerializedPropertyType.Enum)
                {
                    int dotIndex = valueString.LastIndexOf('.');
                    if (dotIndex >= 0) valueString = valueString.Substring(dotIndex + 1);

                    if (prop.enumValueIndex >= 0 && prop.enumValueIndex < prop.enumNames.Length)
                    {
                        string currentEnumName = prop.enumNames[prop.enumValueIndex];
                        match = (currentEnumName == valueString);
                    }
                }
                else if (prop.propertyType == SerializedPropertyType.String)
                {
                    match = (prop.stringValue == valueString);
                }
                else if (prop.propertyType == SerializedPropertyType.Boolean)
                {
                    if (bool.TryParse(valueString, out bool rightBool))
                        match = (prop.boolValue == rightBool);
                }
                else
                {
                    Debug.LogWarning($"[EnableIf] Property type of '{fieldName}' is not supported for comparisons.");
                    return false;
                }

                return op == CompareOp.Equal ? match : !match;
            }
            else
            {
                Debug.LogWarning($"[EnableIf] Operator '{op}' is only supported for numeric types. Field '{fieldName}'.");
            }

            return false;
        }
    }
}
