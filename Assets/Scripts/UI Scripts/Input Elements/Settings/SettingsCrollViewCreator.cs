using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class SettingsCrollViewCreator : MonoBehaviour
{
#if UNITY_EDITOR

    [SerializeField] private SettingsHolder settingsHolder;
    
    
    [Header("UI Generation")] 
    [SerializeField] private Transform contentContainer;
    [SerializeField] private SaveSettingsButtonController saveButton;
    [SerializeField] private ResetSettingsButtonController resetButton;

    
    
    [Header("Prefabs")]
    [SerializeField] private FloatInputSlider floatPrefab;
    [SerializeField] private BoolInputPrefabController boolPrefab;
    
    [ContextMenu("Generate UI")]
    public void GenerateUI()
    {
        if (settingsHolder == null || contentContainer==null || floatPrefab==null || boolPrefab == null)
        {
            Debug.LogError("Fill all references!");
            return;
        }

        for (int i = contentContainer.childCount - 1; i >= 0; i--)
        {
            Undo.DestroyObjectImmediate(contentContainer.GetChild(i).gameObject);
        }

        if (saveButton != null)
        {
            saveButton.SetupInEditor(settingsHolder);
        }
        if (resetButton != null)
        {
            resetButton.SetupInEditor(settingsHolder);
        }

        foreach (var value in settingsHolder.Values)
        {
            GameObject instanceObj;
            if (value.GetType() == typeof(BoolSettingValue))
            {
                instanceObj = (GameObject)PrefabUtility.InstantiatePrefab(boolPrefab.gameObject, contentContainer);
                if(instanceObj.TryGetComponent(out BoolInputPrefabController controller))
                {
                    controller.SetupInEditor(value as BoolSettingValue);
                }
            }
            else if (value.GetType() == typeof(FloatSettingValue))
            {
                instanceObj = (GameObject)PrefabUtility.InstantiatePrefab(floatPrefab.gameObject, contentContainer);
                if(instanceObj.TryGetComponent(out FloatInputSlider controller))
                {
                    controller.SetupInEditor(value as FloatSettingValue);
                }
            }
            else 
                continue;

            instanceObj.name = value.name + " setting";
            Undo.RegisterCreatedObjectUndo(instanceObj, "Generate Settings UI");
            EditorUtility.SetDirty(instanceObj);
        }
        
        Debug.Log("<color=green>UI successfully generated!</color>");
        
        if (contentContainer.TryGetComponent(out CustomContentLayout contentLayout))
        {
            contentLayout.EditorUpdateLayout();
        }
    }
    
#endif
}