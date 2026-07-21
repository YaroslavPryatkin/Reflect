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
    private PlayerTargetMoveVectorUser _playerTargetMoveVectorUser;
    private GettingHitController _gettingHitController;
    private PlayerFixedDirectionMovementController _playerFixedDirectionMovementController;
    private PlayerSlidingController _playerSlidingController;
    private PlayerMeleeController _playerMeleeController;
    private PlayerTargetLockController _playerTargetLockController;
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        _playerSensors = GetComponent<PlayerSensors>();
        _playerInputController = GetComponent<PlayerInputController>();
        _playerDashController = GetComponent<PlayerDashController>();
        _playerForwardJumpingController = GetComponent<PlayerForwardJumpingController>();
        _playerTargetMoveVectorUser = GetComponent<PlayerTargetMoveVectorUser>();
        _playerJumpController = GetComponent<PlayerJumpController>();
        _playerLandingController = GetComponent<PlayerLandingController>();
        _gettingHitController = GetComponent<GettingHitController>();
        _playerFixedDirectionMovementController = GetComponent<PlayerFixedDirectionMovementController>();
        _playerSlidingController = GetComponent<PlayerSlidingController>();
        _playerMeleeController =  GetComponent<PlayerMeleeController>();
        _playerTargetLockController = GetComponent<PlayerTargetLockController>();
    }

    public Vector3 TargetMoveDirection { get; private set; } = Vector3.zero;
    public float TargetMoveSpeed { get; private set; } = 0f;

    public bool CanSlide { get; private set; } = false;
    public bool CanGetStunned { get; private set; } = false;
    public bool CanFixedMovement { get; private set; } = false;
    public bool CanAim { get; private set; } = false;
    public bool CanJump { get; private set; } = false;
    public bool CanHoldSword { get; private set; } = false;
    public bool CanUseSword { get; private set; } = false;
    public bool UseGroundPhysics { get;private set; } = false;
    public bool LookToLockedTarget { get; private set; } = false;

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
            _playerSlidingController.IsStateNon;

        CanFixedMovement = !_playerForwardJumpingController.IsForwardJumping &&
                           _playerLandingController.CanBeInterrupted &&
                           _playerSlidingController.IsStateNon;
        
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
        
        CanUseSword = _playerSlidingController.IsActiveSlidingPhase;

        UseGroundPhysics = _playerSensors.IsGrounded && !_playerJumpController.ShouldSuppressGroundFriction;

        
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
                if (_playerJumpController.ShouldSuppressGroundFriction)
                {
                    MakeSlidingTargetMoveVector(0, 0,0, groundSpeedHardCap);
                }
                else if (_playerSlidingController.IsActiveSlidingPhase)
                {
                    if (_playerSensors.HasSomethingInTheCollider)
                        MakeSlidingTargetMoveVector(slideFriction, slideDrag, slideAngleSpeed,
                            _playerSlidingController.SlideSpeedHardCap, _playerSlidingController.MinSlideSpeedIfCanNotStandUp);
                    else
                        MakeSlidingTargetMoveVector(slideFriction, slideDrag, slideAngleSpeed, _playerSlidingController.SlideSpeedHardCap);
                }
                else if (_playerLandingController.LandingState == 3)
                {
                    MakeSlidingTargetMoveVector(landingRollFriction, landingRollDrag, landingRollAngleSpeed, groundSpeedHardCap, _playerLandingController.RollLandingMinimalSpeed);
                }
                else if (_playerForwardJumpingController.IsLanding)
                {
                    MakeSlidingTargetMoveVector(forwardJumpLandingFriction, forwardJumpLandingDrag, forwardJumpLandingAngleSpeed, groundSpeedHardCap);
                }
                else
                {
                    TargetMoveDirection = _playerInputController.InputMoveVector;
                    TargetMoveSpeed = groundSpeed;
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

    private void MakeSlidingTargetMoveVector(float friction, float drag, float angleSpeed, float maximalSpeed = Mathf.Infinity, float minimalSpeed = 0)
    {
        var currentSpeed = _playerSensors.SpeedAlignedWithGround;
        TargetMoveSpeed = Mathf.Clamp( Mathf.MoveTowards(currentSpeed, 0, (friction + drag*currentSpeed*currentSpeed)*Time.deltaTime ), minimalSpeed,maximalSpeed);
        TargetMoveDirection = Vector3.RotateTowards(TargetMoveDirection, _playerInputController.NonZeroInputMoveVector, angleSpeed*Time.deltaTime, 0.0f);
    }
}
