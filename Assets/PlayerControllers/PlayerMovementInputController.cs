using UnityEngine;
using UnityEngine.InputSystem;




public class PlayerMovementInputController : MonoBehaviour
{
    [SerializeField] private float jumpBufferTime = 0.2f;
    [SerializeField] private float dashBufferTime = 0.2f;
    
    private Utility.TemporaryValue<bool> jumpBuffer = new (false,true);
    public void ResetJumpBuffer(){jumpBuffer.Deactivate();}
    public bool IsJumpPressed => jumpBuffer.Value;
    public bool IsDashPressed { get; private set; }
    public bool IsSlidePressed { get; private set; } = false;
    public Vector3 InputMoveVector => inputMoveVector;
    public Vector3 LastNonZeroInputMoveVector { get; private set; } = Vector3.zero;
    public bool IsPlayerPressingWASD { get; private set; } = false;
    
    
    
    private Vector3 rawInputMoveVector;
    private Vector3 inputMoveVector; // always normalized
    
    private bool shouldFreezeLookDirection = false;
    private Vector3 freezeLookDirection= Vector3.zero;
    
    
    private void OnEnable()
    {
        PlayerMapInputManager.Instance.OnMoveEvent += HandleMove;
        PlayerMapInputManager.Instance.OnJumpEvent += HandleJump;
        PlayerMapInputManager.Instance.OnDashPressEvent += HandleDashPressed;
        PlayerMapInputManager.Instance.OnDashReleaseEvent += HandleDashReleased;
        PlayerMapInputManager.Instance.OnSlidePressEvent +=  HandleSlidePress;
        PlayerMapInputManager.Instance.OnSlideReleaseEvent +=  HandleSlideRelease;
    }

    private void OnDisable()
    {
        PlayerMapInputManager.Instance.OnMoveEvent -= HandleMove;
        PlayerMapInputManager.Instance.OnJumpEvent -= HandleJump;
        PlayerMapInputManager.Instance.OnDashPressEvent -= HandleDashPressed;
        PlayerMapInputManager.Instance.OnDashReleaseEvent -= HandleDashReleased;
        PlayerMapInputManager.Instance.OnSlidePressEvent -=  HandleSlidePress;
        PlayerMapInputManager.Instance.OnSlideReleaseEvent -=  HandleSlideRelease;
    }

    private void HandleDashPressed()
    {
        IsDashPressed = true;
        shouldFreezeLookDirection = true;
        freezeLookDirection = GlobalLookDirectionManager.CurrentLookDirection;
    }
    private void HandleDashReleased()
    {
        IsDashPressed = false;
        shouldFreezeLookDirection = false;
    }
    
    private void HandleSlidePress()
    {
        IsSlidePressed = true;
    }
    
    private void HandleSlideRelease()
    {
        IsSlidePressed = false;
    }
    
    
    private void HandleMove(Vector2 input)
    {
        rawInputMoveVector = new Vector3(input.x, 0f, input.y);
    }

    private void HandleJump()
    {
        jumpBuffer.Activate(jumpBufferTime);
    }
    
    private void Update()
    {
        if (shouldFreezeLookDirection)
        {
            inputMoveVector = Utility.FromLocalToGlobalByZX(freezeLookDirection, rawInputMoveVector);
        }
        else
            inputMoveVector = GlobalLookDirectionManager.FromCameraLocalToGlobalByZX(rawInputMoveVector);
        
        inputMoveVector.y = 0f;
        inputMoveVector.Normalize();

        if (inputMoveVector.magnitude > 0.001f)
        {
            LastNonZeroInputMoveVector = inputMoveVector;
            IsPlayerPressingWASD = true;
        }
        else
        {
            IsPlayerPressingWASD = false;
        }
    }
    
    
}
