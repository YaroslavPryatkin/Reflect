using UnityEngine;

public class PlayerJumpController : MonoBehaviour
{

    [Header("Jumping from ground")]
    [SerializeField] private float jumpStrength = 20f;
    
    [Header("Jumping from wall")]
    [SerializeField] private float wallJumpUpStrength = 25f;
    [SerializeField] private float wallJumpSideStrength = 30f;
    [SerializeField] private float wallJumpBackStrength = 10f;
    [SerializeField] private float wallJumpBackSpeedThreshold = 5f;
    
    [Header("Jumping from rail line")]
    [SerializeField] private float railJumpStrength = 20f;
    
    [Header("Coyote")]
    [SerializeField] private float groundCoyoteTime = 0.2f;
    [SerializeField] private float wallCoyoteTime = 0.1f;
    [SerializeField] private float railCoyoteTime = 0.1f;


    [Header("Ground stand timings")]
    [SerializeField] private float startingGroundStandJumpTime = 0.1f;
    [SerializeField] private float continuingGroundStandJumpTime = 0.7f;
    [SerializeField] private float groundStandJumpRechargeTime = 0.2f;
    
    [Header("Ground run timings")]
    [SerializeField] private float startingGroundRunJumpTime = 0.1f;
    [SerializeField] private float continuingGroundRunJumpTime = 0.7f;
    [SerializeField] private float groundRunSpeedThreshold = 2f;
    [SerializeField] private float groundRunJumpRechargeTime = 0.2f;
    
    [Header("Wall jumping timings")] 
    [SerializeField] private float startingWallJumpTime = 0.1f;
    [SerializeField] private float continuingWallJumpTime = 0.7f;
    [SerializeField] private float wallJumpRechargeTime = 0.4f;
    [Header("Rail line jumping timings")] 
    [SerializeField] private float startingRailJumpTime = 0.1f;
    [SerializeField] private float continuingRailJumpTime = 0.7f;
    [SerializeField] private float railJumpRechargeTime = 0.4f;
    
    [Header("Suppress")]
    [SerializeField] private float groundSpeedSuppressAfterJump = 0.3f;
    
    [Header("Suppresses for fixed movement")]
    [SerializeField] private float afterJumpFromWall = 0.5f;
    [SerializeField] private float afterJumpFromGround = 0.2f;
    [SerializeField] private float afterJumpFromLine = 0.2f;
    
    [Header("General")]
    [SerializeField] private float interruptionRechargeTime = 0.1f;


    public float StartingGroundStandJumpTime => startingGroundStandJumpTime;
    public float ContinuingGroundStandJumpTime => continuingGroundStandJumpTime;
    
    public float StartingGroundRunJumpTime => startingGroundRunJumpTime;
    public float ContinuingGroundRunJumpTime => continuingGroundRunJumpTime;
    
    public float StartingWallJumpTime => startingWallJumpTime;
    public float ContinuingWallJumpTime => continuingWallJumpTime;
    
    public float StartingRailJumpTime => startingRailJumpTime;
    public float ContinuingRailJumpTime => continuingRailJumpTime;
    
    private Rigidbody rb;
    private PlayerSlidingController _playerSlidingController;
    private PlayerFixedDirectionMovementController _playerFixedDirectionMovementController;
    private PlayerSensors _playerSensors;
    private PlayerInputController _playerInputController;
    private PlayerForwardJumpingController _playerForwardJumpingController;
    private PlayerManager _playerManager;

    public enum JumpPlaceEnum
    {
        Non, Ground, Wall, Line
    }
    
    private readonly UtilityClasses.DelayedValueTimer<JumpPlaceEnum> _possibleJumpPlace = JumpPlaceEnum.Non;

    public enum StateEnum
    {
        Non, Starting, Continuing
    }
    private readonly UtilityClasses.BlockingValueTimer<StateEnum> _jumpState = StateEnum.Non;

