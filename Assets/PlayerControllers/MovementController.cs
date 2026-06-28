using System;
using System.Numerics;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;


public class MovementController : MonoBehaviour
{
// ── Movement ──────────────────────────────────────────────
    [Header("Movement — Speeds")]
    [SerializeField] private float groundSpeed = 6f;
    [SerializeField] private float airSpeed = 1f;
    [SerializeField] private float wallRunSpeed = 9f;
    [SerializeField] private float groundAcceleration = 50f;
    [SerializeField] private float airAcceleration = 4f;
    [SerializeField] private float wallRunAcceleration = 100f;
    [SerializeField] private float additionalAirAccelerationIfAngleIsBig = 10f;

    [Header("Jumping")]
    [SerializeField] private float jumpStrength = 5f;
    [SerializeField] private float wallRunJumpUpStrength = 7f;
    [SerializeField] private float wallRunJumpSideStrength = 3f;
    [SerializeField] private float coyoteTime = 0.3f;
    [SerializeField] private float jumpRechargeTime = 0.5f;

    [Header("Wall Running")]
    [SerializeField] private float wallRunDistanceFromWall = 0.5f;
    [SerializeField] private float wallRunDistanceFromWallError = 0.1f;
    [SerializeField] private float wallRunRotationFromWallError = 0.2f;
    [SerializeField] private float minWallRunSpeedThreshold = 2f;
    [SerializeField] private float angleToStartWallRun = 0.1f;
    [SerializeField] private float angleToFinishWallRun = 0.2f;
    [SerializeField] private float canNotWallRunAfterWallRunDelay = 0.5f;
    [SerializeField] private float canNotWallRunAfterJumpingFromGroundDelay = 0.15f;

    [Header("Air Turning")]
    [SerializeField] private float airTurnDeadzone = 60f;
    [SerializeField] private float airTurnPow = 3f;
    [SerializeField] private float airTurnMaxLossPerFrame = 0.08f;

    [Header("Landing")] 
    [SerializeField] private float rollDuration = 0.4f;
    [SerializeField] private float minSpeedToRoll = 3f;
    
    [Header("Rotation / Physics")]
    [SerializeField] private float turnTorque = 30f;
    [SerializeField] private float maxPhysicsRotationSpeed = 20f;
    
    
    private PlayerSensors playerSensors;
    private PlayerMovementInputController _playerMovementInputController;
    private AnimationController animationController;
    private Rigidbody rb;

    private Vector3 targetMoveVector;
    

    private Utility.ValueTimer<int> wallRunning = 0; // 0 - not, 1 - on right wall, -1 - on left wall
    private Utility.TemporaryValue<bool> rolling = new(false, true);
    private Vector3 wallRunNormal = Vector3.zero;
    private Vector3 wallRunPoint = Vector3.zero;

    /// <summary>
    /// 0 - not, 1 - on right wall, -1 - on left wall
    /// </summary>
    public int WallRunningState => wallRunning.Value;
    public bool IsRolling => rolling.Value;

    public bool IsHighJump { get; private set; } = false;
    
    private Utility.DelayDurationValueTimer<int> canJump = 0; // 0 - not, 1 - from ground, 2 - from wall

    
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerSensors = GetComponent<PlayerSensors>();
        _playerMovementInputController = GetComponent<PlayerMovementInputController>();
        animationController = GetComponent<AnimationController>();
        
