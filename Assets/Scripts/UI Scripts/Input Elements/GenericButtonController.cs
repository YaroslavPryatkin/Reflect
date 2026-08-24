using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public abstract class GenericButtonController :MonoBehaviour, ISceneLifecycleDependant
{
    private Button _button;

    protected abstract UnityAction FunctionToCall { get; }

    public void OnSceneLoad()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(FunctionToCall);
    }
    
    public void OnSceneUnload()
    {        
        if (_button != null)
        {
            _button.onClick.RemoveListener(FunctionToCall);
        }
    }
    
    public void SetInteractionState(bool state)
    {
        _button.interactable = state;
    }
}