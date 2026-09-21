using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class LevelController :  SceneLocalSingleton<LevelController>
{
    [SerializeField] private ArenaController startArena;
    [SerializeField] private MakeLevelAvailableHolder makeLevelAvailableHolder;
    
    private readonly List<ArenaController> _arenas=new();
    private ArenaController _currentArena;
    private bool _haveCurrentArena;
    public static  bool CanRegenerateOnArena => !Instance._haveCurrentArena || Instance._currentArena.CanRegenerate;

    public static bool ShouldHoldSwordOnArena => Instance._haveCurrentArena && Instance._currentArena.IsStillHaveEnemies;

    public static void ChangeArena(ArenaController arena) => Instance.ChangeArenaInternal(arena);

    public static void FinishLevel() => Instance.FinishLevelInternal();
    
    public static void ResetLevel() => Instance.ResetLevelInternal();

    public static void TryResetArena() => Instance.TryResetArenaInternal();
    
    public static void TrySkipArena() => Instance.TrySkipArenaInternal();
    public static void TryReturnPlayerToSpawnPoint(bool isDeath) => Instance.TryReturnPlayerToSpawnPointInternal(isDeath);
    
    private void Awake()
    {
        _haveCurrentArena = false;
        
        _arenas.Clear();
        foreach(Transform child in transform)
        {
            if (child.TryGetComponent(out ArenaController arenaController))
            {
                _arenas.Add(arenaController);
            }
        }

        if (_arenas.Count == 0)
        {
            Debug.LogError("No arenas!");
            return;
        }
        
        if (startArena == null)
        {
            startArena = _arenas[0];
        }
        else if (!_arenas.Contains(startArena))
        {
            Debug.LogError("Start arena is not a child of level controller!");
            _arenas.Add(startArena);
        }


        AwakeLevel();
    }

    private void AwakeLevel()
    {
        var myScene = gameObject.scene.name;
        var myGameSave = GameSavings.GetOrCreate(myScene, true);

        var shouldSave = false;
        if (myGameSave.CurrentArenaIndex == -1)
        {
            myGameSave.CurrentArenaIndex = _arenas.IndexOf(startArena);
            shouldSave = true;
        }

        if (!myGameSave.IsAvailable)
        {
            myGameSave.IsAvailable = true;
            shouldSave = true;
        }
        
        if(shouldSave)
            GameSavings.Save();
        
        _currentArena = _arenas[myGameSave.CurrentArenaIndex];
        _haveCurrentArena = true;

        foreach (var finishedArenaIndex in myGameSave.FinishedArenas)
        {
            _arenas[finishedArenaIndex].SetArenaFinishedOnStart();
        }
    }
    
    private void Start()
    {
        if (_haveCurrentArena)
        {
            _currentArena.ResetArena();
        }
    }



    private void ChangeArenaInternal(ArenaController arenaController)
    {
        
        if (_haveCurrentArena)
        {
            _currentArena.DeactivateArena();
        }
        
        if (_arenas.Contains(arenaController))
        {
            _currentArena = arenaController;
            var myScene = gameObject.scene.name;
            var myGameSave = GameSavings.GetOrCreate(myScene, true);
            
            myGameSave.FinishCurrentArena(_arenas.IndexOf(_currentArena));
            
            GameSavings.Save();
            
            _currentArena.ActivateArena();
        }
    }

    private void FinishLevelInternal()
    {
        if (_haveCurrentArena)
        {
            var myScene = gameObject.scene.name;
            var myGameSave = GameSavings.GetOrCreate(myScene, true);
            myGameSave.IsFinished = true;
        }
        
        if (makeLevelAvailableHolder != null)
        {
            for (var i = 0; i < makeLevelAvailableHolder.levelNames.Count; ++i)
            {
                GameSavings.EnsureLevelAvailable(makeLevelAvailableHolder.levelNames[i]);
            }
        }

        GameSavings.Save();
        
        UIManager.ShowWin();
    }

    private void ResetLevelInternal()
    {
        var myScene = gameObject.scene.name;
        GameSavings.ResetLevel(myScene);
        GameSavings.Save();
        
        AwakeLevel();
        if (_haveCurrentArena)
        {
            _currentArena.ResetArena();
        }
    }
    
    private void TryResetArenaInternal()
    {
        if( _haveCurrentArena)
        {
            _currentArena.ResetArena();
        }
    }
    
    private void TrySkipArenaInternal()
    {
        if (_haveCurrentArena)
        {
            _currentArena.SkipArena();
        }
    }

    private void TryReturnPlayerToSpawnPointInternal(bool isDeath)
    {
        if (_haveCurrentArena && !_currentArena.IsArenaWithEnemies)
        {
            _currentArena.ReturnPlayerToSpawnPoint();
            if(isDeath)
                GameSavings.IncreaseAmountOfDeaths(gameObject.scene.name);
        }
    }
}