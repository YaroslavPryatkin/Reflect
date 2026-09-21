using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class BindingsScrollViewCreator : MonoBehaviour
{
#if UNITY_EDITOR
    [Header("Input System References")]
    [SerializeField] private InputActionAsset inputAsset;
    [SerializeField] private string actionMapName = "Player";
    [SerializeField] private string controlScheme = "Keyboard&Mouse";

    [Header("UI Generation")] 
    [SerializeField] private BindingSettingPrefabController prefab;
    [SerializeField] private Transform contentContainer;
    [SerializeField] private List<string> excludeNames=new();
    

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
            if (excludeNames.Contains(action.name)) 
                continue;

            bool isComposite = false;
            for (int i = 0; i < action.bindings.Count; i++)
            {
                if (action.bindings[i].isComposite)
                {
                    isComposite = true;
                    break;
                }
            }

            if (isComposite)
            {
                ProcessCompositeAction(action);
            }
            else
            {
                ProcessSimpleAction(action);
            }
        }
        
        EditorUtility.SetDirty(inputAsset);
        AssetDatabase.SaveAssets();
        
        Debug.Log("<color=green>UI successfully generated!</color>");
        
        if (contentContainer.TryGetComponent(out CustomContentLayout contentLayout))
        {
            contentLayout.EditorUpdateLayout();
        }
    }

    
    
    private void ProcessSimpleAction(InputAction action)
    {
        var validBindingIndices = new List<int>();
        var referenceIndex = -1;

        for (int i = 0; i < action.bindings.Count; i++)
        {
            var binding = action.bindings[i];
            if (binding.isComposite || binding.isPartOfComposite) continue;

            if (referenceIndex == -1) referenceIndex = i;

            if (BelongsToControlScheme(action, i, controlScheme))
            {
                validBindingIndices.Add(i);
            }
        }

        if (referenceIndex == -1) return;

        var refBinding = action.bindings[referenceIndex];

        while (validBindingIndices.Count < 2)
        {
            action.AddBinding(new InputBinding
            {
                name = refBinding.name,
                path = "",
                interactions = refBinding.interactions,
                processors = refBinding.processors,
                groups = controlScheme
            });
            validBindingIndices.Add(action.bindings.Count - 1);
        }
        
        CreateUIInstance(action.name, action, validBindingIndices[0], validBindingIndices[1]);
    }

    private void ProcessCompositeAction(InputAction action)
    {
        int referenceHeaderIndex = -1;
        for (int i = 0; i < action.bindings.Count; i++)
        {
            if (action.bindings[i].isComposite)
            {
                referenceHeaderIndex = i;
                break;
            }
        }

        if (referenceHeaderIndex == -1) return;

        var validHeaders = GetCompositeHeaderIndices(action);

        while (validHeaders.Count < 2)
        {
            CloneComposite(action, referenceHeaderIndex);
            validHeaders = GetCompositeHeaderIndices(action);
        }

        int header1 = validHeaders[0];
        int header2 = validHeaders[1];

        var parts1 = GetCompositeParts(action, header1);
        var parts2 = GetCompositeParts(action, header2);

        foreach (var part1 in parts1)
        {
            int s1 = part1.index;
            int s2 = -1;

            foreach (var part2 in parts2)
            {
                if (part2.partName.Equals(part1.partName, System.StringComparison.OrdinalIgnoreCase))
                {
                    s2 = part2.index;
                    break;
                }
            }

            string displayName = $"{action.name} - {CapitalizeFirstLetter(part1.partName)}";
            CreateUIInstance(displayName, action, s1, s2);
        }
    }

    private bool BelongsToControlScheme(InputAction action, int bindingIndex, string scheme)
    {
        if (string.IsNullOrEmpty(scheme)) return true;

        var header = action.bindings[bindingIndex];
        if (header.groups.Contains(scheme)) return true;

        if (!header.isComposite) return false;
        
        var hasAnyGroupsInParts = false;

        for (var i = bindingIndex + 1; i < action.bindings.Count; i++)
        {
            var binding = action.bindings[i];
            if (!binding.isPartOfComposite) break;

            if (!string.IsNullOrEmpty(binding.groups))
            {
                hasAnyGroupsInParts = true;
                if (binding.groups.Contains(scheme)) return true;
            }
        }

        return !hasAnyGroupsInParts;
    }
    
    
    private void CreateUIInstance(string displayName, InputAction action, int s1, int s2)
    {
        var instanceObj = (GameObject)PrefabUtility.InstantiatePrefab(prefab.gameObject, contentContainer);
        instanceObj.name = displayName + " binding";

        Undo.RegisterCreatedObjectUndo(instanceObj, "Generate Settings UI");

        var instance = instanceObj.GetComponent<BindingSettingPrefabController>();
        instance.SetupInEditor(displayName, inputAsset, actionMapName, action.name, s1, s2);

        EditorUtility.SetDirty(instanceObj);
    }
    
    private void CloneComposite(InputAction action, int originalHeaderIndex)
    {
        var originalHeader = action.bindings[originalHeaderIndex];

        action.AddBinding(new InputBinding
        {
            name = originalHeader.name,
            path = originalHeader.path, 
            interactions = originalHeader.interactions,
            processors = originalHeader.processors,
            groups = "",
            isComposite = true
        });

        for (int i = originalHeaderIndex + 1; i < action.bindings.Count; i++)
        {
            var originalPart = action.bindings[i];
            
            if (!originalPart.isPartOfComposite) break;

            action.AddBinding(new InputBinding
            {
                name = originalPart.name,      
                path = "",                     
                interactions = originalPart.interactions,
                processors = originalPart.processors,
                groups = controlScheme,
                isPartOfComposite = true
            });
        }
    }
    
    private List<int> GetCompositeHeaderIndices(InputAction action)
    {
        var headers = new List<int>();
        for (var i = 0; i < action.bindings.Count; i++)
        {
            if (action.bindings[i].isComposite && BelongsToControlScheme(action, i, controlScheme))
            {
                headers.Add(i);
            }
        }
        return headers;
    }
    
    private List<(string partName, int index)> GetCompositeParts(InputAction action, int headerIndex)
    {
        var parts = new List<(string partName, int index)>();
        for (int i = headerIndex + 1; i < action.bindings.Count; i++)
        {
            var binding = action.bindings[i];
            if (!binding.isPartOfComposite) break;

            parts.Add((binding.name, i));
        }
        return parts;
    }
    
    private static string CapitalizeFirstLetter(string str)
    {
        if (string.IsNullOrEmpty(str)) return str;
        return char.ToUpper(str[0]) + (str.Length > 1 ? str.Substring(1) : "");
    }
    
#endif
}