using System;
using UnityEngine;
using System.Collections.Generic;
using CustomAttributes;

[DefaultExecutionOrder(-10)]
public class ArenaController : MonoBehaviour
{
    public enum ArenaFinishEnum
    {
        Enemies, Trigger,EnemiesAndTrigger
    }
    
    [Header("Flags")]
    [SerializeField] private ArenaFinishEnum finishCondition = ArenaFinishEnum.Trigger;
    [SerializeField] private bool canRegenerate = true;
    [Header("Finish")] 
    [SerializeField, EnableIf("finishCondition == Enemies")] private ArenaController nextArena;
    [SerializeField] private List<DoorController> doorsToOpenOnFinish = new();
    [Header("Resetting Arena")]
    [SerializeField] private string playerSpawnPointTag = "Respawn";
    [SerializeField] private List<RouteController> routesToRestart = new();
    [Header("Signs")] 
    [SerializeField] private List<TwoActiveFlipper> signs = new();
    [SerializeField] private bool setActiveFalseWhenArenaNotActive = true;

    public bool IsActive { get; private set; } = false;
    public bool CanRegenerate => canRegenerate;
    public bool IsArenaWithEnemies => finishCondition != ArenaFinishEnum.Trigger;
    public bool IsStillHaveEnemies => IsArenaWithEnemies && _aliveEnemies.Value;
    
    
    private Transform _playerSpawnPoint;
    private readonly List<Vector3> _enemiesSpawnPositions=new();
    private readonly List<Quaternion> _enemiesSpawnRotations=new();
    private readonly List<EnemyHealthController> _enemiesHealth=new();
    private readonly List<EnemyAI> _enemyAis = new();
    private HealthController _playerHealth;
    private PlayerManager _playerManager;
    
    
    private UtilityStructures.BoolCounter _aliveEnemies;
    private UtilityStructures.BoolCounter _triggerEntered;
    private UtilityStructures.BoolCounter _buttonTriggerEntered;
    private ArenaController _lastTriggerNextArena;
    private bool _lastHaveNextArena = false;

    private UtilityStructures.ToggleableState _pressETextActivator;
    private UtilityStructures.ToggleableState _pressETextHider = new(PressETextController.Hide, PressETextController.StopHiding);
    

    private void Awake()
    {
        _pressETextActivator = new (()=>PressETextController.Activate(this), PressETextController.Deactivate);
        
        GatherEnemies();
        
        var foundSpawnPoint = false;
        foreach (Transform child in transform)
        {
            if (child.gameObject.CompareTag(playerSpawnPointTag))
            {
                foundSpawnPoint = true;
                _playerSpawnPoint=child;
            }
            else if (child.TryGetComponent(out ArenaFinishTriggerController finishController))
            {
                finishController.SetArenaController(this);
            }
            else if (child.TryGetComponent(out DoorController doorController))
            {
                doorsToOpenOnFinish.Add(doorController);
            }
            else if (child.TryGetComponent(out RouteController routeController))
            {
                routesToRestart.Add(routeController);
            }
        }

        if (!foundSpawnPoint)
        {
            _playerSpawnPoint = UtilityFunctions.MakeEmptyObject("Player spawn point", transform);
            _playerSpawnPoint.tag = playerSpawnPointTag;
        }
        
        
        _playerHealth = PlayerManager.Player.GetComponent<HealthController>();
        _playerManager = PlayerManager.Player.GetComponent<PlayerManager>();

        if (setActiveFalseWhenArenaNotActive)
        {
            SetActiveSigns(false);
        }
    }


    private void SetActiveSigns(bool isActive)
    {
        foreach (var sign in signs)
        {
            sign.gameObject.SetActive(isActive);
        }
    }
    
    private void SetOpenSigns(bool isOpen)
    {
        foreach (var sign in signs)
        {
            sign.SetOpen(isOpen);
        }
    }
    
    private void GatherEnemies()
    {
        var children = GetComponentsInChildren<EnemyHealthController>();
        foreach (var child in children)
        {
            _enemiesSpawnPositions.Add(child.transform.position);
            _enemiesSpawnRotations.Add(child.transform.rotation);
            child.SetArenaController(this);
            _enemiesHealth.Add(child);
            _enemyAis.Add(child.GetComponent<EnemyAI>());
        }
        _aliveEnemies = _enemiesHealth.Count;
    }
    
    private void RestartRoutes()
    {
        foreach (var route in routesToRestart)
        {
            route.Restart();
        }
    }
    
