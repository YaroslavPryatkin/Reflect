using System.Collections.Generic;

public class LevelSave : ISaveValue
{
    public bool IsAvailable=false;

    public bool IsFinished = false;

    public int AmountOfDeaths = 0;
    
    public int CurrentArenaIndex = -1;

    public readonly List<int> FinishedArenas = new();
    
    public void Set(string data)
    {
        var parts = data.Split(';');
        if (parts.Length >= 3)
        {
            if(bool.TryParse(parts[0], out var isAvailable))
                IsAvailable = isAvailable;
            if(bool.TryParse(parts[1], out var isFinished))
                IsFinished = isFinished;
            if (int.TryParse(parts[2], out var amountOfDeaths))
                AmountOfDeaths = amountOfDeaths;
            if (int.TryParse(parts[3], out var currentArenaIndex))
                CurrentArenaIndex = currentArenaIndex;
            
            FinishedArenas.Clear();
            for (var i = 4; i < parts.Length; i++)
            {
                if(int.TryParse(parts[i], out var index))
                    FinishedArenas.Add(index);
            }
        }
    }

    public void FinishCurrentArena(int nextArenaIndex)
    {
        FinishedArenas.Add(CurrentArenaIndex);
        CurrentArenaIndex = nextArenaIndex;
    }
    
    public string Get()
    {
        var res = IsAvailable + ";" + IsFinished + ";" + AmountOfDeaths + ";" + CurrentArenaIndex;
        
        foreach (var arena in FinishedArenas)
        {
            res+=";" + arena;
        }
        
        return res;
    }

    public void ResetLevelCompleteness()
    {
        IsFinished = false;
        AmountOfDeaths = 0;
        CurrentArenaIndex = -1;
        FinishedArenas.Clear();
    }
}