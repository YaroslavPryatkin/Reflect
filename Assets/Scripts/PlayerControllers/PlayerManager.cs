using Unity.VisualScripting;
using UnityEngine;
using FixedMovementStateEnum = PlayerFixedDirectionMovementController.StateEnum;

public class PlayerManager : MonoBehaviour
{
    [Header("Speeds")] 
    [SerializeField] private float groundSpeed = 9f;
    [SerializeField] private float airSpeed = 1f;
    [SerializeField] private float wallRunSpeed = 12f;
    [SerializeField] private float railLineSpeed = 12f;
    [SerializeField] private float groundSpeedHardCap = 25f;

    [Header("Speed Loss")] 
    [Header("Sliding")]
    [SerializeField] private float slideFriction = 9f;
    [SerializeField] private float slideDrag = 0.2f;
    [SerializeField] private float slideAngleSpeed = 2f;
    [Header("Forward Jump")]
    [SerializeField] private float forwardJumpLandingFriction = 6f;
    [SerializeField] private float forwardJumpLandingDrag = 0.07f;
    [SerializeField] private float forwardJumpLandingAngleSpeed = 2f;
    [Header("Landing")]
    [SerializeField] private float landingRollFriction = 0.1f;
    [SerializeField] private float landingRollDrag = 0f;
    [SerializeField] private float landingRollAngleSpeed = 2f;
    
