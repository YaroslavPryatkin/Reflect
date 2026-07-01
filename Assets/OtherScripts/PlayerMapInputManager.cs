using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-100)]
public class PlayerMapInputManager : MonoBehaviour
{
    public static PlayerMapInputManager Instance;

    public event System.Action<Vector2> OnMoveEvent;
    public event System.Action<Vector2> OnLookEvent;
    public event System.Action OnJumpEvent;
    public event System.Action OnDashPressEvent;
    public event System.Action OnDashReleaseEvent;
    public event System.Action OnSlidePressEvent;
    public event System.Action OnSlideReleaseEvent;

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
            //Debug.Log("On slide pressed");
            OnSlidePressEvent?.Invoke();
        }
        else if (context.canceled)
        {
            //Debug.Log("On slide released");
            OnSlideReleaseEvent?.Invoke();
        }
    }
}
