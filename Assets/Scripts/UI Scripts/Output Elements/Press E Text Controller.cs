using System;
using UnityEngine;

[DefaultExecutionOrder(-10)]
public class PressETextController : SceneLocalSingleton<PressETextController>
{
    private ArenaController _currentArenaToFinish;
    private void Start()
    {
        gameObject.SetActive(false);
    }

    private readonly UtilityClasses.MultipleBoolValue _isActive = new();
    
    public static void Activate(ArenaController arenaController)
    {
        Instance._currentArenaToFinish = arenaController;
        Instance._isActive.Set();
        Instance.gameObject.SetActive(true);
    }

    public static void Deactivate()
    {
        Instance._isActive.Unset();
        Instance.gameObject.SetActive(Instance._isActive.Value);
    }

    private void OnEnable()
    {
        GlobalGameInputManager.Instance.OnInteractEvent += OnEvent;
        ExplanationTextController.HideText();
    }

    private void OnDisable()
    {
        GlobalGameInputManager.Instance.OnInteractEvent -= OnEvent;
        ExplanationTextController.StopHidingText();
    }

    private void OnEvent()
    {
        if (_isActive.Value)
        {
            if(_currentArenaToFinish.IsActive)
                Deactivate();
            
            _currentArenaToFinish.TryFinishArenaFromTrigger();
        }
    }
}
