using System;
using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "MakeLevelAvailableHolder", menuName = "Level Management/MakeLevelAvailableHolder")]
public class MakeLevelAvailableHolder : ScriptableObject
{
#if UNITY_EDITOR
    [SerializeField] private List<SceneAsset> levels = new();
#endif

    [HideInInspector,SerializeField] public List<string> levelNames = new();
    
#if UNITY_EDITOR
    private void OnValidate()
    {
        levelNames.Clear();
        foreach (var level in levels)
        {
            levelNames.Add(level.name);
        }
    }
#endif
}