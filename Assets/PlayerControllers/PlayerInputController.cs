using UnityEngine;

[DefaultExecutionOrder(-25)]
public class PlayerInputController : MonoBehaviour
{
    [SerializeField] private float jumpBufferTime = 0.2f;
    [SerializeField] private float attackBufferTime = 0.4f;
    [SerializeField] private int attackBufferSize = 2;
    
    
    private Utility.TemporaryValue<bool> jumpBuffer = new (false,true);
    public void ResetJumpBuffer(){jumpBuffer.Deactivate();}
    public bool IsJumpPressed => jumpBuffer.Value;

    public bool IsAttackPressed { get; private set; } = false;
    public bool IsParryPressed { get; private set; } = false;

    public bool IsAttackBufferActive => attackBuffer.Value;
    
    public void ConsumeAttack(){attackBuffer.Deactivate();}
    
    private Utility.MultipleTemporaryValue<bool> attackBuffer;

    public enum SwordInputEnum {Non, Attack, Parry}
    public SwordInputEnum SwordInputState { get; private set; }  = SwordInputEnum.Non;
    public SwordInputEnum LastActiveSwordInputState { get; private set; } =  SwordInputEnum.Non;
    
    
    
    public bool IsDashPressed { get; private set; } = false;
    public bool IsSlidePressed { get; private set; } = false;
    public bool IsAimPressed { get; private set; } = false;
    
    public Vector3 InputMoveVector => inputMoveVector;
    public Vector3 LastNonZeroInputMoveVector { get; private set; } = Vector3.zero;
    public bool IsPlayerPressingWASD { get; private set; } = false;

    public void ChangeLastNonZeroInputToCurrentHorizontalVelocity()
    {
        LastNonZeroInputMoveVector = _playerSensors.HorizontalVelocity;
    }
    
    
    private Vector3 rawInputMoveVector;
    private Vector3 inputMoveVector; // always normalized
    
    private bool shouldFreezeLookDirection = false;
    private Vector3 freezeLookDirection= Vector3.zero;

    private PlayerGunController _playerGunController;
    private PlayerSensors _playerSensors;
    private PlayerTargetLockController _playerTargetLockController;

    private void Awake()
    {
        _playerGunController = GetComponent<PlayerGunController>();
        _playerSensors = GetComponentInChildren<PlayerSensors>();
        _playerTargetLockController = GetComponent<PlayerTargetLockController>();
        attackBuffer = new(false, true, attackBufferTime, attackBufferSize);
    }
    
    private void OnEnable()
    {
        GlobalGameInputManager.Instance.OnMoveEvent += HandleMove;
        GlobalGameInputManager.Instance.OnJumpEvent += HandleJump;
        GlobalGameInputManager.Instance.OnDashPressEvent += HandleDashPressed;
        GlobalGameInputManager.Instance.OnDashReleaseEvent += HandleDashReleased;
        GlobalGameInputManager.Instance.OnSlidePressEvent +=  HandleSlidePress;
        GlobalGameInputManager.Instance.OnSlideReleaseEvent +=  HandleSlideRelease;
        GlobalGameInputManager.Instance.OnAttackPressEvent += HandleAttackPress;
        GlobalGameInputManager.Instance.OnAttackReleaseEvent += HandleAttackRelease;
        GlobalGameInputManager.Instance.OnParryPressEvent += HandleParryPress;
        GlobalGameInputManager.Instance.OnParryReleaseEvent += HandleParryRelease;
        GlobalGameInputManager.Instance.OnAimPressEvent += HandleAimPress;
        GlobalGameInputManager.Instance.OnAimReleaseEvent += HandleAimRelease;
        GlobalGameInputManager.Instance.OnTargetLockEvent += HandleTargetTargetLock;
    }

    private void OnDisable()
    {
        GlobalGameInputManager.Instance.OnMoveEvent -= HandleMove;
        GlobalGameInputManager.Instance.OnJumpEvent -= HandleJump;
        GlobalGameInputManager.Instance.OnDashPressEvent -= HandleDashPressed;
        GlobalGameInputManager.Instance.OnDashReleaseEvent -= HandleDashReleased;
        GlobalGameInputManager.Instance.OnSlidePressEvent -=  HandleSlidePress;
        GlobalGameInputManager.Instance.OnSlideReleaseEvent -=  HandleSlideRelease;
        GlobalGameInputManager.Instance.OnAttackPressEvent -= HandleAttackPress;
        GlobalGameInputManager.Instance.OnAttackReleaseEvent -= HandleAttackRelease;
        GlobalGameInputManager.Instance.OnParryPressEvent -= HandleParryPress;
        GlobalGameInputManager.Instance.OnParryReleaseEvent -= HandleParryRelease;
        GlobalGameInputManager.Instance.OnAimPressEvent -= HandleAimPress;
        GlobalGameInputManager.Instance.OnAimReleaseEvent -= HandleAimRelease;
        GlobalGameInputManager.Instance.OnTargetLockEvent -= HandleTargetTargetLock;
    }

    private void HandleTargetTargetLock()
    {
        _playerTargetLockController.ChangeLock();
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
        rawInputMoveVector = new Vector3(input.x, 0f, 
            input.y);
    }

    private void HandleJump()
    {
        jumpBuffer.Activate(jumpBufferTime);
    }

    private void HandleAttackPress()
    {
        if (IsAimPressed)
        {
            _playerGunController.Shoot();
        }
        else
        {
            IsAttackPressed = true;
            attackBuffer.Activate();
        }
    }
    private void HandleAttackRelease()
    {
        IsAttackPressed = false;
    }

    private void HandleParryPress()
    {
        IsParryPressed = true;
    }
    
    private void HandleParryRelease()
    {
        IsParryPressed = false;
    }

    private void HandleAimPress()
    {
        IsAimPressed = true;
    }

    private void HandleAimRelease()
    {
        IsAimPressed = false;
    }
    
    private void Update()
    { 
        if (_playerGunController.GunState == GunController.GunStateEnum.Non)
        {
            if (IsParryPressed)
            {
                SwordInputState = SwordInputEnum.Parry;
                LastActiveSwordInputState = SwordInputEnum.Parry;
            }
            else if (IsAttackBufferActive)
            {
                SwordInputState = SwordInputEnum.Attack;
                LastActiveSwordInputState = SwordInputEnum.Attack;
            }
            else
            {
                SwordInputState = SwordInputEnum.Non;
            }
        }
        
        
        if (shouldFreezeLookDirection)
        {
            inputMoveVector = Utility.FromLocalToGlobalByZX(freezeLookDirection, rawInputMoveVector);
        }
        else if (_playerTargetLockController.IsLocked)
        {
            inputMoveVector = Utility.FromLocalToGlobalByZX(_playerTargetLockController.TargetDirection, rawInputMoveVector);
        }
        else
        {
            inputMoveVector = GlobalLookDirectionManager.FromCameraLocalToGlobalByZX(rawInputMoveVector);
        }

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
