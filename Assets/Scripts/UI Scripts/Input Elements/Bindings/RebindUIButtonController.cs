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

        _initialized = true;
        UpdateUI();
    }

    private void OnEnable()
    {
        UpdateUI();
        GameSettings.OnBindingsReset += UpdateUI;
    }

    private void OnDisable()
    {
        GameSettings.OnBindingsReset -= UpdateUI;
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
        if (!_initialized || _action == null) return;
        
        var displayString = GetEnglishBindingDisplayString(_action,bindIndex);
        

        buttonText.text = string.IsNullOrEmpty(displayString) ? EmptyBindingText : displayString;
    }
    
    private static string GetEnglishBindingDisplayString(InputAction action, int bindingIndex)
    {
        if (bindingIndex < 0 || bindingIndex >= action.bindings.Count)
            return string.Empty;

        string path = action.bindings[bindingIndex].effectivePath;

        return InputControlPath.ToHumanReadableString(
            path, 
            InputControlPath.HumanReadableStringOptions.OmitDevice
        );
    }
}