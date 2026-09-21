using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.IO;
using System;

[DefaultExecutionOrder(-300)]
public class GameSettings : MonoBehaviour
{
    [SerializeField] private string versionName = "V";
    [SerializeReference] private List<SettingsHolder> settingHolders=new();
    [SerializeField] private InputActionAsset inputAsset;
    
    private static GameSettings  _instance;
    private readonly List<SettingsHolder> _goodHolders = new();

    private string _bindingSavePath;
    
    public static void SaveBindings()=> _instance.SaveBindingOverridesInternal();
    public static void ResetBindings() => _instance.ResetBindingOverridesInternal();
    public static Action OnBindingsReset;
    
    
    private void Awake()
    {
        if (_instance == null) { _instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); return; }

        if (inputAsset == null)
        {
            Debug.LogError("InputActionAsset is null");
            return;
        }
        
        _bindingSavePath = ISaveValue.GetSavePath(versionName + "_Bindings.json");
        
        var usedHolderNames = new HashSet<string>();
        
        foreach (var holder in settingHolders)
        {
            var holderName = holder.Initialize(versionName);
            if (usedHolderNames.Add(holderName))
            {
                _goodHolders.Add(holder);
            }
            else
            {
                Debug.LogError($"Duplicate settings holder name: {holderName}");
            }
        }

        foreach (var holder in _goodHolders)
        {
            holder.Load();
        }

        LoadBindingOverridesInternal();
    }
    
    private void SaveBindingOverridesInternal()
    {
        if (inputAsset == null || string.IsNullOrEmpty(_bindingSavePath)) return;

        var overridesJson = inputAsset.SaveBindingOverridesAsJson();
        
        File.WriteAllText(_bindingSavePath, overridesJson);
    }


    private void LoadBindingOverridesInternal()
    {
        if (inputAsset == null || string.IsNullOrEmpty(_bindingSavePath)) return;

        if (File.Exists(_bindingSavePath))
        {
            var overridesJson = File.ReadAllText(_bindingSavePath);
            inputAsset.LoadBindingOverridesFromJson(overridesJson);
        }
    }
    
    private void ResetBindingOverridesInternal()
    {
        if (inputAsset == null) return;
        inputAsset.RemoveAllBindingOverrides();
        OnBindingsReset?.Invoke();
    }
}