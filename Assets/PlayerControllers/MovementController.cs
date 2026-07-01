using UnityEngine;
using Vector3 = UnityEngine.Vector3;


public class MovementController : MonoBehaviour
{
// ── Movement ──────────────────────────────────────────────
    [Header("Movement — Speeds")]
    [SerializeField] private float groundSpeed = 6f;
    [SerializeField] private float airSpeed = 1f;
    [SerializeField] private float wallRunSpeed = 9f;

    [Header("Speed Loss")] 
    [SerializeField] private float rollFriction = 0.1f;
    [SerializeField] private float rollDrag = 0f;
    [SerializeField] private float slideFriction = 0.1f;
    [SerializeField] private float slideDrag = 0f;
    [SerializeField] private float forwardJumpLandingFriction = 0.1f;
    [SerializeField] private float forwardJumpLandingDrag = 0f;
    

    [Header("Jumping Strength")]
    [SerializeField] private float jumpStrength = 5f;
    [SerializeField] private float wallRunJumpUpStrength = 25f;
    [SerializeField] private float wallRunJumpSideStrength = 30f;
    [SerializeField] private float wallRunJumpBackwardStrength = 10f;
    [Header("Jumping Other")]
    [SerializeField] private float wallRunJumpBackwardStrengthThreshold = 5f;
    [SerializeField] private float coyoteTime = 0.3f;
    [SerializeField] private float jumpRechargeTime = 0.5f;
    [SerializeField] private float groundSpeedSuppressAfterJump = 0.1f;
    [SerializeField] private float maximalJumpDuration = 0.7f;

    
    [Header("Wall Running")]
    [SerializeField] private float startingWallRunSpeedThreshold = 0.5f;
    [SerializeField] private float endingWallRunSpeedThreshold = 2f;
    [SerializeField] private float angleToStartWallRun = 0.1f;
    [SerializeField] private float angleToFinishWallRun = 0.2f;
    [SerializeField] private float canNotWallRunAfterWallRunDelay = 0.5f;
    [SerializeField] private float canNotWallRunAfterJumpingFromGroundDelay = 0.15f;



    [Header("Rolling")] 
    [SerializeField] private float rollDuration = 0.4f;
    [SerializeField] private float rollSpeedThreshold = 3f;

    [Header("Sliding")] 
    [SerializeField] private float slideSpeedGain = 4f;
    [SerializeField] private float slideSpeedGainMaxSpeed = 14f;
    [SerializeField] private float slideSpeedThreshold = 3f;
    [SerializeField] private float minimalSlideTime = 0.5f;
    [SerializeField] private float slideRechargeTime = 0.3f;
    [SerializeField] private float startingSlideTime = 0.1f;
    [SerializeField] private float endingSlideTime = 0.1f;
    

    
    
    private PlayerSensors _playerSensors;
    private PlayerMovementInputController _playerMovementInputController;
    private AnimationController _animationController;
    private PlayerDashController _playerDashController;
    private PlayerForwardJumpingController _playerForwardJumpingController;
    private TargetMoveVectorUser _targetMoveVectorUser;
    private Rigidbody rb;

    
    private Utility.TemporaryValue<bool> rolling = new(false, true);
    private Utility.ValueTimer<int> wallRunning = 0; // 0 - not, 1 - on right wall, -1 - on left wall
    private bool shouldStopWallrunBecouseOfSpeed = true;
    private Utility.FractionValueTimer<int> slidingPhase = 0; // 0 - not sliding, 1 - starting, 2 - in process, 3 - ending
    private Utility.TemporaryValue<bool> groundCheckSuppress = new(false, true);
    private Utility.DelayDurationValueTimer<int> canJump = 0; // 0 - not, 1 - from ground, 2 - from wall
    

    /// <summary>
    /// 0 - not, 1 - on right wall, -1 - on left wall
    /// </summary>
    public int WallRunningState => wallRunning.Value;
    public bool IsRolling => rolling.Value;

    
    public Vector3 TargetMoveVector { get; private set; } = Vector3.zero;
    
    public Vector3 wallRunNormal { get; private set; } = Vector3.zero;
    public Vector3 wallRunPoint { get; private set; } = Vector3.zero;
    
    public int SlidingPhase => slidingPhase.Value;
    public float SlidingPhaseFraction => slidingPhase.TimeFraction;
    
