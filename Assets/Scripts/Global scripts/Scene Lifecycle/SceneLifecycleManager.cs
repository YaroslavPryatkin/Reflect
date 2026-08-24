using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.SceneManagement;
using System.Collections;

[DefaultExecutionOrder(-250)]
public class SceneLifecycleManager : MonoBehaviour
{
    private Scene _myScene;
    private readonly List<ISceneLifecycleDependant> _registeredDependants = new();

    private void Awake()
    {
        _myScene = gameObject.scene;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
        
        RegisterAllInScene();
    }

    private void Start()
    {
        LoadingScreenController.RemoveLoadScreen();
    }

    private void OnSceneUnloaded(Scene scene)
    {
        if (scene == _myScene)
        {
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            UnregisterAll(); 
        }
    }

    private void RegisterAllInScene()
    {
        UnregisterAll();

        var currentScene = gameObject.scene; 
        var rootObjects = currentScene.GetRootGameObjects();
        
        var allDependants = new List<ISceneLifecycleDependant>();

        foreach (var root in rootObjects)
        {
            var dependants = root.GetComponentsInChildren<ISceneLifecycleDependant>(true);
            allDependants.AddRange(dependants);
        }

        var sortedDependants = allDependants
            .OrderBy(GetExecutionOrder)
            .ToList();

        foreach (var dependant in sortedDependants)
        {
            dependant.OnSceneLoad();
            _registeredDependants.Add(dependant);
        }
    }

    private int GetExecutionOrder(ISceneLifecycleDependant dependant)
    {
        var type = dependant.GetType();
        var attribute = type.GetCustomAttribute<DefaultExecutionOrder>();

        return attribute?.order ?? 0;
    }
    
    public void UnregisterAll()
    {
        GlobalTimeScaleController.ReturnTimePaceAll();
        for (int i = _registeredDependants.Count - 1; i >= 0; i--)
        {
            var dependant = _registeredDependants[i];

            if (dependant is Object unityObj && unityObj != null)
            {
                dependant.OnSceneUnload();
            }
        }
        _registeredDependants.Clear();
    }
}