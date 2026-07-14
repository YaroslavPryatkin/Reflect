using UnityEngine;

public class PlayerTargetMoveVectorUser : MonoBehaviour
{
    [Header("Acceleration")]
    [SerializeField] private float groundAcceleration = 50f;
    [SerializeField] private float groundDeceleration = 40f;
    [SerializeField] private float groundAccelerationIfDotIsLow = 100f;
    [SerializeField] private float groundLowDot = -0.3f;
    //[SerializeField] private float groundVerticalAcceleration = 100f;
    [SerializeField] private float airAcceleration = 4f;
    [SerializeField] private float additionalAirAccelerationIfAngleIsBig = 10f;
    [SerializeField] private float wallRunAcceleration = 70f;
    [SerializeField] private float wallRunDeceleration = 15f;
    [SerializeField] private float turnTorque = 30f;
    
    [Header("Air Turning")]
    [SerializeField] private float airTurnDeadzone = 60f;
    [SerializeField] private float airTurnPow = 3f;
    [SerializeField] private float airTurnMaxLossPerFrame = 0.08f;
    
    [Header("Wall Running")]
    [SerializeField] private float wallRunDistanceFromWall = 0.5f;
    [SerializeField] private float wallRunDistanceFromWallError = 0.1f;
    [SerializeField] private float wallRunRotationFromWallError = 0.2f;
    
    // [Header("Snap to ground")]
    // [SerializeField] private float snapToGroundDistance = 0.1f;
    // [SerializeField] private float snapToGroundTriggerDistance = 0.3f;
    
    
    private PlayerSensors _playerSensors;
    private PlayerMovementController _playerMovementController;
    private PlayerForwardJumpingController _playerForwardJumpingController;
    private PlayerTargetLockController _playerTargetLockController;
    private MeleeToTargetMoveController _meleeToTargetMoveController;
    private Rigidbody rb;

    
    private float currentHorizontalSpeed = 0f;
    private float targetSpeed = 0f;
    private Vector3 targetDir = Vector3.zero;
    
    private void Awake()
    {
        _playerSensors = GetComponent<PlayerSensors>();
        _playerMovementController = GetComponent<PlayerMovementController>();
        _playerForwardJumpingController =  GetComponent<PlayerForwardJumpingController>();
        _playerTargetLockController = GetComponent<PlayerTargetLockController>();
        _meleeToTargetMoveController = GetComponent<MeleeToTargetMoveController>();
        rb = GetComponent<Rigidbody>();
    }
    
    private void FixedUpdate()
    {
        currentHorizontalSpeed = _playerSensors.HorizontalSpeed;
        targetSpeed = _playerMovementController.TargetMoveSpeed;
        targetDir = _playerMovementController.TargetMoveHorizontalDirection;
        

        
        if (!_playerForwardJumpingController.IsInAir)
        {
            if(!_meleeToTargetMoveController.IsMoving)
                ChangeVelocityVectorToMatchTargetMoveVector();
            ChangeLookDirection();
        }
    }

