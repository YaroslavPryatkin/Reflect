using UnityEngine;

[DefaultExecutionOrder(-25)]
public class PlayerInputController : MonoBehaviour
{
    [Header("Jump")]
    [SerializeField] private float jumpBufferTime = 0.2f;
    [Header("Melee attack")]
    [SerializeField] private float attackBufferTime = 0.4f;
    [SerializeField] private float heavyChargeAntiTime = 0.1f;
    [SerializeField] private int attackBufferSize = 2;
    [Header("Parry")]
    [SerializeField] private float parryBufferTime = 0.4f;
    [SerializeField] private int parryBufferSize = 2;
    
    
    private readonly UtilityClasses.TemporaryValue<bool> _jumpBuffer = new (false,true);
    public void ConsumeJump(){_jumpBuffer.Deactivate();}
    public void ConsumeAttack(){
        _attackBuffer.Deactivate();
        _parryBuffer.DeactivateAll();
        _shouldAttackReleaseTriggerAttack = false;
    }

    public void ConsumeParry()
    {
        _parryBuffer.Deactivate(); 
        _attackBuffer.DeactivateAll();
        _heavyAttackChargeAntiBuffer.Deactivate();
    }

    public void ClearAllBuffers()
    {
        _attackBuffer.DeactivateAll(); 
        _parryBuffer.DeactivateAll(); 
        _jumpBuffer.Deactivate();
        _heavyAttackChargeAntiBuffer.Deactivate();
    }
    public bool IsJumpBufferActive => _jumpBuffer.Value;
    public bool IsDashPressed { get; private set; } = false;
    public bool IsSlidePressed { get; private set; } = false;
    public bool IsAimPressed { get; private set; } = false;
    
    private bool _isAttackPressed = false;
    private bool _shouldAttackReleaseTriggerAttack = false;

    public bool IsAttackBufferActive => _attackBuffer.Value;
    
    private UtilityClasses.MultipleTemporaryValue<bool> _attackBuffer;

    private readonly UtilityClasses.TemporaryValue<bool> _heavyAttackChargeAntiBuffer = new(true,false);
    
    public bool IsHeavyAttackCharging => _isAttackPressed && _heavyAttackChargeAntiBuffer.Value;
    
    public bool IsParryBufferActive => _parryBuffer.Value;
    
    private UtilityClasses.MultipleTemporaryValue<bool> _parryBuffer;
    
    public Vector3 InputMoveVector => inputMoveVector;
    public Vector3 NonZeroInputMoveVector { get; private set; } = Vector3.zero;
    public bool IsPlayerPressingWASD { get; private set; } = false;

    public void ChangeLastNonZeroInputToCurrentHorizontalVelocity()
    {
        NonZeroInputMoveVector = _playerSensors.NormalizedHorizontalVelocity;
    }
    
    
    private Vector3 rawInputMoveVector;
    private Vector3 inputMoveVector; // always normalized
    
    private bool shouldFreezeLookDirection = false;
    private Vector3 freezeLookDirection= Vector3.zero;

    private PlayerGunController _playerGunController;
    private PlayerSensors _playerSensors;
    private PlayerTargetLockController _playerTargetLockController;
    private MeleeTransformController _meleeTransformController;

    private void Awake()
    {
        _playerGunController = GetComponent<PlayerGunController>();
        _playerSensors = GetComponentInChildren<PlayerSensors>();
        _playerTargetLockController = GetComponent<PlayerTargetLockController>();
        _meleeTransformController = GetComponent<MeleeTransformController>();
        _attackBuffer = new(false, true, attackBufferTime, attackBufferSize);
        _parryBuffer = new (false, true, parryBufferTime, parryBufferSize);
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
        _playerTargetLockController.FlipLock();
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
        _jumpBuffer.Activate(jumpBufferTime);
    }
    private void HandleAttackPress()
    {
        if (IsAimPressed)
        {
            _playerGunController.Shoot();
        }
        else
        {
            _shouldAttackReleaseTriggerAttack = true;
            _isAttackPressed = true;
            _heavyAttackChargeAntiBuffer.Activate(heavyChargeAntiTime);
        }
    }
    private void HandleAttackRelease()
    {
        _heavyAttackChargeAntiBuffer.Deactivate();
        _isAttackPressed = false;
        if (_shouldAttackReleaseTriggerAttack)
        {
            _shouldAttackReleaseTriggerAttack = false;
            _attackBuffer.Activate();
        }
    }
    private void HandleParryPress()
    {
        _parryBuffer.Activate();
    }
    private void HandleParryRelease()
    {
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
        if (shouldFreezeLookDirection)
        {
            inputMoveVector = UtilityFunctions.FromLocalToGlobalByZX(freezeLookDirection, rawInputMoveVector);
        }
        else if (_playerTargetLockController.IsLocked)
        {
            inputMoveVector = UtilityFunctions.FromLocalToGlobalByZX(_playerTargetLockController.NormalizedHorizontalDirectionToLockedTarget, rawInputMoveVector);
        }
        else
        {
            inputMoveVector = GlobalLookDirectionManager.FromCameraLocalToGlobalByZX(rawInputMoveVector);
        }

        inputMoveVector.y = 0f;
        
        
        if (inputMoveVector.sqrMagnitude > 0.001f)
        {
            inputMoveVector.Normalize();
            NonZeroInputMoveVector = inputMoveVector;
            IsPlayerPressingWASD = true;
        }
        else
        {
            if(_playerSensors.HorizontalSpeed > 0.01f)
                NonZeroInputMoveVector = _playerSensors.NormalizedHorizontalVelocity;
            else
                NonZeroInputMoveVector = transform.forward;
            IsPlayerPressingWASD = false;
        }
        
    }
    
}
