using UnityEngine;
using UnityEngine.InputSystem;

public class GlobalInputManager : MonoBehaviour
{
    public static GlobalInputManager Instance;

    public event System.Action<Vector2> OnMoveEvent;
    public event System.Action<Vector2> OnLookEvent;
    public event System.Action OnJumpEvent;
    public event System.Action OnDashPressEvent;
    public event System.Action OnDashReleaseEvent;

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
}
