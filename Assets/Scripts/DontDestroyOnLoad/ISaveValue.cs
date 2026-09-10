using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.IO;
using System;

public interface ISaveValue
{
    public void Set(string value);
    public string Get();
    
    public static string GetSavePath(string fileName)
    {
        string savesFolderPath;

        try
        {
            var exeFolderPath = Directory.GetParent(Application.dataPath).FullName;
            savesFolderPath = Path.Combine(exeFolderPath, "Saves");

            if (!Directory.Exists(savesFolderPath))
            {
                Directory.CreateDirectory(savesFolderPath);
            }

            var testFilePath = Path.Combine(savesFolderPath, ".permission_test");
            File.WriteAllText(testFilePath, "test");
            File.Delete(testFilePath);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or System.Security.SecurityException)
        {
            savesFolderPath = Path.Combine(Application.persistentDataPath, "Saves");
        
            if (!Directory.Exists(savesFolderPath))
            {
                Directory.CreateDirectory(savesFolderPath);
            }
        }

        return Path.Combine(savesFolderPath, fileName);
    }
}