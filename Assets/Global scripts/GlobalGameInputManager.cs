using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-200 )]
public class GlobalGameInputManager : MonoBehaviour
{
    public static GlobalGameInputManager Instance;

    public event System.Action OnPauseEvent;
    
    public event System.Action<Vector2> OnMoveEvent;
    public event System.Action<Vector2> OnLookEvent;
    public event System.Action OnJumpEvent;
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

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void OnMove(InputAction.CallbackContext context) 
        => OnMoveEvent?.Invoke(context.ReadValue<Vector2>());
    
    public void OnLook(InputAction.CallbackContext context) 
        => OnLookEvent?.Invoke(context.ReadValue<Vector2>());

    public void OnJump(InputAction.CallbackContext context) 
    {
        if (context.started) OnJumpEvent?.Invoke();
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
}
