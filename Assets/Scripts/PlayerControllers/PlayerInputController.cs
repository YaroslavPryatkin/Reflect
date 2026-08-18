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
    [SerializeField] private float weaponArtAntiTime = 0.1f;
    
    

    public bool IsDashPressed { get; private set; } = false;
    public bool IsSlidePressed { get; private set; } = false;
    public bool IsAimPressed { get; private set; } = false;
    
    private readonly UtilityTimers.TemporaryValue<bool> _jumpBuffer = new (false,true);
    public bool IsJumpBufferActive => _jumpBuffer.Value;
    
    private UtilityTimers.MultipleTemporaryValue<bool> _attackBuffer;
    private bool _shouldTriggerAttackOnAttackRelease = false;
    private UtilityClasses.PressAntiBuffer _heavyAttackBuffer;
    
    private UtilityTimers.MultipleTemporaryValue<bool> _parryBuffer;
    private UtilityClasses.PressAntiBuffer _weaponArtBuffer;
    
    
    public bool IsAttackBufferActive => _attackBuffer.Value;
    public bool IsHeavyAttackCharging => _heavyAttackBuffer.IsPressed;
    
    public bool IsParryBufferActive => _parryBuffer.Value;
    public bool IsWeaponArtCharging => _weaponArtBuffer.IsPressed;
    
    public Vector3 InputMoveVector => _inputMoveVector;
    public Vector3 NonZeroInputMoveVector { get; private set; } = Vector3.zero;
    public bool IsPlayerPressingWASD { get; private set; } = false;
    
    
    private Vector3 _rawInputMoveVector;
    private Vector3 _inputMoveVector; // always normalized
    
    private bool _shouldFreezeLookDirection = false;
    private Vector3 _freezeLookDirection= Vector3.zero;

    private PlayerGunController _playerGunController;
    private PlayerSensors _playerSensors;
    private PlayerTargetLockController _playerTargetLockController;
    private MeleeTransformController _meleeTransformController;
    private PlayerMeleeController _playerMeleeController;

    public void ChangeLastNonZeroInputToCurrentHorizontalVelocity()
    {
        NonZeroInputMoveVector = _playerSensors.NormalizedHorizontalVelocity;
    }
    public void ConsumeJump(){_jumpBuffer.Deactivate();}
    public void ConsumeAttack(){
        _attackBuffer.Deactivate();
        _parryBuffer.DeactivateAll();
        _shouldTriggerAttackOnAttackRelease = false;
        _weaponArtBuffer.Release();
    }

    public void ConsumeParry()
    {
        _parryBuffer.Deactivate(); 
        _attackBuffer.DeactivateAll();
        _heavyAttackBuffer.Release();
    }

    public void ClearAllBuffers()
    {
        _attackBuffer.DeactivateAll(); 
        _parryBuffer.DeactivateAll(); 
        _jumpBuffer.Deactivate();
        _heavyAttackBuffer.Release();
        _weaponArtBuffer.Release();
        _playerMeleeController.ResetNextComboToPlay();
    }
    
    private void Awake()
    {
        _playerGunController = GetComponent<PlayerGunController>();
        _playerSensors = GetComponentInChildren<PlayerSensors>();
        _playerTargetLockController = GetComponent<PlayerTargetLockController>();
        _meleeTransformController = GetComponent<MeleeTransformController>();
        _playerMeleeController = GetComponent<PlayerMeleeController>();
        _attackBuffer = new(false, true, attackBufferTime, attackBufferSize);
        _parryBuffer = new (false, true, parryBufferTime, parryBufferSize);
        _heavyAttackBuffer = new(heavyChargeAntiTime);
        _weaponArtBuffer = new(weaponArtAntiTime);
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
        _shouldFreezeLookDirection = true;
        _freezeLookDirection = GlobalLookDirectionManager.CurrentLookDirection;
    }
    private void HandleDashReleased()
    {
        IsDashPressed = false;
        _shouldFreezeLookDirection = false;
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
        _rawInputMoveVector = new Vector3(input.x, 0f, 
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
            _shouldTriggerAttackOnAttackRelease = true;
            _heavyAttackBuffer.Press();
        }
    }
    private void HandleAttackRelease()
    {
        _heavyAttackBuffer.Release();
        if (_shouldTriggerAttackOnAttackRelease)
        {
            _shouldTriggerAttackOnAttackRelease = false;
            _attackBuffer.Activate();
        }
    }
    private void HandleParryPress()
    {
        _parryBuffer.Activate();
        _weaponArtBuffer.Press();
    }
    private void HandleParryRelease()
    {
        _weaponArtBuffer.Release();
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
        if (_shouldFreezeLookDirection)
        {
            _inputMoveVector = UtilityFunctions.FromLocalToGlobalByZX(_freezeLookDirection, _rawInputMoveVector);
        }
        else if (_playerTargetLockController.IsLocked)
        {
            _inputMoveVector = UtilityFunctions.FromLocalToGlobalByZX(_playerTargetLockController.NormalizedHorizontalDirectionToLockedTarget, _rawInputMoveVector);
        }
        else
        {
            _inputMoveVector = GlobalLookDirectionManager.FromCameraLocalToGlobalByZX(_rawInputMoveVector);
        }

        _inputMoveVector.y = 0f;
        
        
        if (_inputMoveVector.sqrMagnitude > 0.001f)
        {
            _inputMoveVector.Normalize();
            NonZeroInputMoveVector = _inputMoveVector;
            IsPlayerPressingWASD = true;
        }
        else
        {
            NonZeroInputMoveVector = transform.forward;
            IsPlayerPressingWASD = false;
        }
        
    }
    
}
