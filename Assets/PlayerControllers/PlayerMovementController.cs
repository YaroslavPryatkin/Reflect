using UnityEngine;
using Vector3 = UnityEngine.Vector3;

[DefaultExecutionOrder(50)]
public class PlayerMovementController : MonoBehaviour
{
// ── Movement ──────────────────────────────────────────────
    [Header("Movement — Speeds")]
    [SerializeField] private float groundSpeed = 6f;
    [SerializeField] private float airSpeed = 1f;
    [SerializeField] private float wallRunSpeed = 9f;
    [SerializeField] private float groundSpeedHardCap = 25f;

    [Header("Speed Loss")] 
    [SerializeField] private float slideFriction = 0.1f;
    [SerializeField] private float slideDrag = 0f;
    [SerializeField] private float slideAngleSpeed = 0.1f;
    [SerializeField] private float forwardJumpLandingFriction = 0.1f;
    [SerializeField] private float forwardJumpLandingDrag = 0f;
    [SerializeField] private float forwardJumpLandingAngleSpeed = 0.1f;
    [SerializeField] private float landingRollFriction = 0.1f;
    [SerializeField] private float landingRollDrag = 0f;
    [SerializeField] private float landingRollAngleSpeed = 0.1f;
    
    [Header("Wall Running")]
    [SerializeField] private float startingWallRunSpeedThreshold = 0.5f;
    [SerializeField] private float endingWallRunSpeedThreshold = 2f;
    [SerializeField] private float angleToStartWallRun = 0.1f;
    [SerializeField] private float angleToFinishWallRun = 0.2f;
    
    [Header("Delays after jump")]
    [SerializeField] private float canNotWallRunAfterWallRunDelay = 0.5f;
    [SerializeField] private float canNotWallRunAfterJumpingFromGroundDelay = 0.15f;
    [SerializeField] private float groundSpeedSuppressAfterJump = 0.1f;

    [Header("Sliding")] 
    [SerializeField] private float slideSpeedHardCap = 20f;
    [SerializeField] private float slideSpeedGain = 4f;
    [SerializeField] private float slideSpeedGainMaxSpeed = 14f;
    [SerializeField] private float minSlideSpeedIfCanNotStandUp = 4f;
    [SerializeField] private float slideSpeedThreshold = 3f;
    [SerializeField] private float minimalSlideTime = 0.5f;
    [SerializeField] private float slideRechargeTime = 0.3f;
    [SerializeField] private float startingSlideTime = 0.1f;
    [SerializeField] private float endingSlideTime = 0.1f;
    

    
    
    private PlayerSensors _playerSensors;
    private PlayerInputController _playerInputController;
    private PlayerDashController _playerDashController;
    private PlayerForwardJumpingController _playerForwardJumpingController;
    private PlayerJumpController _playerJumpController;
    private PlayerLandingController _playerLandingController;
    private PlayerTargetMoveVectorUser _playerTargetMoveVectorUser;
    private Rigidbody rb;
    
    /// <summary>
    /// 0 - not, 1 - on right wall, -1 - on left wall
    /// </summary>
    private Utility.BlockingValueTimer<int> wallRunningState = 0;
    private bool shouldStopWallrunBecouseOfSpeed = true;
    
    
    /// <summary>
    /// 0 - not sliding, 1 - starting, 2 - in process, 3 - ending
    /// </summary>
    private Utility.FractionBlockingValueTimer<int> slidingPhase = 0; 
    private Utility.TemporaryValue<bool> jumpingFromGround = new(false, true);
    public bool JumpingFromGround => jumpingFromGround.Value;

    /// <summary>
    /// 0 - not, 1 - on right wall, -1 - on left wall
    /// </summary>
    public int WallRunningState => wallRunningState.Value;
    public Vector3 LastWallRunNormal { get; private set; } = Vector3.zero;
    public Vector3 LastWallRunPoint { get; private set; } = Vector3.zero;
    
    /// <summary>
    /// 0 - not sliding, 1 - starting, 2 - in process, 3 - ending
    /// </summary>
    public int SlidingPhase => slidingPhase.Value;
    public float SlidingPhaseFraction => slidingPhase.TimeFraction;
    public float StartingSlideTime => startingSlideTime;
    public float EndingSlideTime => endingSlideTime;

    public bool IsActiveSlidingPhase => slidingPhase.Value == 1 || slidingPhase.Value == 2;
    
    public Vector3 TargetMoveHorizontalDirection { get; private set; } = Vector3.zero;
    public float TargetMoveSpeed { get; private set; } = 0f;
    
