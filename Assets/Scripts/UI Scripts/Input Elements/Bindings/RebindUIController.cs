using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class RebindUIController : GenericButtonController
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI buttonText;

    [Header("Auto-Generated Data")]
    [SerializeField] private InputActionAsset inputAsset;
    [SerializeField] private string actionMapName;
    [SerializeField] private string actionName;
    [SerializeField] private int bindingIndex;

    private InputAction _action;
    private InputActionRebindingExtensions.RebindingOperation _rebindingOperation;

    private string SettingsKey => $"bind_{_action.actionMap.name}/{_action.name}_{bindingIndex}";
    protected override UnityEngine.Events.UnityAction FunctionToCall => StartRebinding;

    private bool _initialized = false;

    
    public void SetupInEditor(InputActionAsset asset, string mapName, string actionName, int index)
    {
        inputAsset = asset;
        actionMapName = mapName;
        this.actionName = actionName;
        bindingIndex = index;
    }

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

    private void StartRebinding()
    {
        buttonText.text = "Press button...";
        _action.Disable();

        _rebindingOperation = _action.PerformInteractiveRebinding(bindingIndex)
            .WithControlsExcluding("Mouse/position")
            .WithControlsExcluding("Mouse/delta")
            .OnComplete(_ => CompleteRebind())
            .OnCancel(_ => CancelRebind())
            .Start();
    }

    private void CompleteRebind()
    {
        _rebindingOperation.Dispose();
        _action.Enable();

        string newPath = _action.bindings[bindingIndex].effectivePath;
        string actionFullName = $"{_action.actionMap.name}/{_action.name}";

        var bindSetting = GameSettings.Get<BindSettingsValue>(SettingsKey);

        if (bindSetting == null)
        {
            bindSetting = new BindSettingsValue(actionFullName, bindingIndex, newPath);
            GameSettings.SetValue(SettingsKey, bindSetting);
        }
        else
        {
            bindSetting.Set(actionFullName, bindingIndex, newPath);
        }

        GameSettings.SaveSettings();

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
        var bindSetting = GameSettings.Get<BindSettingsValue>(SettingsKey);

        string displayPath = (bindSetting != null && !string.IsNullOrEmpty(bindSetting.OverridePath))
            ? bindSetting.OverridePath
            : _action.bindings[bindingIndex].effectivePath;

        var displayString = InputControlPath.ToHumanReadableString(
            displayPath,
            InputControlPath.HumanReadableStringOptions.OmitDevice);

        buttonText.text = displayString;
    }
}