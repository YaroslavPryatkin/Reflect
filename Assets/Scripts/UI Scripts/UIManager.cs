using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DefaultExecutionOrder(-170)]
public class UIManager : MonoBehaviour
{
    public interface IInitializable
    {
        public void Initialize();
    }
    
    private static UIManager _instance;
    
    [SerializeField] private GameObject escapeMenu;
    [SerializeField] private GameObject inGameUI;
    [SerializeField] private GameObject deathScreen;
    [SerializeField] private GameObject debugText;
    [SerializeField] private float deathScreenFadeInDuration = 1f;
    
    private PlayerHealthController _playerHealthController;
    
    
    
    private enum StateEnum
    {
        Escape, Active, Death
    }
    private readonly UtilityClasses.FractionBlockingValueTimerUnscaled<StateEnum> _state = 
        StateEnum.Active;
    public static bool IsGameActive => _instance._state.Value == StateEnum.Active;
    public static bool IsDeathScreen => _instance._state.Value == StateEnum.Death;
    
    
    
    private bool _debugTextIsTaken = false;
    private TextMeshProUGUI _debugText;
    public static TextMeshProUGUI DebugText
    {
        get
        {
            if (_instance._debugTextIsTaken)
            {
                throw new InvalidOperationException("DebugText has already been taken");
            }
            
            _instance.debugText.gameObject.SetActive(true);
            _instance._debugTextIsTaken = true;
            return _instance._debugText;
        }
    }
    
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        var awakeAnyways = GetComponentsInChildren<IInitializable>(true);
        foreach (var awakeAnyway in awakeAnyways)
        {
            awakeAnyway.Initialize();
        }
        
        _playerHealthController = GlobalGameManager.Player.GetComponent<PlayerHealthController>();
        
        escapeMenu.GetComponent<EscapeMenuController>().SetSingleTone();
        deathScreen.GetComponent<DeathScreenController>().SetSingleTone();
        
        EscapeMenuController.ContinueButton.onClick.AddListener(() => SetState(StateEnum.Active));
        EscapeMenuController.ResetButton.onClick.AddListener(ResetPressed);
        EscapeMenuController.ExitButton.onClick.AddListener(CloseGame);
        
        DeathScreenController.ResetButton.onClick.AddListener(ResetPressed);
        DeathScreenController.ExitButton.onClick.AddListener(CloseGame);
        
        
        if (!debugText.TryGetComponent(out _debugText))
        {
            Debug.LogError("No debug text component found on " + gameObject.name);
        }
        debugText.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        GlobalGameInputManager.Instance.OnPauseEvent += EscapePressed;
    }

    private void OnDisable()
    {
        GlobalGameInputManager.Instance.OnPauseEvent -= EscapePressed;
    }

    private void Start()
    {
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
                ResetPressed();
                break;
        }
    }

    public void ResetPressed()
    {
        SetState(StateEnum.Active);
        _playerHealthController.TryResetArena();
    }

    public static void ShowDeath() => _instance.SetState(StateEnum.Death);
    
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
                _state.SetForce(StateEnum.Active);
                break;
            case StateEnum.Escape:
                escapeMenu.SetActive(true);
                inGameUI.SetActive(false);
                deathScreen.SetActive(false);
                _state.SetForce(StateEnum.Escape);
                break;
            case StateEnum.Death:
                escapeMenu.SetActive(false);
                inGameUI.SetActive(false);
                deathScreen.SetActive(true);
                _state.SetForce(StateEnum.Death, deathScreenFadeInDuration);
                break;
            default:                
                escapeMenu.SetActive(false);
                inGameUI.SetActive(false);
                deathScreen.SetActive(false);
                break;
        }

        switch (state)
        {
            case StateEnum.Active:
                GlobalGameInputManager.SwitchInputMap(GlobalGameInputManager.InputMaps.Game);
                SetCursorLocked(true);
                break;
            default:
                GlobalGameInputManager.SwitchInputMap(GlobalGameInputManager.InputMaps.UI);
                SetCursorLocked(false);
                break;
        }

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
        if (_state.Value == StateEnum.Death)
        {
            GlobalTimeScaleController.ChangeTimePace(
                this, 1 - Mathf.Clamp01(_state.TimeFraction));
        }
    }

    public void CloseGame()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
    
    private static void SetCursorLocked(bool locked){
        Cursor.visible = !locked;
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
    }
    
}