    public enum JumpTypeEnum
    {
        Standing, Running, Wall, Line
    }
    private JumpTypeEnum _currentJumpType = JumpTypeEnum.Standing;
    public bool IsStateNon => _jumpState.Value == StateEnum.Non;
    public StateEnum State => _jumpState.Value;
    public JumpTypeEnum Type => _currentJumpType;
    
    private readonly UtilityClasses.TemporaryValue<bool> _shouldSuppressGroundFriction = new(false, true);
    public bool ShouldSuppressGroundFriction => _shouldSuppressGroundFriction.Value;

    public void InterruptJump()
    {
        if(_jumpState!=StateEnum.Non)
            _jumpState.SetForce(StateEnum.Non, interruptionRechargeTime);
    }
    
    private void InterruptJumpIfRequired()
    {
        if (
            (!_playerManager.CanJump && _jumpState.Value != StateEnum.Non) ||
            (_jumpState.Value == StateEnum.Continuing && !_playerSlidingController.IsStateNon)
        )
        {
            _jumpState.SetForce(StateEnum.Non, interruptionRechargeTime);
        }
    }
    
    private void ChangeJumpState()
    {
        if (_jumpState.CanBeChanged)
        {
            switch (_jumpState.Value)
            {
                case StateEnum.Starting:
                    
                    rb.linearVelocity = _playerSensors.VelocityAlignedWithGround;
                    
                    switch (_currentJumpType)
                    {
                        case JumpTypeEnum.Standing or JumpTypeEnum.Running:
                            if(_playerSensors.IsGrounded && _playerSensors.FoundGroundNormal)
                                rb.AddForce(_playerSensors.GroundNormal * jumpStrength, ForceMode.Impulse);
                            else
                                rb.AddForce(Vector3.up * jumpStrength, ForceMode.Impulse);
                    
                            _playerFixedDirectionMovementController.SuppressAfterJump(afterJumpFromGround);
                            _shouldSuppressGroundFriction.Activate(groundSpeedSuppressAfterJump);
                            break;
                        case JumpTypeEnum.Wall:
                            var force = Vector3.up * wallJumpUpStrength + _playerFixedDirectionMovementController.LastNormal * wallJumpSideStrength;
                            if(_playerSensors.HorizontalSpeed >= wallJumpBackSpeedThreshold)
                                force -= _playerSensors.NormalizedHorizontalVelocity * wallJumpBackStrength;
                            rb.AddForce( force, ForceMode.Impulse);
                            
                            _playerFixedDirectionMovementController.SuppressAfterJump(afterJumpFromWall);
                            break;
                        case JumpTypeEnum.Line:
                            rb.AddForce( Vector3.up * railJumpStrength, ForceMode.Impulse);
                            
                            _playerFixedDirectionMovementController.SuppressAfterJump(afterJumpFromLine);
                            break;
                    }
                    
                    _playerSensors.UpdateVelocity();

                    switch (_currentJumpType)
                    {
                        case JumpTypeEnum.Standing:
                            _jumpState.SetForce(StateEnum.Continuing, continuingGroundStandJumpTime);
                            break;
                        case JumpTypeEnum.Running:
                            _jumpState.SetForce(StateEnum.Continuing, continuingGroundRunJumpTime);
                            break;
                        case JumpTypeEnum.Wall:
                            _jumpState.SetForce(StateEnum.Continuing, continuingWallJumpTime);
                            break;
                        case  JumpTypeEnum.Line:
                            _jumpState.SetForce(StateEnum.Continuing, continuingRailJumpTime);
                            break;
                    }
                    break;
                case StateEnum.Continuing:
                    switch (_currentJumpType)
                    {
                        case JumpTypeEnum.Standing:
                            _jumpState.SetForce(StateEnum.Non, groundStandJumpRechargeTime);
                            break;
                        case JumpTypeEnum.Running:
                            _jumpState.SetForce(StateEnum.Non, groundRunJumpRechargeTime);
                            break;
                        case JumpTypeEnum.Wall:
                            _jumpState.SetForce(StateEnum.Non, wallJumpRechargeTime);
                            break;
                        case JumpTypeEnum.Line:
                            _jumpState.SetForce(StateEnum.Non, railJumpRechargeTime);
                            break;
                    }
                    break;
            }
        }
    }

