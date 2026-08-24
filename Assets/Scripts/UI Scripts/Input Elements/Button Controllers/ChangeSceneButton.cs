using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using TMPro;

public class ChangeSceneButton : GenericButtonController
{
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private string sceneName;
    

    protected override UnityAction FunctionToCall => LoadTargetScene;

    private void Awake()
    {
        if (sceneName == null || string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError($"[LevelEnterController] Missing scene name");
            return;
        }
        
        if (text != null)
        {
            text.text = sceneName;
        }
    }

    private void LoadTargetScene()
    {
        if (string.IsNullOrEmpty(sceneName)) return;

        LoadingScreenController.LoadScene(sceneName);
    }  
}