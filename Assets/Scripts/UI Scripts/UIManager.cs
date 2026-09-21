using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Object = System.Object;

[DefaultExecutionOrder(-170)]
public class UIManager : SceneLocalSingleton<UIManager>
{
    [SerializeField] private GameObject canvas;
    [SerializeField] private GameObject escapeMenu;
    [SerializeField] private GameObject inGameUI;
    [SerializeField] private GameObject deathScreen;
    [SerializeField] private GameObject winScreen;
    [SerializeField] private GameObject debugText;
    
    [Header("Other")]
    [SerializeField] private float deathAndWinScreenFadeInDuration = 1f;
    [SerializeField] private string mainMenuSceneName = "Main menu";
    
    private PlayerHealthController _playerHealthController;
    
    
    
    private enum StateEnum
    {
        Escape, Active, Death, Win
    }
    private readonly UtilityTimers.FractionBlockingValueTimerUnscaled<StateEnum> _state = 
        StateEnum.Escape;
    
    public static bool IsGameActive => Instance._state.Value == StateEnum.Active;
    public static bool IsDeathScreen => Instance._state.Value == StateEnum.Death;
    public static bool IsWinScreen => Instance._state.Value == StateEnum.Win;
    
    
    private bool _debugTextIsTaken = false;
    private TextMeshProUGUI _debugText;
    public static TextMeshProUGUI DebugText
    {
        get
        {
            if (Instance._debugTextIsTaken)
            {
                throw new InvalidOperationException("DebugText has already been taken");
            }
            
            Instance.debugText.gameObject.SetActive(true);
            Instance._debugTextIsTaken = true;
            return Instance._debugText;
        }
    }
    
    private void Awake()
    {
        _playerHealthController = PlayerManager.Player.GetComponent<PlayerHealthController>();
        
        if (!debugText.TryGetComponent(out _debugText))
        {
            Debug.LogError("No debug text component found on " + gameObject.name);
        }
        debugText.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        canvas.SetActive(true);
        GlobalGameInputManager.Instance.OnPauseEvent += EscapePressed;
    }

    private void Start()
    {
        _state.SetForce(StateEnum.Escape);
        SetState(StateEnum.Active);
    }

    private void OnDisable()
    {
        canvas.SetActive(false);
        GlobalGameInputManager.Instance.OnPauseEvent -= EscapePressed;
        
        SetState(StateEnum.Escape);
    }
    
    public void EscapePressed()
    {
        switch (_state.Value)
        {
            case StateEnum.Escape:
                SetState(StateEnum.Active);
                break;
            case StateEnum.Active:
                SetState(StateEnum.Escape);
                break;
            case StateEnum.Death:
                ResetPressedInternal();
                break;
            case StateEnum.Win:
                if (string.IsNullOrEmpty(mainMenuSceneName)) return;

                LoadingScreenController.LoadScene(mainMenuSceneName);
                break;
        }
    }

    public static void ContinuePressed() => Instance?.SetState(StateEnum.Active);
    
    public static void ResetPressed() => Instance?.ResetPressedInternal();
    
    public static void SkipArenaPressed() => Instance?.SkipArenaPressedInternal();
    
    public void ResetPressedInternal()
    {
        SetState(StateEnum.Active);
        LevelController.TryResetArena();
    }

    private void SkipArenaPressedInternal()
    {
        LevelController.TrySkipArena();
        if (_state.Value != StateEnum.Win)
        {
            ResetPressedInternal();
        }
    }

    public static void ShowDeath() => Instance?.SetState(StateEnum.Death);
    public static void ShowWin()=>Instance?.SetState(StateEnum.Win);
    
    private void SetState(StateEnum state)
    {
        //Debug.Log(_state.Value + " => " + state);
        if(_state.Value == state) return;
        
        switch (state)
        {
            case StateEnum.Active:
                escapeMenu.SetActive(false);
                inGameUI.SetActive(true);
                deathScreen.SetActive(false);
                winScreen.SetActive(false);
                _state.SetForce(StateEnum.Active);
                break;
            case StateEnum.Escape:
                escapeMenu.SetActive(true);
                inGameUI.SetActive(false);
                deathScreen.SetActive(false);
                winScreen.SetActive(false);
                _state.SetForce(StateEnum.Escape);
                break;
            case StateEnum.Death:
                escapeMenu.SetActive(false);
                inGameUI.SetActive(false);
                deathScreen.SetActive(true);
                winScreen.SetActive(false);
                _state.SetForce(StateEnum.Death, deathAndWinScreenFadeInDuration);
                break;
            case StateEnum.Win:
                escapeMenu.SetActive(false);
                inGameUI.SetActive(false);
                deathScreen.SetActive(false);
                winScreen.SetActive(true);
                _state.SetForce(StateEnum.Win, deathAndWinScreenFadeInDuration);
                break;
            default:                
                escapeMenu.SetActive(false);
                inGameUI.SetActive(false);
                deathScreen.SetActive(false);
                winScreen.SetActive(false);
                break;
        }

        GlobalGameInputManager.SwitchInputMap(state switch
        {
            StateEnum.Active => GlobalGameInputManager.InputMaps.Game,
            _ => GlobalGameInputManager.InputMaps.UI
        });

        switch (state)
        {
            case StateEnum.Escape:
                GlobalTimeScaleController.ChangeTimePace(this,0);
                break;
            default:
                GlobalTimeScaleController.ReturnTimePace(this);
                break;
        }
    }

    private void Update()
    {
        if (_state.Value is StateEnum.Death or StateEnum.Win)
        {
            GlobalTimeScaleController.ChangeTimePace(
                this, 1 - Mathf.Clamp01(_state.TimeFraction));
        }
    }


    

    
}
