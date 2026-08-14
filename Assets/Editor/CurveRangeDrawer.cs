using UnityEditor;
using UnityEngine;
using CustomAttributes;

[CustomPropertyDrawer(typeof(CurveRangeAttribute))]
public class CurveRangeDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var rangeAttribute = (CurveRangeAttribute)attribute;

        if (property.propertyType == SerializedPropertyType.AnimationCurve)
        {
            EditorGUI.CurveField(position, property, Color.green, rangeAttribute.Bounds, label);
        }
        else
        {
            EditorGUI.LabelField(position, label.text, "Use [CurveRange] only with AnimationCurve");
        }
    }
}