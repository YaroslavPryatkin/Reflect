using UnityEngine;
using UnityEngine.InputSystem;




public class PlayerMovementInputController : MonoBehaviour
{
    [SerializeField] private float jumpBufferTime = 0.2f;
    
    private Utility.TemporaryValue<bool> jumpBuffer = new (false,true);
    public void ResetJumpBuffer(){jumpBuffer.Deactivate();}
    public bool IsJumpPressed(){return jumpBuffer.Value;}

    private Vector3 rawInputMoveVector;
    private Vector3 inputMoveVector; // always normalized
    
    private bool shouldFreezeLookDirection = false;
    private Vector3 freezeLookDirection= Vector3.zero;
    
    public Vector3 InputMoveVector => inputMoveVector;
    
    private void OnEnable()
    {
        GlobalInputManager.Instance.OnMoveEvent += HandleMove;
        GlobalInputManager.Instance.OnJumpEvent += HandleJump;
        GlobalInputManager.Instance.OnDashPressEvent += HandleDashPressed;
        GlobalInputManager.Instance.OnDashReleaseEvent += HandleDashReleased;
    }

    private void OnDisable()
    {
        GlobalInputManager.Instance.OnMoveEvent -= HandleMove;
        GlobalInputManager.Instance.OnJumpEvent -= HandleJump;
        GlobalInputManager.Instance.OnDashPressEvent -= HandleDashPressed;
        GlobalInputManager.Instance.OnDashReleaseEvent -= HandleDashReleased;
    }

    private void HandleDashPressed()
    {
        shouldFreezeLookDirection = true;
        freezeLookDirection = GlobalLookDirectionManager.CurrentLookDirection;
    }
    private void HandleDashReleased()
    {
        shouldFreezeLookDirection = false;
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
        if(shouldFreezeLookDirection)
            inputMoveVector = Utility.FromLocalToGlobalByZX(freezeLookDirection, rawInputMoveVector);
        else
            inputMoveVector = GlobalLookDirectionManager.FromCameraLocalToGlobalByZX(rawInputMoveVector);
        
        inputMoveVector.y = 0f;
        inputMoveVector.Normalize();
    }
    
    
}
