using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelEnterButton : GenericButtonController
{
    private string _sceneName;

    protected override UnityAction FunctionToCall => LoadTargetScene;

    public void Initialize(string sceneName)
    {
        _sceneName = sceneName;
    }

    private void LoadTargetScene()
    {
        if (string.IsNullOrEmpty(_sceneName)) return;

        LoadingScreenController.LoadScene(_sceneName);
    }
        
}