    public void SnapToWall()
    {
        var wallRunPoint = _playerMovementController.LastWallRunPoint;
        var wallRunNormal = _playerMovementController.LastWallRunNormal;
        var dist = Vector3.Distance(transform.position, wallRunPoint);
        var maxError = wallRunDistanceFromWall * wallRunDistanceFromWallError;
        if (Mathf.Abs(dist - wallRunDistanceFromWall) > maxError)
        {
            transform.position = wallRunPoint + wallRunNormal * wallRunDistanceFromWall;
            //Debug.Log("Snapped position to " + transform.position);
        }
        
        if ( targetSpeed > 0.001f && 
             Vector3.Dot(_playerSensors.NormalizedHorizontalVelocity, targetDir) < 1 - wallRunRotationFromWallError)
        {
            var realTargetDir = Vector3.ProjectOnPlane(targetDir, wallRunNormal);
            realTargetDir.y = 0;
            rb.linearVelocity = realTargetDir * _playerSensors.Speed;
            rb.rotation = Quaternion.LookRotation(realTargetDir);
            _playerSensors.UpdateVelocity();
            //Debug.Log("Snapped valocity to " + rb.linearVelocity);
        }
    }


    

    
    private void ChangeVelocityVectorToMatchTargetMoveVector()
    {
        Vector3 newHorizontalVelocity;
        var newY = _playerSensors.Velocity.y;
        if (_playerSensors.IsGrounded && !_playerMovementController.JumpingFromGround)
        {
            if (_playerSensors.IsGrounded && _playerSensors.FoundGroundNormal)
            {
                var slopeRotation = Quaternion.FromToRotation(Vector3.up, _playerSensors.GroundNormal);
                targetDir = slopeRotation * _playerMovementController.TargetMoveHorizontalDirection;
            }
            
            newHorizontalVelocity = _playerSensors.VelocityAlignedWithGround;

            SmartVelocityChange(ref newHorizontalVelocity, groundAcceleration, groundDeceleration, groundAccelerationIfDotIsLow, groundLowDot);

            if (_playerSensors.FoundGroundNormal)
            {
                var normal = _playerSensors.GroundNormal;
            
                if (normal.y > 0.01f) 
                {
                    newY = -(normal.x * newHorizontalVelocity.x + normal.z * newHorizontalVelocity.z) / normal.y;
                }
            }
        }
        else
        {
            newHorizontalVelocity = _playerSensors.HorizontalVelocity;
            if (_playerMovementController.WallRunningState != 0)
            {
                newY = 0;
                SnapToWall();
                SmartVelocityChange(ref newHorizontalVelocity, wallRunAcceleration, wallRunDeceleration);
            }
            else
            {
                //Debug.Log("Air Acceleration " + targetSpeed);
                if (targetDir != Vector3.zero)
                {
                    if (currentHorizontalSpeed > 0.001f)
                    {
                        Vector3 currentDir = newHorizontalVelocity / currentHorizontalSpeed;


                        float angleDeg = Vector3.Angle(currentDir, targetDir);
                        float t = Mathf.Max(0f, angleDeg - airTurnDeadzone) / (180f - airTurnDeadzone);
                        float fraction = Mathf.Pow(t, airTurnPow);
                        float speedRetention = 1f - airTurnMaxLossPerFrame * fraction;

                        newHorizontalVelocity =
                            Vector3.RotateTowards(
                                currentDir,
                                targetDir,
                                (airAcceleration + fraction * additionalAirAccelerationIfAngleIsBig) *
                                Time.fixedDeltaTime, 0f
                            ) *
                            (currentHorizontalSpeed * speedRetention);

                        float projectionOnTarget = Vector3.Dot(newHorizontalVelocity, targetDir);
                        if (projectionOnTarget < targetSpeed)
                        {
                            float speedToAdd = Mathf.Min(
                                targetSpeed - projectionOnTarget,
                                airAcceleration * Time.fixedDeltaTime
                            );
                            newHorizontalVelocity += targetDir * speedToAdd;
                        }

                        float maxAllowedSpeed = Mathf.Max(currentHorizontalSpeed, targetSpeed);
                        if (newHorizontalVelocity.magnitude > maxAllowedSpeed)
                            newHorizontalVelocity = newHorizontalVelocity.normalized * maxAllowedSpeed;
                    }
                    else
                    {
                        newHorizontalVelocity = Vector3.MoveTowards(
                            newHorizontalVelocity,
                            targetDir * targetSpeed,
                            groundAcceleration * Time.fixedDeltaTime
                        );
                    }
                }
            }
        }
        rb.linearVelocity = new Vector3(newHorizontalVelocity.x, newY, newHorizontalVelocity.z);
    }
    
    private void SmartVelocityChange(ref Vector3 newHorizontalVelocity, float acceleration, float deceleration)
    {
        if (currentHorizontalSpeed > targetSpeed && currentHorizontalSpeed > 0.001f)
        {
            var alignedVelocity = Vector3.MoveTowards(
                newHorizontalVelocity, 
                targetDir * currentHorizontalSpeed, 
                acceleration * Time.fixedDeltaTime
            );
            var newSpeed = Mathf.MoveTowards(
                currentHorizontalSpeed, 
                targetSpeed, 
                deceleration * Time.fixedDeltaTime
            );

            newHorizontalVelocity = alignedVelocity.normalized * newSpeed;
        }
        else
        {
            newHorizontalVelocity = Vector3.MoveTowards(
                newHorizontalVelocity,
                targetDir * targetSpeed,
                acceleration * Time.fixedDeltaTime
            );
        }
    }
    
    private void SmartVelocityChange(ref Vector3 newHorizontalVelocity, float acceleration, float deceleration, float accelerationAtHighAngle, float lowDot)
    {
        if (Vector3.Dot(_playerSensors.NormalizedHorizontalVelocity, targetDir) < lowDot)
        {
            newHorizontalVelocity = Vector3.MoveTowards(
                newHorizontalVelocity,
                targetDir * targetSpeed,
                accelerationAtHighAngle * Time.fixedDeltaTime
            );
        }
        else
        {
            SmartVelocityChange(ref newHorizontalVelocity,  acceleration, deceleration);
        }
    }

    private void ChangeLookDirection()
    {
        var targetLookDir = Vector3.zero;

        if (_meleeToTargetMoveController.IsLooking)
        {
            targetLookDir = _meleeToTargetMoveController.MoveDirection;
        }
        else if (_playerTargetLockController.IsLocked && _playerMovementController.CanLookToLockedTarget)
        {
            targetLookDir = _playerTargetLockController.TargetDirection;
        }
        else if (_playerSensors.HorizontalSpeed > 2f)
        {
            targetLookDir = _playerSensors.HorizontalVelocity;;
        }
        else if (targetDir != Vector3.zero)
        {
            targetLookDir =  _playerMovementController.TargetMoveHorizontalDirection;
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
