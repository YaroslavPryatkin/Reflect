using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelResetButton : GenericButtonController
{
    private string _sceneName;
    private LevelEnterController _controller;

    protected override UnityAction FunctionToCall => ResetLevel;

    public void Initialize(LevelEnterController controller, string sceneName)
    {
        _sceneName = sceneName;
        _controller = controller;
    }
    
    private void ResetLevel()
    {
        if (string.IsNullOrEmpty(_sceneName)) return;

        GameSavings.ResetLevel(_sceneName);
        GameSavings.Save();
        
        _controller.UpdateUI();
    }
        
}