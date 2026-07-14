using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using CustomAttributes;


[CustomPropertyDrawer(typeof(SelectSubclassAttribute))]
public class SelectSubclassDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.ManagedReference)
        {
            EditorGUI.PropertyField(position, property, label);
            return;
        }

        Type baseType = GetTypeFromManagedReferenceTypename(property.managedReferenceFieldTypename);
        if (baseType == null)
        {
            EditorGUI.PropertyField(position, property, label);
            return;
        }

        Rect labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight);
        Rect buttonRect = new Rect(position.x + EditorGUIUtility.labelWidth, position.y, position.width - EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight);

        EditorGUI.LabelField(labelRect, label);

        string currentTypeName = string.IsNullOrEmpty(property.managedReferenceFullTypename) 
            ? "Null (None)" 
            : property.managedReferenceFullTypename.Split(' ').Last().Split('.').Last();

        if (GUI.Button(buttonRect, currentTypeName, EditorStyles.popup))
        {
            GenericMenu menu = new GenericMenu();
            
            menu.AddItem(new GUIContent("Null (None)"), string.IsNullOrEmpty(property.managedReferenceFullTypename), () =>
            {
                property.managedReferenceValue = null;
                property.serializedObject.ApplyModifiedProperties();
            });

            var derivedTypes = GetDerivedTypes(baseType);
            foreach (var type in derivedTypes)
            {
                bool isSelected = property.managedReferenceFullTypename.Contains(type.FullName);
                menu.AddItem(new GUIContent(type.Name), isSelected, () =>
                {
                    property.managedReferenceValue = Activator.CreateInstance(type);
                    property.serializedObject.ApplyModifiedProperties();
                });
            }
            menu.ShowAsContext();
        }

        if (!string.IsNullOrEmpty(property.managedReferenceFullTypename))
        {
            EditorGUI.indentLevel++;
            
            SerializedProperty endProperty = property.GetEndProperty();
            SerializedProperty childProperty = property.Copy();
            childProperty.NextVisible(true);

            float currentY = position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            while (!SerializedProperty.EqualContents(childProperty, endProperty))
            {
                float height = EditorGUI.GetPropertyHeight(childProperty, true);
                Rect childRect = new Rect(position.x, currentY, position.width, height);
                
                EditorGUI.PropertyField(childRect, childProperty, true);
                currentY += height + EditorGUIUtility.standardVerticalSpacing;
                
                if (!childProperty.NextVisible(false))
                    break;
            }

            EditorGUI.indentLevel--;
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.ManagedReference || string.IsNullOrEmpty(property.managedReferenceFullTypename))
        {
            return EditorGUIUtility.singleLineHeight;
        }

        float height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        
        SerializedProperty endProperty = property.GetEndProperty();
        SerializedProperty childProperty = property.Copy();
        childProperty.NextVisible(true);

        while (!SerializedProperty.EqualContents(childProperty, endProperty))
        {
            height += EditorGUI.GetPropertyHeight(childProperty, true) + EditorGUIUtility.standardVerticalSpacing;
            if (!childProperty.NextVisible(false))
                break;
        }

        return height;
    }

    private Type GetTypeFromManagedReferenceTypename(string typename)
    {
        if (string.IsNullOrEmpty(typename)) return null;
        var parts = typename.Split(' ');
        return parts.Length < 2 ? null : Assembly.Load(parts[0]).GetType(parts[1]);
    }

    private IEnumerable<Type> GetDerivedTypes(Type baseType)
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => baseType.IsAssignableFrom(type) &&
                           !type.IsInterface &&
                           !type.IsAbstract &&
                           type.GetCustomAttribute<VisibleSubclassAttribute>() != null);

    }
}