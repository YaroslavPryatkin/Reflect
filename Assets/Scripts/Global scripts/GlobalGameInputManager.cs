using System;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-200 )]
public class GlobalGameInputManager : SceneLocalSingleton<GlobalGameInputManager>
{
    public event System.Action OnPauseEvent;
    
    public event System.Action<Vector2> OnMoveEvent;
    public event System.Action<Vector2> OnLookEvent;
    public event System.Action OnJumpEvent;
    public event System.Action OnInteractEvent;
    public event System.Action OnTargetLockEvent;
    public event System.Action OnDashPressEvent;
    public event System.Action OnDashReleaseEvent;
    public event System.Action OnSlidePressEvent;
    public event System.Action OnSlideReleaseEvent;
    public event System.Action OnAttackPressEvent;
    public event System.Action OnAttackReleaseEvent;
    public event System.Action OnParryPressEvent;
    public event System.Action OnParryReleaseEvent;
    public event System.Action OnAimPressEvent;
    public event System.Action OnAimReleaseEvent;
    
    
    [SerializeField] private string gameInputMapName = "Player";
    [SerializeField] private string uiInputMapName = "UI";
    
    public PlayerInput PlayerInput { get; private set; }
    private bool _firstTimeSwitchingMap = true;

    private void Awake()
    {
        PlayerInput = GetComponent<PlayerInput>();
        _firstTimeSwitchingMap = true;
        GameSettings.ApplyAllBindings(PlayerInput);
    }
    
    public void OnMove(InputAction.CallbackContext context) 
        => OnMoveEvent?.Invoke(context.ReadValue<Vector2>());
    
    public void OnLook(InputAction.CallbackContext context) 
        => OnLookEvent?.Invoke(context.ReadValue<Vector2>());

    public void OnJump(InputAction.CallbackContext context) 
    {
        if (context.started) 
            OnJumpEvent?.Invoke();
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        if(context.started)
            OnDashPressEvent?.Invoke();
        else if(context.canceled)
            OnDashReleaseEvent?.Invoke();
    }

    public void OnSlide(InputAction.CallbackContext context)
    {
        
        if (context.started)
        {
            OnSlidePressEvent?.Invoke();
        }
        else if (context.canceled)
        {
            OnSlideReleaseEvent?.Invoke();
        }
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            OnAttackPressEvent?.Invoke();
        }
        else if (context.canceled)
        {
            OnAttackReleaseEvent?.Invoke();
        }
    }

    public void OnPause(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            OnPauseEvent?.Invoke();
        }
    }

    public void OnParry(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            OnParryPressEvent?.Invoke();
        }
        else if (context.canceled)
        {
            OnParryReleaseEvent?.Invoke();
        }
    }
    
    public void OnAim(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            OnAimPressEvent?.Invoke();
        }
        else if (context.canceled)
        {
            OnAimReleaseEvent?.Invoke();
        }
    }

    public void OnTargetLock(InputAction.CallbackContext context)
    {
        if (context.started)
            OnTargetLockEvent?.Invoke();
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.started) 
            OnInteractEvent?.Invoke();
    }

    public enum InputMaps
    {
        UI, Game
    }
    
    public static void SwitchInputMap(InputMaps map) => Instance.SwitchInputMapInternal(map);
    
    private static void SetCursorLocked(bool locked){
        Cursor.visible = !locked;
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
    }
    
    private void SwitchInputMapInternal(InputMaps map)
    {
        if (_firstTimeSwitchingMap)
        {
            PlayerInput.SwitchCurrentActionMap(gameInputMapName);
            PlayerInput.currentActionMap.Disable();
            PlayerInput.SwitchCurrentActionMap(uiInputMapName);
            PlayerInput.currentActionMap.Disable();
            _firstTimeSwitchingMap = false;
        }

        SetCursorLocked(map switch
        {
            InputMaps.Game => true,
            _ => false
        });
        
        var mapName = map switch
        {
            InputMaps.Game => gameInputMapName,
            _ => uiInputMapName
        };
        if (PlayerInput.inputIsActive)
        {
            PlayerInput.currentActionMap.Disable();
            PlayerInput.SwitchCurrentActionMap(mapName);
            PlayerInput.currentActionMap.Enable();
        }
    }
}
