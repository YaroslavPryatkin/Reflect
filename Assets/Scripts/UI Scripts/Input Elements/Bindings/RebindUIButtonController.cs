using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class RebindUIButtonController : GenericButtonController
{

    [SerializeField] private TextMeshProUGUI buttonText;


    [SerializeField] private InputActionAsset inputAsset;
    [SerializeField] private string actionMapName;
    [SerializeField] private string actionName;
    [SerializeField] private int bindIndex;

    
    
    private const string EmptyBindingText = "None";
    
    private InputAction _action;
    private InputActionRebindingExtensions.RebindingOperation _rebindingOperation;

    protected override UnityEngine.Events.UnityAction FunctionToCall => StartRebinding;

    private bool _initialized = false;
    
    #if UNITY_EDITOR

    public void SetupInEditor(InputActionAsset asset, string mapName, string actionName, int bindIndex)
    {
        inputAsset = asset;
        actionMapName = mapName;
        this.actionName = actionName;
        this.bindIndex = bindIndex;
    }
    
    #endif
    
    private void Start()
    {
        if (inputAsset != null)
        {
            var map = inputAsset.FindActionMap(actionMapName);
            if (map != null) _action = map.FindAction(actionName); 
        }

        UpdateUI();
        _initialized = true;
    }

    private void OnEnable()
    {
        if(_initialized)
            UpdateUI();
    }

    private void OnDisable()
    {
        _rebindingOperation?.Dispose();
    }

    private void StartRebinding()
    {
        if (_action == null) return;

        buttonText.text = "...";
        _action.Disable();

        _rebindingOperation = _action.PerformInteractiveRebinding(bindIndex)
            .WithControlsExcluding("Mouse/position")
            .WithControlsExcluding("Mouse/delta")
            .WithCancelingThrough("<Keyboard>/backspace")
            .OnComplete(_ => CompleteRebind())
            .OnCancel(_ => CancelRebind())
            .Start();
    }

    private void CompleteRebind()
    {
        if (_action.bindings[bindIndex].overridePath == "<Keyboard>/delete")
        {
            _action.ApplyBindingOverride(bindIndex, "");
        }
        
        _rebindingOperation.Dispose();
        _action.Enable(); 
        UpdateUI();
    }

    private void CancelRebind()
    {
        _rebindingOperation.Dispose();
        _action.Enable();
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (_action == null) return;
        var displayString = _action.GetBindingDisplayString(bindIndex, InputBinding.DisplayStringOptions.DontIncludeInteractions);
        

        buttonText.text = string.IsNullOrEmpty(displayString) ? EmptyBindingText : displayString;
    }
}