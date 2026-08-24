using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.IO;

public interface ISaveValue
{
    public void Set(string value);
    public string Get();
    
    public static string GetSavePath(string fileName)
    {
        var exeFolderPath = Directory.GetParent(Application.dataPath).FullName;
        var savesFolderPath = Path.Combine(exeFolderPath, "Saves");
        if (!Directory.Exists(savesFolderPath))
        {
            Directory.CreateDirectory(savesFolderPath);
        }
        
        return Path.Combine(savesFolderPath,fileName);
    }
}