        rb.maxAngularVelocity = maxPhysicsRotationSpeed;
    }


    private void Update()
    {
        ChangeRolling();

        ChangeIsHighJump();
        
        ChangeCanJump();
        
        //Debug.Log("wall running = " + (int)wallRunning + ", can be changed = " + wallRunning.CanBeChanged+", is left wall run = " + playerSensors.IsLeftWallRun + ", is on ground = " + playerSensors.IsGrounded);
        PerformJump();

        HandleWallRunLogic();
        
        SetTargetMoveVector();

    }

    private void ChangeRolling()
    {
        //Debug.Log(rolling.Value);
        var x = rb.linearVelocity.x;
        var  z = rb.linearVelocity.z;
        if (IsHighJump && playerSensors.IsFarGrounded && x*x + z*z >= minSpeedToRoll* minSpeedToRoll)
        {
            rolling.Activate(rollDuration);
        }
    }

    private void ChangeIsHighJump()
    {
        if (playerSensors.IsGrounded)
            IsHighJump = false;
        
        if(!playerSensors.IsFarGrounded)
            IsHighJump = true;
    }
    
    private void ChangeCanJump()
    {
        if (IsRolling)
        {
            if(canJump != 0)
                canJump.SetForce(0,0,0);
        }
        else if (wallRunning != 0 && canJump.CanBeChanged && !IsRolling)
        {
            if (canJump != 2)
                canJump.SetForce(2, 0, 0);
        }
        else if (playerSensors.IsGrounded && canJump.CanBeChanged && !IsRolling)
        {
            if (canJump != 1)
                canJump.SetForce(1, 0, 0);
        }
        else if(canJump.RealValue != 0)
            canJump.SetForce(0, coyoteTime, 0);
    }
    

    private void PerformJump()
    {
        if (!_playerMovementInputController.IsJumpPressed()) return;
        
        if (canJump == 2)
        {
            animationController.HandleJump();
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * wallRunJumpUpStrength + wallRunNormal * wallRunJumpSideStrength,
                ForceMode.Impulse);

            wallRunning.SetForce(0, canNotWallRunAfterWallRunDelay);
            canJump.SetForce(0, 0, jumpRechargeTime);
            _playerMovementInputController.ResetJumpBuffer();
        }
        else if (canJump == 1)
        {
            animationController.HandleJump();
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpStrength, ForceMode.Impulse);

            wallRunning.SetForce(0, canNotWallRunAfterJumpingFromGroundDelay);
            canJump.SetForce(0, 0, jumpRechargeTime);
            _playerMovementInputController.ResetJumpBuffer();
        }
    }
    



    
    
    
    private void HandleWallRunLogic()
    {
        var inputMoveVector = _playerMovementInputController.InputMoveVector;
        if (wallRunning == 0 && wallRunning.CanBeChanged) //not wall running and ready to start
        {
            //Debug.Log("Is left wall run = " + playerSensors.IsLeftWallRun + ", dot product = " +
                     // Vector3.Dot(inputMoveVector, playerSensors.LeftWallRunNormal));
            if (playerSensors.IsLeftWallRun && 
                Vector3.Dot(inputMoveVector, playerSensors.LeftWallRunNormal) < angleToStartWallRun)
            {
                wallRunning.SetForce(-1, 0);
                wallRunNormal = playerSensors.LeftWallRunNormal;
                wallRunPoint = playerSensors.LeftWallRunPoint;
                //SnapToWall();
            }
            else if (playerSensors.IsRightWallRun &&
                     Vector3.Dot(inputMoveVector, playerSensors.RightWallRunNormal) < angleToStartWallRun)
            {
                wallRunning.SetForce(1, 0);
                wallRunNormal = playerSensors.RightWallRunNormal;
                wallRunPoint = playerSensors.RightWallRunPoint;
                //SnapToWall();
            }
        }
        else if (wallRunning == 1) //right wall running
        {
            if (!playerSensors.IsRightWallRun)
            {
                wallRunning.SetForce(0, 0);
            }
            else if (Vector3.Dot(inputMoveVector, playerSensors.RightWallRunNormal) >= angleToFinishWallRun)
            {
                wallRunning.SetForce(0, canNotWallRunAfterWallRunDelay);
            }
            else if (GetHorizontalSpeed() < minWallRunSpeedThreshold)
            {
                wallRunning.SetForce(0, canNotWallRunAfterWallRunDelay);
            }
            else
            {
                wallRunNormal = playerSensors.RightWallRunNormal;
                wallRunPoint = playerSensors.RightWallRunPoint;
            }
        }
        else if (wallRunning == -1)//left wall running
        {
            if (!playerSensors.IsLeftWallRun)
            {
                wallRunning.SetForce(0, 0);
            }
            else if (Vector3.Dot(inputMoveVector, playerSensors.LeftWallRunNormal) >= angleToFinishWallRun)
            {
                wallRunning.SetForce(0, canNotWallRunAfterWallRunDelay);
            }
            else if (GetHorizontalSpeed() < minWallRunSpeedThreshold)
            {
                wallRunning.SetForce(0, canNotWallRunAfterWallRunDelay);
            }
            else
            {
                wallRunNormal = playerSensors.LeftWallRunNormal;
                wallRunPoint = playerSensors.LeftWallRunPoint;
            }
        }
    }
    
    private void SnapToWall()
    {
        var dist = Vector3.Distance(transform.position, wallRunPoint);
        var maxError = wallRunDistanceFromWall * wallRunDistanceFromWallError;
        if (Mathf.Abs(dist - wallRunDistanceFromWall) > maxError)
        {
            //Debug.Log("Snapped position");
            transform.position = wallRunPoint + wallRunNormal * wallRunDistanceFromWall;
        }

        if (Vector3.Dot(rb.linearVelocity, targetMoveVector.normalized) < 1 - wallRunRotationFromWallError)
        {
            //Debug.Log("Snapped rotation");
            var projectedVelocity = Vector3.ProjectOnPlane(rb.linearVelocity, wallRunNormal);
            projectedVelocity.y = 0;
            rb.linearVelocity = projectedVelocity;
        }

        //Debug.Log("Snapped to wall, final speed = " + GetHorizontalSpeed());
 
    }

    private float GetHorizontalSpeed()
    {
        return new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z).magnitude;
    }

    
    private void SetTargetMoveVector()
    {
        if (wallRunning == 0)
        {
            rb.useGravity = true;
            if(playerSensors.IsGrounded)
                targetMoveVector = _playerMovementInputController.InputMoveVector * groundSpeed;
            else
                targetMoveVector = _playerMovementInputController.InputMoveVector * airSpeed;
        }
        else
        {
            rb.useGravity = false;
            targetMoveVector =  Vector3.Cross(Vector3.up, wallRunNormal * (int)wallRunning).normalized * wallRunSpeed;
        }
    }
    
    
    
    
    
    private void FixedUpdate()
    {
        ChangeVelocityVectorToMatchTargetMoveVector();
        ChangeLookDirection();
    }

    private void ChangeVelocityVectorToMatchTargetMoveVector()
    {
        Vector3 currentVelocity = rb.linearVelocity;
        Vector3 currentHorizontalVelocity = new Vector3(currentVelocity.x, 0f, currentVelocity.z);

        if (playerSensors.IsGrounded)
        {
            currentHorizontalVelocity = Vector3.MoveTowards(
                currentHorizontalVelocity,
                targetMoveVector,
                groundAcceleration * Time.fixedDeltaTime
            );
        }
        else if(wallRunning != 0)
        {
            SnapToWall();
            currentHorizontalVelocity = Vector3.MoveTowards(
                currentHorizontalVelocity,
                targetMoveVector,
                wallRunAcceleration * Time.fixedDeltaTime
            );
        }
        else    
        {
            if (targetMoveVector != Vector3.zero)
            {
                Vector3 targetDir = targetMoveVector.normalized;
                float targetSpeed = targetMoveVector.magnitude;
                float currentSpeed = currentHorizontalVelocity.magnitude;

                if (currentSpeed > 0.001f)
                {
                    Vector3 currentDir = currentHorizontalVelocity / currentSpeed;
                    
                    
                    float angleDeg = Vector3.Angle(currentDir, targetDir);
                    float t = Mathf.Max(0f, angleDeg - airTurnDeadzone) / (180f - airTurnDeadzone);
                    float fraction = Mathf.Pow(t, airTurnPow);
                    float speedRetention = 1f - airTurnMaxLossPerFrame * fraction;

                    currentHorizontalVelocity = 
                        Vector3.RotateTowards(
                            currentDir, 
                            targetDir, 
                            (airAcceleration + fraction * additionalAirAccelerationIfAngleIsBig) * Time.fixedDeltaTime, 0f
                            ) * 
                        (currentSpeed * speedRetention);
                    
                    float projectionOnTarget = Vector3.Dot(currentHorizontalVelocity, targetDir);
                    if (projectionOnTarget < targetSpeed)
                    {
                        float speedToAdd = Mathf.Min(
                            targetSpeed - projectionOnTarget,
                            airAcceleration * Time.fixedDeltaTime
                        );
                        currentHorizontalVelocity += targetDir * speedToAdd;
                    }

                    float maxAllowedSpeed = Mathf.Max(currentSpeed, targetSpeed);
                    if (currentHorizontalVelocity.magnitude > maxAllowedSpeed)
                        currentHorizontalVelocity = currentHorizontalVelocity.normalized * maxAllowedSpeed;
                }
                else
                {
                    currentHorizontalVelocity = Vector3.MoveTowards(
                        currentHorizontalVelocity,
                        targetMoveVector,
                        groundAcceleration * Time.fixedDeltaTime
                    );
                }
            }
        }

        if (wallRunning != 0)
            currentVelocity.y = 0;

        rb.linearVelocity = new Vector3(currentHorizontalVelocity.x, currentVelocity.y, currentHorizontalVelocity.z);
    }

    private void ChangeLookDirection()
    {
        Vector3 currentHorizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
    
        Vector3 targetLookDir = Vector3.zero;
    
        if (playerSensors.IsLocked)
        {
            targetLookDir = playerSensors.LockedTarget - rb.position;
        }
        else if (currentHorizontalVelocity.magnitude > 0.2f)
        {
            targetLookDir = currentHorizontalVelocity;
        }
        else if (targetMoveVector != Vector3.zero)
        {
            targetLookDir = targetMoveVector;
        }

        targetLookDir.y = 0f;

        if (targetLookDir != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetLookDir.normalized);
            Quaternion deltaRotation = targetRotation * Quaternion.Inverse(rb.rotation);
            deltaRotation.ToAngleAxis(out float angle, out Vector3 axis);
        
            if (angle > 180f) angle -= 360f;
        
            if (angle != 0f)
            {
                Vector3 turnVelocity = axis * (angle * Mathf.Deg2Rad * turnTorque);
                rb.AddTorque(turnVelocity - rb.angularVelocity, ForceMode.VelocityChange);
            }
        }
    }
}