    public bool CanLookToLockedTarget { get; private set; } = false;
    
    
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
    }



    private void Update()
    {
        //Debug.Log("Rolling = " + IsRolling + ", Dashing = " + _playerDashController.IsDashing + ",  WallRunning = " + wallRunning.Value + ", forward jumping in air = " + _playerForwardJumpingController.IsInAir + ", forward jumping landing = " + _playerForwardJumpingController.IsLanding);

        _playerLandingController.ToCallFromMovementController();
        
        ChangeSlide();
        
        HandleWallRunLogic();
        
        SetTargetMoveVector();

    }

    private void ChangeSlide()
    {
        if (!_playerSensors.IsGrounded || 
            !_playerLandingController.CanBeInterrupted || 
            _playerDashController.IsDashing || 
            _playerForwardJumpingController.IsForwardJumping)
        {
            if (slidingPhase != 0)
            {
                slidingPhase.SetForce(0, slideRechargeTime);
            }
        }
        else if (IsActiveSlidingPhase && _playerSensors.HorizontalSpeed < slideSpeedThreshold && !_playerSensors.HasSomethingInTheCollider)
        {
            slidingPhase.SetForce(3, endingSlideTime);
        }
        else
        {
            if (slidingPhase.CanBeChanged)
            {
                switch (slidingPhase.Value)
                {
                    case 0:
                        if (_playerInputController.IsSlidePressed && _playerSensors.HorizontalSpeed >= slideSpeedThreshold)
                        {
                            _playerLandingController.InterruptLanding();
                            slidingPhase.SetForce(1, startingSlideTime);
                            ApplySlideSpeedGain();
                        }
                        break;
                    case 1:
                        if (_playerInputController.IsSlidePressed || _playerSensors.HasSomethingInTheCollider)
                        {
                            slidingPhase.SetForce(2, minimalSlideTime);
                        }
                        else
                        {
                            slidingPhase.SetForce(3, endingSlideTime);
                        }
                        break;
                    case 2:
                        if (!_playerInputController.IsSlidePressed && !_playerSensors.HasSomethingInTheCollider)
                        {
                            slidingPhase.SetForce(3, endingSlideTime);
                        }
                        break;
                    case 3:
                        slidingPhase.SetForce(0, slideRechargeTime);
                        break;
                }
            }
        }
    }
    
    private void ApplySlideSpeedGain()
    {
        float currentSpeed = _playerSensors.HorizontalSpeed;
        
        if (currentSpeed < slideSpeedGainMaxSpeed)
        {
            float newSpeed = Mathf.Min(currentSpeed + slideSpeedGain, slideSpeedGainMaxSpeed);
            
            var currentDir = _playerSensors.NormalizedHorizontalVelocity;
            
            rb.linearVelocity = new Vector3(currentDir.x * newSpeed, _playerSensors.Velocity.y, currentDir.z * newSpeed);
        }
    }





    public void ChangeFlagsAfterJumpFromWall()
    {
        wallRunningState.SetForce(0, canNotWallRunAfterWallRunDelay);
    }


    public void ChangeFlagsAfterJumpFromGround()
    {
        wallRunningState.SetForce(0, canNotWallRunAfterJumpingFromGroundDelay);
        jumpingFromGround.Activate(groundSpeedSuppressAfterJump);
    }
    
    
    
    private void HandleWallRunLogic()
    {
        if (_playerForwardJumpingController.IsForwardJumping || _playerLandingController.LandingState != 0 ||
            slidingPhase != 0)
        {
            wallRunningState.SetForce(0, 0);
            return;
        }
        
        
        var inputMoveVector = _playerInputController.InputMoveVector;
        var currentSpeed = _playerSensors.HorizontalSpeed;
        
        if (currentSpeed >= endingWallRunSpeedThreshold)
            shouldStopWallrunBecouseOfSpeed = true;
        
        if (wallRunningState == 0 && wallRunningState.CanBeChanged && currentSpeed >= startingWallRunSpeedThreshold) //not wall running and ready to start
        {
            //Debug.Log("Is left wall run = " + playerSensors.IsLeftWallRun + ", dot product = " +
                     // Vector3.Dot(inputMoveVector, playerSensors.LeftWallRunNormal));
            if (_playerSensors.IsLeftWallRun && 
                Vector3.Dot(inputMoveVector, _playerSensors.LeftWallRunNormal) < angleToStartWallRun)
            {
                shouldStopWallrunBecouseOfSpeed = false;
                wallRunningState.SetForce(-1, 0);
                LastWallRunNormal = _playerSensors.LeftWallRunNormal;
                LastWallRunPoint = _playerSensors.LeftWallRunPoint;
                _playerTargetMoveVectorUser.SnapToWall();
            }
            else if (_playerSensors.IsRightWallRun &&
                     Vector3.Dot(inputMoveVector, _playerSensors.RightWallRunNormal) < angleToStartWallRun)
            {
                shouldStopWallrunBecouseOfSpeed = false;
                wallRunningState.SetForce(1, 0);
                LastWallRunNormal = _playerSensors.RightWallRunNormal;
                LastWallRunPoint = _playerSensors.RightWallRunPoint;
                _playerTargetMoveVectorUser.SnapToWall();
            }
        }
        else if (wallRunningState == 1) //right wall running
        {
            if (!_playerSensors.IsRightWallRun)
            {
                wallRunningState.SetForce(0, 0);
                //Debug.Log("lose wall");
            }
            else if (Vector3.Dot(inputMoveVector, _playerSensors.RightWallRunNormal) >= angleToFinishWallRun)
            {
                wallRunningState.SetForce(0, canNotWallRunAfterWallRunDelay);
                //Debug.Log("turn back");
            }
            else if (shouldStopWallrunBecouseOfSpeed && currentSpeed < endingWallRunSpeedThreshold)
            {
                //Debug.Log("lose speed");
                wallRunningState.SetForce(0, canNotWallRunAfterWallRunDelay);
            }
            else
            {
                LastWallRunNormal = _playerSensors.RightWallRunNormal;
                LastWallRunPoint = _playerSensors.RightWallRunPoint;
            }
        }
        else if (wallRunningState == -1)//left wall running
        {
            if (!_playerSensors.IsLeftWallRun)
            {
                wallRunningState.SetForce(0, 0);
            }
            else if (Vector3.Dot(inputMoveVector, _playerSensors.LeftWallRunNormal) >= angleToFinishWallRun)
            {
                wallRunningState.SetForce(0, canNotWallRunAfterWallRunDelay);
            }
            else if (shouldStopWallrunBecouseOfSpeed && currentSpeed < endingWallRunSpeedThreshold)
            {
                wallRunningState.SetForce(0, canNotWallRunAfterWallRunDelay);
            }
            else
            {
                LastWallRunNormal = _playerSensors.LeftWallRunNormal;
                LastWallRunPoint = _playerSensors.LeftWallRunPoint;
            }
        }
    }

    private void SetTargetMoveVector()
    {
        CanLookToLockedTarget = false;
        if (wallRunningState == 0)
        {
            rb.useGravity = true;
            if (_playerSensors.IsGrounded)
            {
                if (jumpingFromGround)
                {
                    MakeSlidingTargetMoveVector(0, 0,0, groundSpeedHardCap);
                }
                else if (IsActiveSlidingPhase)
                {
                    if (_playerSensors.HasSomethingInTheCollider)
                        MakeSlidingTargetMoveVector(slideFriction, slideDrag, slideAngleSpeed,
                            slideSpeedHardCap, minSlideSpeedIfCanNotStandUp);
                    else
                        MakeSlidingTargetMoveVector(slideFriction, slideDrag, slideAngleSpeed, slideSpeedHardCap);
                }
                else if (_playerLandingController.LandingState == 3)
                {
                    MakeSlidingTargetMoveVector(landingRollFriction, landingRollDrag, landingRollAngleSpeed, groundSpeedHardCap, _playerLandingController.RollLandingMinimalSpeed);
                }
                else if (_playerForwardJumpingController.IsLanding)
                {
                    MakeSlidingTargetMoveVector(forwardJumpLandingFriction, forwardJumpLandingDrag, forwardJumpLandingAngleSpeed, groundSpeedHardCap);
                }
                else if (_playerJumpController.StartingJump)
                {
                    MakeSlidingTargetMoveVector(0, 0, 1, groundSpeedHardCap);
                }
                else
                {
                    TargetMoveHorizontalDirection = _playerInputController.InputMoveVector;
                    TargetMoveSpeed = groundSpeed;
                    CanLookToLockedTarget = true;
                    //Debug.Log("Ground " + counter++);
                }
            }
            else
            {
                TargetMoveHorizontalDirection = _playerInputController.InputMoveVector;
                TargetMoveSpeed = airSpeed;
                CanLookToLockedTarget = true;
            }
        }
        else
        {
            rb.useGravity = false;
            TargetMoveHorizontalDirection =  Vector3.Cross(Vector3.up, LastWallRunNormal * (int)wallRunningState);
            TargetMoveSpeed = wallRunSpeed;
        }

        TargetMoveHorizontalDirection = TargetMoveHorizontalDirection.normalized;
    }

    private void MakeSlidingTargetMoveVector(float friction, float drag, float angleSpeed, float maximalSpeed = Mathf.Infinity, float minimalSpeed = 0)
    {
        var currentSpeed = _playerSensors.SpeedAlignedWithGround;
        TargetMoveSpeed = Mathf.Clamp( Mathf.MoveTowards(currentSpeed, 0, (friction + drag*currentSpeed*currentSpeed)*Time.deltaTime ), minimalSpeed,maximalSpeed);
        TargetMoveHorizontalDirection = Vector3.RotateTowards(TargetMoveHorizontalDirection, _playerInputController.LastNonZeroInputMoveVector, angleSpeed*Time.deltaTime, 0.0f);
    }
    
    
    

}