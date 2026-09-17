using System;
using UnityEngine;

[DefaultExecutionOrder(-10)]
public class PressETextController : SceneLocalSingleton<PressETextController>
{
    private ArenaController _currentArenaToFinish;
    private UtilityStructures.BoolCounter _isActive;
    private UtilityStructures.BoolCounter _isHidden;
    
    
    private void Start()
    {
        gameObject.SetActive(false);
    }
    
    private void ChangeSetActive()
    {
        gameObject.SetActive(_isActive.Value && !_isHidden.Value);
    }
    
    public static void Activate(ArenaController arenaController)
    {
        Instance._currentArenaToFinish = arenaController;
        Instance._isActive.Set();
        Instance.ChangeSetActive();
    }

    public static void Deactivate()
    {
        Instance._isActive.Unset();
        Instance?.ChangeSetActive();
    }
    
    public static void Hide()
    {
        Instance._isHidden.Set();
        Instance.ChangeSetActive();
    }

    public static void StopHiding()
    {
        Instance._isHidden.Unset();
        Instance.ChangeSetActive();
    }

    private void OnEnable()
    {
        GlobalGameInputManager.Instance.OnInteractEvent += OnEvent;
        ExplanationTextController.Hide();
    }

    private void OnDisable()
    {
        GlobalGameInputManager.Instance.OnInteractEvent -= OnEvent;
        ExplanationTextController.StopHiding();
    }

    private void OnEvent()
    {
        if (_isActive.Value)
        {
            _currentArenaToFinish.TryFinishArenaFromTrigger();
        }
    }
}
