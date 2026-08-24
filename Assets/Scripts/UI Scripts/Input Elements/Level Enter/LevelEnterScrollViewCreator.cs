using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif


[ExecuteAlways]
public class LevelEnterScrollViewCreator : MonoBehaviour
{
    [SerializeField] private LevelEnterController prefab;
    [SerializeField] private Transform contentContainer;
    [SerializeField] private LevelsInitializersHolder levelsInitializersHolder;
    
    
#if UNITY_EDITOR
    private void OnEnable()
    {
        if (Application.isPlaying)
        {
            EditorApplication.delayCall -= Rebuild;
            return;
        }
        
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
        EditorApplication.delayCall -= Rebuild;
    }

    private void OnValidate()
    {
        Unsubscribe();
        Subscribe();
        ScheduleRebuild();
    }

    private void Subscribe()
    {
        if (levelsInitializersHolder != null)
        {
            levelsInitializersHolder.OnHolderValidate -= ScheduleRebuild;
            levelsInitializersHolder.OnHolderValidate += ScheduleRebuild;
        }
    }

    private void Unsubscribe()
    {
        if (levelsInitializersHolder != null)
        {
            levelsInitializersHolder.OnHolderValidate -= ScheduleRebuild;
        }
    }

    private void ScheduleRebuild()
    {
        EditorApplication.delayCall -= Rebuild;
        
        if (Application.isPlaying)
        {
            return;
        }
        
        EditorApplication.delayCall += Rebuild;
    }

    private void Rebuild()
    {
        EditorApplication.delayCall -= Rebuild;
        if (this == null) return;
        GenerateUI();
    }
    
    [ContextMenu("Generate UI")]
    public void GenerateUI()
    {
        if (levelsInitializersHolder == null || prefab == null || contentContainer == null)
        {
            Debug.LogError("Fill all references!");
            return;
        }
        
        for (int i = contentContainer.childCount - 1; i >= 0; i--)
        {
            Undo.DestroyObjectImmediate(contentContainer.GetChild(i).gameObject);
        }

        for (var i = 0; i < levelsInitializersHolder.levels.Length; ++i)
        {
            var instanceObj = (GameObject)PrefabUtility.InstantiatePrefab(prefab.gameObject, contentContainer);
            instanceObj.name = "Enter " + levelsInitializersHolder.levels[i].SceneName;
            
            Undo.RegisterCreatedObjectUndo(instanceObj, "Generate Settings UI");

            var instance = instanceObj.GetComponent<LevelEnterController>();
            instance.Initialize(in levelsInitializersHolder.levels[i]);
                
            EditorUtility.SetDirty(instanceObj);
        }

        if (TryGetComponent(out CustomContentLayout contentLayout))
        {
            contentLayout.EditorUpdateLayout();
        }
    }
#endif
}