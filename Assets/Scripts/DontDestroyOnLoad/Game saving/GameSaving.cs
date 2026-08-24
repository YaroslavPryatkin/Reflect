using UnityEngine;
using System.Collections.Generic;
using System.IO;

[DefaultExecutionOrder(-300)]
public class GameSavings : MonoBehaviour
{
    private static GameSavings _instance;
    
    
    private readonly Dictionary<string, LevelSave> _levels = new();
    
    
    private static string FileName = "gameSave.yaml";
    
    public static LevelSave GetOrCreate(string key, bool isAvailable)
    {
        if (_instance == null)
        {
            Debug.LogError("[Settings] No instance found!");
            return null;
        }

        if (_instance._levels.TryGetValue(key, out var value))
        {
            return value;
        }

        var levelSave = new LevelSave();
        levelSave.IsAvailable = isAvailable;
        _instance._levels[key] = levelSave;
        return levelSave;
    }

    public static void EnsureLevelAvailable(string key)
    {
        if (_instance == null)
        {
            Debug.LogError("[Settings] No instance found!");
            return;
        }
        
        if (_instance._levels.TryGetValue(key, out var value))
        {
            value.IsAvailable = true;
            return;
        }
        
        var levelSave = new LevelSave();
        levelSave.IsAvailable = true;
        _instance._levels[key] = levelSave;
    }

    public static void ResetLevel(string key)
    {
        if (_instance == null)
        {
            Debug.LogError("[Settings] No instance found!");
            return;
        }
        
        if (_instance._levels.TryGetValue(key, out var value))
        {
            value.ResetLevelCompleteness();
        }
    }

    public static void IncreaseAmountOfDeaths(string key)
    {
        if (_instance == null)
        {
            Debug.LogError("[Settings] No instance found!");
            return;
        }
        
        if (_instance._levels.TryGetValue(key, out var value))
        {
            ++value.AmountOfDeaths;
        }
    }


    public static void Save() => _instance?.SaveInternal();
    
    private void Awake()
    {
        if (_instance == null) { _instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); return; }
        
        LoadInternal();
    }

    
    
    private void LoadInternal()
    {
        _levels.Clear();

        if (!File.Exists(ISaveValue.GetSavePath(FileName))) return;

        var lines = File.ReadAllLines(ISaveValue.GetSavePath(FileName));
        foreach (var line in lines)
        {
            var split = line.Split(new[] { ": " }, 2, System.StringSplitOptions.RemoveEmptyEntries);
            if (split.Length != 2) continue;

            var key = split[0];
            var data = split[1];

            if (_levels.TryGetValue(key, out var levelSave))
            {
                levelSave.Set(data);
            }
            else
            {
                var newLevelSave = new LevelSave();
                newLevelSave.Set(data);
                _levels.Add(key, newLevelSave);
            }
        }
    }
    
    private void SaveInternal()
    {
        using var writer = new StreamWriter(ISaveValue.GetSavePath(FileName));
        foreach (var kvp in _levels)
        {
            writer.WriteLine($"{kvp.Key}: {kvp.Value.Get()}");
        }
    }
}