    private PlayerSensors _playerSensors;
    private PlayerInputController _playerInputController;
    private PlayerDashController _playerDashController;
    private PlayerForwardJumpingController _playerForwardJumpingController;
    private PlayerJumpController _playerJumpController;
    private PlayerLandingController _playerLandingController;
    private GettingHitController _gettingHitController;
    private PlayerFixedDirectionMovementController _playerFixedDirectionMovementController;
    private PlayerSlidingController _playerSlidingController;
    private PlayerMeleeController _playerMeleeController;
    private PlayerTargetLockController _playerTargetLockController;
    private PlayerMeleeTransformController _playerMeleeTransformController;
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        _playerSensors = GetComponent<PlayerSensors>();
        _playerInputController = GetComponent<PlayerInputController>();
        _playerDashController = GetComponent<PlayerDashController>();
        _playerForwardJumpingController = GetComponent<PlayerForwardJumpingController>();
        _playerJumpController = GetComponent<PlayerJumpController>();
        _playerLandingController = GetComponent<PlayerLandingController>();
        _gettingHitController = GetComponent<GettingHitController>();
        _playerFixedDirectionMovementController = GetComponent<PlayerFixedDirectionMovementController>();
        _playerSlidingController = GetComponent<PlayerSlidingController>();
        _playerMeleeController =  GetComponent<PlayerMeleeController>();
        _playerTargetLockController = GetComponent<PlayerTargetLockController>();
        _playerMeleeTransformController = GetComponent<PlayerMeleeTransformController>();
    }

    public Vector3 TargetMoveDirection { get; private set; } = Vector3.zero;
    public float TargetMoveSpeed { get; private set; } = 0f;

    public bool CanSlide { get; private set; } = false;
    public bool CanGetStunned { get; private set; } = false;
    public bool CanFixedMovement { get; private set; } = false;
    public bool CanAim { get; private set; } = false;
    public bool CanJump { get; private set; } = false;
    public bool CanHoldSword { get; private set; } = false;
    public bool CanNotUseSword { get; private set; } = false;
    public bool UseGroundPhysics { get;private set; } = false;
    public bool LookToLockedTarget { get; private set; } = false;

    public bool CanNotLanding { get; private set; } = true;

    private void Update()
    {
        _playerLandingController.ToCallFromMovementController();
        SetFlags();
        SetTargetMoveVector();
    }

    private void SetFlags()
    {
        CanSlide = _playerSensors.IsGrounded &&
                   _playerLandingController.CanBeInterrupted &&
                   !_playerDashController.IsDashing &&
                   !_playerForwardJumpingController.IsForwardJumping;
        
        _gettingHitController.CanGetStunned = CanGetStunned =
            _playerFixedDirectionMovementController.IsStateNon &&
            _playerSlidingController.IsStateNon &&
            !_playerForwardJumpingController.IsForwardJumping;

        CanFixedMovement = !_playerForwardJumpingController.IsForwardJumping &&
                           _playerLandingController.CanBeInterrupted &&
                           _playerSlidingController.IsStateNon &&
                           _playerMeleeController.State != MeleeController.MeleeStateEnum.Combo;
        
        CanAim = (
                     _playerFixedDirectionMovementController.IsStateNon ||
                     _playerFixedDirectionMovementController.State == FixedMovementStateEnum.Line
                  ) &&
                 _playerSlidingController.IsStateNon &&
                 !_playerDashController.IsDashing && 
                 !_playerForwardJumpingController.IsForwardJumping &&
                 _playerLandingController.CanBeInterrupted && 
                 _playerMeleeController.CanBeInterruptedToAnything &&
                 !_playerMeleeController.Parrying;

        CanJump = _playerLandingController.CanBeInterrupted &&
                  !_playerDashController.IsDashing &&
                  !_playerForwardJumpingController.IsForwardJumping &&
                  _playerSlidingController.State != PlayerSlidingController.StateEnum.Starting;
        
        CanHoldSword = _playerFixedDirectionMovementController.IsStateNon && 
                   !_playerForwardJumpingController.IsForwardJumping &&
                   _playerLandingController.CanBeInterrupted;
        
        CanNotUseSword = _playerSlidingController.IsActiveSlidingPhase;

        UseGroundPhysics = _playerSensors.IsGrounded && !_playerJumpController.ShouldSuppressGroundFriction;

        CanNotLanding = _playerSensors.IsForceSlide || _playerForwardJumpingController.IsForwardJumping || _playerMeleeTransformController.IsActive;

    }

    private void SetTargetMoveVector()
    {
        LookToLockedTarget = false;
        
        
        if (!_playerFixedDirectionMovementController.IsStateNon)
        {
            rb.useGravity = false;
            TargetMoveDirection = _playerFixedDirectionMovementController.GetMovementDirection();
            TargetMoveSpeed =  _playerFixedDirectionMovementController.State == FixedMovementStateEnum.Line ? 
                  railLineSpeed : wallRunSpeed;
        }
        else
        {
            rb.useGravity = true;
            if (_playerSensors.IsGrounded)
            {
                if (_playerJumpController.ShouldSuppressGroundFriction || _playerJumpController.State == PlayerJumpController.StateEnum.Starting)
                {
                    MakeFrictionTargetSpeed(0, 0, groundSpeedHardCap);
                    MakeNonZeroTargetMoveDirection(0f);
                }
                else if (_playerSlidingController.IsActiveSlidingPhase)
                {
                    if (_playerSensors.IsForceSlide)
                    {
                        
                        TargetMoveDirection = _playerInputController.NonZeroInputMoveVector.normalized;

                        if (_playerSensors.FoundGroundNormal)
                        {
                            var horizontalNormal = new Vector3(_playerSensors.GroundNormal.x, 0f,
                                _playerSensors.GroundNormal.z);
                            if (horizontalNormal.sqrMagnitude > 0.01f)
                            {
                                horizontalNormal = horizontalNormal.normalized;
                                var curAngle = Vector3.SignedAngle(horizontalNormal, TargetMoveDirection, Vector3.up);
                                var clampedAngle = Mathf.Clamp(curAngle, -_playerSlidingController.MaxAngleFromDown,
                                    _playerSlidingController.MaxAngleFromDown);
                                TargetMoveDirection = Quaternion.AngleAxis(clampedAngle, Vector3.up) * horizontalNormal;
                            }
                        }
                        
                        MakeFrictionTargetSpeed(slideFriction, slideDrag,
                            _playerSlidingController.SlideSpeedHardCap, _playerSlidingController.MinForceSlideSpeed);

                    }
                    else if (_playerSensors.HasSomethingInTheCollider)
                    {
                        MakeFrictionTargetSpeed(slideFriction, slideDrag,
                            _playerSlidingController.SlideSpeedHardCap,
                            _playerSlidingController.MinSlideSpeedIfCanNotStandUp);
                        MakeNonZeroTargetMoveDirection(slideAngleSpeed);
                    }
                    else
                    {
                        MakeFrictionTargetSpeed(slideFriction, slideDrag, _playerSlidingController.SlideSpeedHardCap);
                        MakeNonZeroTargetMoveDirection(slideAngleSpeed);
                    }

                }
                else if (_playerLandingController.LandingState == 3)
                {
                    MakeFrictionTargetSpeed(landingRollFriction, landingRollDrag, groundSpeedHardCap, _playerLandingController.RollLandingMinimalSpeed);
                    MakeNonZeroTargetMoveDirection(landingRollAngleSpeed);
                }
                else if (_playerForwardJumpingController.IsLanding)
                {
                    MakeFrictionTargetSpeed(forwardJumpLandingFriction, forwardJumpLandingDrag, groundSpeedHardCap);
                    MakeNonZeroTargetMoveDirection(forwardJumpLandingAngleSpeed);
                }
                else
                {
                    TargetMoveDirection = _playerInputController.InputMoveVector;
                    TargetMoveSpeed = groundSpeed;
                    if(_playerJumpController.IsStateNon && _playerLandingController.IsStateNon && _playerSlidingController.IsStateNon)
                        LookToLockedTarget = _playerTargetLockController.IsLocked;
                    //Debug.Log("Ground " + counter++);
                }
            }
            else
            {
                TargetMoveDirection = _playerInputController.InputMoveVector;
                TargetMoveSpeed = airSpeed;
            }
        }
        
        

        TargetMoveDirection = TargetMoveDirection.normalized;
    }

    private void MakeFrictionTargetSpeed(float friction, float drag, float maximalSpeed = Mathf.Infinity, float minimalSpeed = 0)
    {
        var currentSpeed = _playerSensors.SpeedAlignedWithGround;
        
        TargetMoveSpeed = Mathf.Clamp( Mathf.MoveTowards(currentSpeed, 0, (friction + drag*currentSpeed*currentSpeed)*Time.deltaTime ), minimalSpeed,maximalSpeed);
    }

    private void MakeNonZeroTargetMoveDirection(float angleSpeed)
    {
        TargetMoveDirection = Vector3.RotateTowards(TargetMoveDirection, _playerInputController.NonZeroInputMoveVector, angleSpeed*Time.deltaTime, 1f);
    }
}
