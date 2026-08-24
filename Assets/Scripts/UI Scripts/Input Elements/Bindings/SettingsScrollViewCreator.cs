using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class SettingsScrollViewCreator : MonoBehaviour
{
#if UNITY_EDITOR
    [Header("Input System References")]
    [SerializeField] private InputActionAsset inputAsset;
    [SerializeField] private string actionMapName = "Player";
    [SerializeField] private string controlScheme = "Keyboard&Mouse";

    [Header("UI Generation")]
    [SerializeField] private BindingSettingPrefabController prefab;
    [SerializeField] private Transform contentContainer;
    [SerializeField] private List<string> excludeNames;
    

    [ContextMenu("Generate UI")]
    public void GenerateUI()
    {
        if (inputAsset == null || prefab == null || contentContainer == null)
        {
            Debug.LogError("Fill all references!");
            return;
        }

        for (int i = contentContainer.childCount - 1; i >= 0; i--)
        {
            Undo.DestroyObjectImmediate(contentContainer.GetChild(i).gameObject);
        }

        var actionMap = inputAsset.FindActionMap(actionMapName);
        if (actionMap == null) return;
        
        foreach (var action in actionMap.actions)
        {
            for (int i = 0; i < action.bindings.Count; i++)
            {
                var binding = action.bindings[i];

                if (string.IsNullOrEmpty(binding.path) || binding.isComposite) continue;
                if (!BelongsToControlScheme(action, i, controlScheme)) continue;

                string displayName = GenerateDisplayName(action, binding);
                if(excludeNames.Contains(displayName))continue;
                
                var instanceObj = (GameObject)PrefabUtility.InstantiatePrefab(prefab.gameObject, contentContainer);
                instanceObj.name = displayName + " binding";
                
                Undo.RegisterCreatedObjectUndo(instanceObj, "Generate Settings UI");

                var instance = instanceObj.GetComponent<BindingSettingPrefabController>();
                instance.SetupInEditor(displayName, inputAsset, actionMapName, action.name, i);
                
                EditorUtility.SetDirty(instanceObj);
            }
        }
        
        Debug.Log("<color=green>UI successfully generated!</color>");
        
        if (TryGetComponent(out CustomContentLayout contentLayout))
        {
            contentLayout.EditorUpdateLayout();
        }
    }

    private bool BelongsToControlScheme(InputAction action, int bindingIndex, string scheme)
    {
        var binding = action.bindings[bindingIndex];
        if (binding.groups.Contains(scheme)) return true;

        if (binding.isPartOfComposite)
        {
            for (int i = bindingIndex - 1; i >= 0; i--)
            {
                if (action.bindings[i].isComposite)
                    return action.bindings[i].groups.Contains(scheme);
            }
        }
        return false;
    }

    private string GenerateDisplayName(InputAction action, InputBinding binding)
    {
        if (binding.isPartOfComposite)
            return $"{action.name}: {binding.name}";
        
        return action.name;
    }
#endif
}