    public void ReturnPlayerToSpawnPoint()
    {
        PlayerManager.Player.transform.position = _playerSpawnPoint.position;
        PlayerManager.Player.transform.rotation = _playerSpawnPoint.rotation;
        Physics.SyncTransforms();
        var newYaw = Vector3.SignedAngle(Vector3.forward, _playerSpawnPoint.forward, Vector3.up);
        GlobalLookDirectionManager.SetNewYaw(newYaw);
        _playerManager.ResetEverythingForTeleport();
        RestartRoutes();
    }

    public void ActivateArena()
    {
        if (!IsActive)
        {
            IsActive = true;
            
            if (setActiveFalseWhenArenaNotActive)
            {
                SetActiveSigns(true);
            }
        }
        
        foreach (var enemyAi in _enemyAis)
        {
            enemyAi.Activate();
        }
        foreach (var door in doorsToOpenOnFinish)
        {
            door.SetOpen(false);
        }

        if (_aliveEnemies.Value)
        {
            _pressETextHider.Activate();
        }
    }

    public void DeactivateArena()
    {
        if (IsActive)
        {
            IsActive = false;
            
            if (setActiveFalseWhenArenaNotActive)
            {
                SetActiveSigns(false);
            }
        }
        
        foreach (var enemyAi in _enemyAis)
        {
            enemyAi.Deactivate();
        }

        foreach (var door in doorsToOpenOnFinish)
        {
            door.SetOpen(true);
        }
        
        _pressETextActivator.Deactivate();
        _pressETextHider.Deactivate();
    }

    public void SetArenaFinishedOnStart()
    {
        IsActive = false;

        SetOpenSigns(true);
        _pressETextHider.Deactivate();
        
        if (setActiveFalseWhenArenaNotActive)
        {
            SetActiveSigns(false);
        }
        
        foreach (var enemyAi in _enemyAis)
        {
            enemyAi.Deactivate();
            enemyAi.gameObject.SetActive(false);
        }

        foreach (var door in doorsToOpenOnFinish)
        {
            door.SetOpenSkipAnimation(true);
        }
    }

    public void ResetArena()
    {
        for(var i=0;i<_enemiesHealth.Count;i++)
        {
            _enemiesHealth[i].gameObject.SetActive(true);
            _enemiesHealth[i].transform.position = _enemiesSpawnPositions[i];
            _enemiesHealth[i].transform.rotation = _enemiesSpawnRotations[i];
            _enemiesHealth[i].OnArenaReset();
        }
        ActivateArena();
        _playerHealth.OnArenaReset();
        ReturnPlayerToSpawnPoint();
    }
    
    public void EnemyRevived()
    {
        _aliveEnemies.Set();
        SetOpenSigns(false);
        
        if(IsActive)
            _pressETextHider.Activate();
    }

    public void EnemyDied()
    {
        _aliveEnemies.Unset();
        
        if (!IsActive) return;
        
        if (!_aliveEnemies.Value)
        {
            if (finishCondition == ArenaFinishEnum.Enemies)
            {
                FinishArena(true, nextArena);
            }
            else if (finishCondition == ArenaFinishEnum.EnemiesAndTrigger && 
                     _triggerEntered.Value &&
                     !_buttonTriggerEntered.Value)
            {
                FinishArena(_lastHaveNextArena, _lastTriggerNextArena);
            }
            
            _pressETextHider.Deactivate();
            SetOpenSigns(true);
        }
    }
    
    public void PlayerEnteredTrigger(bool lastHaveNextArena, ArenaController next, bool finishOnButtonPress)
    {
        _triggerEntered.Set();
        _lastTriggerNextArena = next;
        _lastHaveNextArena = lastHaveNextArena;

        if (finishOnButtonPress)
        {
            _buttonTriggerEntered.Set();
            
            if(IsActive)
                _pressETextActivator.Activate();
        }
        else
        {
            TryFinishArenaFromTrigger();
        }
    }
    
    public void PlayerExitedTrigger(bool finishOnButtonPress)
    {
        _triggerEntered.Unset();
        
        if (finishOnButtonPress)
        {
            _buttonTriggerEntered.Unset();
            if (!_buttonTriggerEntered.Value)
            {
                _pressETextActivator.Deactivate();
            }
        }
    }
    
    public void TryFinishArenaFromTrigger()
    {
        if (IsActive && (
                finishCondition == ArenaFinishEnum.Trigger ||
                (
                    finishCondition == ArenaFinishEnum.EnemiesAndTrigger && !_aliveEnemies.Value
                )
            )
           )
        {
            FinishArena(_lastHaveNextArena, _lastTriggerNextArena);
        }
    }

    private void FinishArena(bool lastHaveNextArena, ArenaController next)
    {
        _pressETextActivator.Deactivate();
        if (lastHaveNextArena && !ReferenceEquals(next, null))
            LevelController.ChangeArena(next);
        else
            LevelController.FinishLevel();
    }


}