    private void DetectPlaceToJumpFrom()
    {
        if (!_playerFixedDirectionMovementController.IsStateNon)
        {
            if(_playerFixedDirectionMovementController.State == PlayerFixedDirectionMovementController.StateEnum.Line)
                _possibleJumpPlace.Set(JumpPlaceEnum.Line, 0);
            else
                _possibleJumpPlace.Set(JumpPlaceEnum.Wall, 0);
        }
        else if (_playerSensors.IsGrounded)
        {
            _possibleJumpPlace.Set(JumpPlaceEnum.Ground, 0);
        }
        else
        {
            switch (_possibleJumpPlace.RealValue)
            {
                case JumpPlaceEnum.Ground:
                    _possibleJumpPlace.Set(0, groundCoyoteTime);
                    break;
                case  JumpPlaceEnum.Wall:
                    _possibleJumpPlace.Set(0, wallCoyoteTime);
                    break;
                case JumpPlaceEnum.Line:
                    _possibleJumpPlace.Set(0, railCoyoteTime);
                    break;
            }
        }
    }

    public void PerformLineJump()
    {
        if (_jumpState.Value != StateEnum.Non) return;
        
        _currentJumpType = JumpTypeEnum.Line;
        _jumpState.SetForce(StateEnum.Starting, startingRailJumpTime);
    }
    
    private void StartJump()
    {
        if (!_playerInputController.IsJumpBufferActive  || !_playerManager.CanJump || _jumpState.Value!=StateEnum.Non || !_jumpState.CanBeChanged) return;


        switch (_possibleJumpPlace.Value)
        {
            case JumpPlaceEnum.Wall:
                _currentJumpType = JumpTypeEnum.Wall;
                _jumpState.SetForce(StateEnum.Starting, startingWallJumpTime);
                break;
            case JumpPlaceEnum.Line:
                _currentJumpType = JumpTypeEnum.Line;
                _jumpState.SetForce(StateEnum.Starting, startingRailJumpTime);
                break;
            case JumpPlaceEnum.Ground:
                if (_playerSensors.FrontGroundState == 1)
                {
                    _playerForwardJumpingController.PerformForwardJump(transform.position, _playerSensors.GetForwardGroundEndPoint(), _playerSensors.FrontGroundObstacleHeight);
                }
                else
                {
                    if (_playerSensors.HorizontalSpeed < groundRunSpeedThreshold)
                    {
                        _currentJumpType = JumpTypeEnum.Standing;
                        _jumpState.SetForce(StateEnum.Starting, startingGroundStandJumpTime);
                    }
                    else
                    {
                        _currentJumpType = JumpTypeEnum.Running;
                        _jumpState.SetForce(StateEnum.Starting, startingGroundRunJumpTime);
                    }
                
                }
                break;
        }
        _playerInputController.ConsumeJump();
    }

    private void Update()
    {
        DetectPlaceToJumpFrom();
        StartJump();
    }

    private void FixedUpdate()
    {
        InterruptJumpIfRequired();
        ChangeJumpState();
    }
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        _playerSlidingController = GetComponent<PlayerSlidingController>();
        _playerSensors = GetComponentInChildren<PlayerSensors>();
        _playerInputController = GetComponentInChildren<PlayerInputController>();
        _playerForwardJumpingController = GetComponentInChildren<PlayerForwardJumpingController>();
        _playerManager = GetComponent<PlayerManager>();
        _playerFixedDirectionMovementController = GetComponent<PlayerFixedDirectionMovementController>();
    }
}
