using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.IO;
using System;

[DefaultExecutionOrder(-300)]
public class GameSettings : MonoBehaviour
{
    [SerializeField] private string versionName = "V";
    private static GameSettings  _instance;

    private readonly Dictionary<string, ISaveValue> _values = new();

    private string _savePath;
    
    private void Awake()
    {
        if (_instance == null) { _instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); return; }

        _savePath = ISaveValue.GetSavePath(versionName+"_ControlSettings.yaml");
        
        LoadValuesInternal();
    }

    public static T Get<T>(string key) where T : class, ISaveValue
    {
        if (_instance == null)
        {
            Debug.LogError("[Settings] No instance found!");
            return null;
        }

        if (_instance._values.TryGetValue(key, out var value))
        {
            return value as T;
        }
        return null;
    }
    
    public static void SetValue(string key, ISaveValue value)
    {
        if (_instance == null) return;
        _instance._values[key] = value;
    }

    public static void LoadValues()
    {
        _instance?.LoadValuesInternal();
        if(GlobalGameInputManager.Instance!=null)
            ApplyAllBindings(GlobalGameInputManager.Instance.PlayerInput);
    }

    public static void ResetToBaseSettings()
    {
        _instance?.ResetToBaseSettingsInternal();
        if(GlobalGameInputManager.Instance!=null)
            ApplyAllBindings(GlobalGameInputManager.Instance.PlayerInput);
    }

    public static void SaveSettings() => _instance?.SaveInternal();

    public static void ApplyAllBindings(PlayerInput playerInput) => _instance?.ApplyAllBindingsInternal(playerInput);
    
    private void SaveInternal()
    {
        using var writer = new StreamWriter(_savePath);
        foreach (var kvp in _values)
        {
            writer.WriteLine($"{kvp.Key}: {kvp.Value.GetType().Name}|{kvp.Value.Get()}");
        }
    }

    private void ResetToBaseSettingsInternal()
    {
        _values.Clear();
        _values.Add("mouseSensX", new FloatSettingsValue(1f));
        _values.Add("mouseSensY", new FloatSettingsValue(1f));
        _values.Add("mouseSensAim", new FloatSettingsValue(1f));
    }
    
    private void LoadValuesInternal()
    {
        _values.Clear();
        _values.Add("mouseSensX", new FloatSettingsValue(1f));
        _values.Add("mouseSensY", new FloatSettingsValue(1f));
        _values.Add("mouseSensAim", new FloatSettingsValue(1f));
        
        
        if (!File.Exists(_savePath)) return;

        string[] lines = File.ReadAllLines(_savePath);
        foreach (var line in lines)
        {
            var split = line.Split(new[] { ": " }, 2, System.StringSplitOptions.RemoveEmptyEntries);
            if (split.Length != 2) continue;

            string key = split[0];
            string rawData = split[1];

            var typeAndData = rawData.Split(new[] { '|' }, 2);
            if (typeAndData.Length != 2) continue;

            string typeName = typeAndData[0];
            string data = typeAndData[1];

            ISaveValue setting = CreateSettingInstance(typeName);
            if (setting != null)
            {
                setting.Set(data);
                _values[key] = setting;
            }
        }
    }
    
    
    
    private ISaveValue CreateSettingInstance(string typeName)
    {
        return typeName switch
        {
            nameof(FloatSettingsValue) => new FloatSettingsValue(1f),
            nameof(BindSettingsValue) => new BindSettingsValue(),
            _ => null
        };
    }

    private void ApplyAllBindingsInternal(PlayerInput playerInput)
    {
        if (playerInput == null) return;

        playerInput.actions.RemoveAllBindingOverrides();
        foreach (var kvp in _values)
        {
            if (kvp.Value is BindSettingsValue bindValue)
            {
                if (string.IsNullOrEmpty(bindValue.OverridePath)) continue;

                var action = playerInput.actions.FindAction(bindValue.ActionName);
                if (action != null)
                {
                    action.ApplyBindingOverride(bindValue.BindingIndex, bindValue.OverridePath);
                }
            }
        }
    }
}