using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "LevelsNameHolder", menuName = "Level Management/LevelsNameHolder")]
public class LevelsInitializersHolder : ScriptableObject
{
    [SerializeField] public LevelInitializer[] levels;

#if UNITY_EDITOR
    public event Action OnHolderValidate;

    private void OnValidate()
    {
        for (int i = 0; i < levels.Length; i++)
        {
            levels[i].UpdateSceneName();
        }

        OnHolderValidate?.Invoke();
    }
#endif
}

[Serializable]
public struct LevelInitializer
{
#if UNITY_EDITOR
    [SerializeField] private SceneAsset sceneAsset;
#endif

    [HideInInspector]
    [SerializeField] private string sceneName;

    [SerializeField] public bool availableOnStart;

    public string SceneName => sceneName;

#if UNITY_EDITOR
    public void UpdateSceneName()
    {
        if (sceneAsset != null)
        {
            sceneName = sceneAsset.name;
        }
    }
#endif
}


