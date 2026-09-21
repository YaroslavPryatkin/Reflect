using UnityEngine;
using System.Collections.Generic;
using System.IO;

[CreateAssetMenu(fileName = "SettingsHolder", menuName = "SettingValues/SettingsHolder")]
public class SettingsHolder : ScriptableObject
{
    [SerializeField] private List<AbstractSettingValue> values = new();
    private readonly Dictionary<string, AbstractSettingValue> _valueDictionary = new();
    private string _savePath;

    public IReadOnlyList<AbstractSettingValue> Values => values;
    
    public string Initialize(string versionName)
    {
        _savePath = ISaveValue.GetSavePath(versionName + "_" + this.name + ".yaml");
        
        _valueDictionary.Clear();
        foreach (var value in values)
        {
            if (!_valueDictionary.TryAdd(value.name, value))
            {
                Debug.LogError($"Duplicate settings name: {value.name}");
            }
        }

        return this.name;
    }

    public void Save()
    {
        using var writer = new StreamWriter(_savePath);
        foreach (var value in values)
        {
            writer.WriteLine($"{value.name}:{value.Get()}");
        }
    }

    public void Load()
    {
        if (!File.Exists(_savePath)) return;

        string[] lines = File.ReadAllLines(_savePath);
        foreach (var line in lines)
        {
            var split = line.Split(new[] { ":" }, 2, System.StringSplitOptions.RemoveEmptyEntries);
            if (split.Length != 2) continue;

            var key = split[0];
            var data = split[1];

            if (_valueDictionary.TryGetValue(key, out var value))
            {
                value.Set(data);
            }
        }
    }

    public void Reset()
    {
        foreach (var value in values)
        {
            value.SettingReset();
        }
    }
}