    public float StartingSlideTime => startingSlideTime;
    public float EndingSlideTime => endingSlideTime;
    
    public float MaximalJumpDuration =>  maximalJumpDuration;

    public bool IsHighJump { get;  set; } = false;
    
    public float RollDuration => rollDuration;
    
    
    
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        _playerSensors = GetComponent<PlayerSensors>();
        _playerMovementInputController = GetComponent<PlayerMovementInputController>();
        _animationController = GetComponent<AnimationController>();
        _playerDashController = GetComponent<PlayerDashController>();
        _playerForwardJumpingController = GetComponent<PlayerForwardJumpingController>();
        _targetMoveVectorUser = GetComponent<TargetMoveVectorUser>();
    }



    private void Update()
    {
        //Debug.Log("Rolling = " + IsRolling + ", Dashing = " + _playerDashController.IsDashing + ",  WallRunning = " + wallRunning.Value + ", forward jumping in air = " + _playerForwardJumpingController.IsInAir + ", forward jumping landing = " + _playerForwardJumpingController.IsLanding);
        ChangeRolling();

        ChangeSlide();
        
        ChangeIsHighJump();
        
        ChangeCanJump();
        
        //Debug.Log("wall running = " + (int)wallRunning + ", can be changed = " + wallRunning.CanBeChanged+", is left wall run = " + playerSensors.IsLeftWallRun + ", is on ground = " + playerSensors.IsGrounded);
        PerformJump();
        
        if(!_playerForwardJumpingController.IsForwardJumping && !IsRolling && slidingPhase == 0)
            HandleWallRunLogic();
        
        SetTargetMoveVector();

    }

    private void ChangeSlide()
    {
        if (!_playerSensors.IsGrounded || 
            IsRolling || _playerDashController.IsDashing || _playerForwardJumpingController.IsForwardJumping)
        {
            if (slidingPhase != 0)
            {
                //Debug.Log("Interrupt slide");
                slidingPhase.SetForce(0, slideRechargeTime);
            }
            
            // Debug.Log("Sliding phase = " +  slidingPhase.Value + 
            //           ", slide pressed = " + _playerMovementInputController.IsSlidePressed + ", branch 0");
        }
        else if (slidingPhase==3)
        {
            if (slidingPhase.CanBeChanged)
            {
                //Debug.Log("Finishing slide");
                slidingPhase.SetForce(0, slideRechargeTime);
            }
            // Debug.Log("Sliding phase = " +  slidingPhase.Value + 
            //           ", slide pressed = " + _playerMovementInputController.IsSlidePressed + ", branch 1");
        }
        else if ((slidingPhase == 1 || slidingPhase == 2) && _playerSensors.HorizontalSpeed < slideSpeedThreshold)
        {
            //Debug.Log("Ending slide becouse of speed");
            slidingPhase.SetForce(3, endingSlideTime);
            // Debug.Log("Sliding phase = " +  slidingPhase.Value + 
            //           ", slide pressed = " + _playerMovementInputController.IsSlidePressed + ", branch 2");
        }
        else if(_playerMovementInputController.IsSlidePressed && slidingPhase.CanBeChanged && 
                _playerSensors.HorizontalSpeed >= slideSpeedThreshold)
        {
            if (slidingPhase == 0)
            {
                //Debug.Log("Starting slide");
                slidingPhase.SetForce(1, startingSlideTime);
                ApplySlideSpeedGain();
            }
            else if (slidingPhase == 1)
            {
                //Debug.Log("Continuing slide");
                slidingPhase.SetForce(2, minimalSlideTime);
            }

            // Debug.Log("Sliding phase = " +  slidingPhase.Value + 
            //           ", slide pressed = " + _playerMovementInputController.IsSlidePressed + ", branch 3");
        }
        else if (!_playerMovementInputController.IsSlidePressed && 
                 (slidingPhase == 1 || slidingPhase == 2) && slidingPhase.CanBeChanged )
        {
            //Debug.Log("Intentionally ending slide");
            slidingPhase.SetForce(3, endingSlideTime);
            
            // Debug.Log("Sliding phase = " +  slidingPhase.Value + 
            //           ", slide pressed = " + _playerMovementInputController.IsSlidePressed + ", branch 4");
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
    
    private void ChangeRolling()
    {
        if (IsHighJump && _playerSensors.IsFarGrounded && _playerSensors.HorizontalSpeed >= rollSpeedThreshold && 
            !_playerForwardJumpingController.IsForwardJumping && !IsRolling)
        {
            rolling.Activate(rollDuration);
        }
    }

    private void ChangeIsHighJump()
    {
        if (_playerSensors.IsGrounded || _playerForwardJumpingController.IsForwardJumping || 
            IsRolling || _playerDashController.IsDashing)
            IsHighJump = false;
        
        if(!_playerSensors.IsFarGrounded)
            IsHighJump = true;
    }
    
    private void ChangeCanJump()
    {
        if (IsRolling || _playerDashController.IsDashing || _playerForwardJumpingController.IsForwardJumping || slidingPhase == 1)
        {
            if(canJump != 0)
                canJump.SetForce(0,0,0);
        }
        else if (wallRunning != 0 && canJump.CanBeChanged && !IsRolling)
        {
            if (canJump != 2)
                canJump.SetForce(2, 0, 0);
        }
        else if (_playerSensors.IsGrounded && canJump.CanBeChanged && !IsRolling)
        {
            if (canJump != 1)
                canJump.SetForce(1, 0, 0);
        }
        else if(canJump.RealValue != 0)
            canJump.SetForce(0, coyoteTime, 0);
    }
    

    
    private void PerformJump()
    {
        if (!_playerMovementInputController.IsJumpPressed) return;
        
        if (canJump == 2)
        {
            //Debug.Log("Wall jump");
            _animationController.HandleJump();
            rb.linearVelocity = _playerSensors.HorizontalVelocity;
            
            var force = Vector3.up * wallRunJumpUpStrength + wallRunNormal * wallRunJumpSideStrength;
            if(_playerSensors.HorizontalSpeed >= wallRunJumpBackwardStrengthThreshold)
                force -= _playerSensors.NormalizedHorizontalVelocity * wallRunJumpBackwardStrength;
            
            rb.AddForce( force, ForceMode.Impulse);
            
            
            _playerSensors.UpdateVelocity();
            wallRunning.SetForce(0, canNotWallRunAfterWallRunDelay);
            canJump.SetForce(0, 0, jumpRechargeTime);
            _playerMovementInputController.ResetJumpBuffer();
        }
        else if (canJump == 1)
        {
            if (_playerSensors.FrontGroundState == 1 && _playerSensors.HorizontalSpeed >= _playerForwardJumpingController.MinSpeedToTrigger)
            {
                var endPos = new Vector3(_playerSensors.FrontGroundPoint.x, _playerSensors.FrontGroundHeight + transform.position.y,
                    _playerSensors.FrontGroundPoint.z);
                
                _playerForwardJumpingController.PerformForwardJump(transform.position, endPos);
            }
            else
            {
                _animationController.HandleJump();
                rb.linearVelocity = _playerSensors.HorizontalVelocity;
                _playerSensors.UpdateVelocity();
                rb.AddForce(Vector3.up * jumpStrength, ForceMode.Impulse);

                wallRunning.SetForce(0, canNotWallRunAfterJumpingFromGroundDelay);
                canJump.SetForce(0, 0, jumpRechargeTime);
                groundCheckSuppress.Activate(groundSpeedSuppressAfterJump);
            }
            _playerMovementInputController.ResetJumpBuffer();
        }
    }
    



    
    
    
    private void HandleWallRunLogic()
    {
        var inputMoveVector = _playerMovementInputController.InputMoveVector;
        var currentSpeed = _playerSensors.HorizontalSpeed;
        
        if (!shouldStopWallrunBecouseOfSpeed && currentSpeed >= endingWallRunSpeedThreshold)
            shouldStopWallrunBecouseOfSpeed = true;
        
        if (wallRunning == 0 && wallRunning.CanBeChanged && currentSpeed >= startingWallRunSpeedThreshold) //not wall running and ready to start
        {
            //Debug.Log("Is left wall run = " + playerSensors.IsLeftWallRun + ", dot product = " +
                     // Vector3.Dot(inputMoveVector, playerSensors.LeftWallRunNormal));
            if (_playerSensors.IsLeftWallRun && 
                Vector3.Dot(inputMoveVector, _playerSensors.LeftWallRunNormal) < angleToStartWallRun)
            {
                shouldStopWallrunBecouseOfSpeed = false;
                wallRunning.SetForce(-1, 0);
                wallRunNormal = _playerSensors.LeftWallRunNormal;
                wallRunPoint = _playerSensors.LeftWallRunPoint;
                _targetMoveVectorUser.SnapToWall();
            }
            else if (_playerSensors.IsRightWallRun &&
                     Vector3.Dot(inputMoveVector, _playerSensors.RightWallRunNormal) < angleToStartWallRun)
            {
                shouldStopWallrunBecouseOfSpeed = false;
                wallRunning.SetForce(1, 0);
                wallRunNormal = _playerSensors.RightWallRunNormal;
                wallRunPoint = _playerSensors.RightWallRunPoint;
                _targetMoveVectorUser.SnapToWall();
            }
        }
        else if (wallRunning == 1) //right wall running
        {
            if (!_playerSensors.IsRightWallRun)
            {
                wallRunning.SetForce(0, 0);
                //Debug.Log("lose wall");
            }
            else if (Vector3.Dot(inputMoveVector, _playerSensors.RightWallRunNormal) >= angleToFinishWallRun)
            {
                wallRunning.SetForce(0, canNotWallRunAfterWallRunDelay);
                //Debug.Log("turn back");
            }
            else if (shouldStopWallrunBecouseOfSpeed && currentSpeed < endingWallRunSpeedThreshold)
            {
                //Debug.Log("lose speed");
                wallRunning.SetForce(0, canNotWallRunAfterWallRunDelay);
            }
            else
            {
                wallRunNormal = _playerSensors.RightWallRunNormal;
                wallRunPoint = _playerSensors.RightWallRunPoint;
            }
        }
        else if (wallRunning == -1)//left wall running
        {
            if (!_playerSensors.IsLeftWallRun)
            {
                wallRunning.SetForce(0, 0);
            }
            else if (Vector3.Dot(inputMoveVector, _playerSensors.LeftWallRunNormal) >= angleToFinishWallRun)
            {
                wallRunning.SetForce(0, canNotWallRunAfterWallRunDelay);
            }
            else if (shouldStopWallrunBecouseOfSpeed && currentSpeed < endingWallRunSpeedThreshold)
            {
                wallRunning.SetForce(0, canNotWallRunAfterWallRunDelay);
            }
            else
            {
                wallRunNormal = _playerSensors.LeftWallRunNormal;
                wallRunPoint = _playerSensors.LeftWallRunPoint;
            }
        }
    }

    private void SetTargetMoveVector()
    {
        if (wallRunning == 0)
        {
            rb.useGravity = true;
            if (_playerSensors.IsGrounded)
            {
                if (groundCheckSuppress)
                {
                    MakeSlidingTargetMoveVector(0, 0);
                }
                else if (slidingPhase == 1 || slidingPhase == 2)
                {
                    MakeSlidingTargetMoveVector(slideFriction, slideDrag);
                    //Debug.Log("Sliding new speed = " + TargetMoveVector.magnitude);
                }
                else if (IsRolling && _playerMovementInputController.IsPlayerPressingWASD)
                {
                    MakeSlidingTargetMoveVector(rollFriction, rollDrag);
                }
                else if (_playerForwardJumpingController.IsLanding)
                {
                    MakeSlidingTargetMoveVector(forwardJumpLandingFriction, forwardJumpLandingDrag);
                }
                else
                {
                    TargetMoveVector = _playerMovementInputController.InputMoveVector * groundSpeed;
                    //Debug.Log("Ground " + counter++);
                }
            }
            else
                TargetMoveVector = _playerMovementInputController.InputMoveVector * airSpeed;
        }
        else
        {
            rb.useGravity = false;
            TargetMoveVector =  Vector3.Cross(Vector3.up, wallRunNormal * (int)wallRunning).normalized * wallRunSpeed;
        }
    }

    private void MakeSlidingTargetMoveVector(float friction, float drag)
    {
        var currentSpeed = _playerSensors.HorizontalSpeed;
        var newSpeed = Mathf.MoveTowards(currentSpeed, 0, (friction + drag*currentSpeed*currentSpeed)*Time.deltaTime );
        TargetMoveVector = _playerMovementInputController.LastNonZeroInputMoveVector * newSpeed;
    }
    
    
    

}