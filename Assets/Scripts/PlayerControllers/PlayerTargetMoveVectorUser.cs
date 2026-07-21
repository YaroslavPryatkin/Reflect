using UnityEngine;

public class PlayerTargetMoveVectorUser : MonoBehaviour
{
    [Header("Acceleration")]
    [Header("Ground")]
    [SerializeField] private float groundAcceleration = 50f;
    [SerializeField] private float groundDeceleration = 40f;
    [SerializeField] private float groundAccelerationIfDotIsLow = 100f;
    [SerializeField] private float groundLowDot = -0.3f;
    //[SerializeField] private float groundVerticalAcceleration = 100f;
    [Header("Air")]
    [SerializeField] private float airAcceleration = 4f;
    [SerializeField] private float additionalAirAccelerationIfAngleIsBig = 10f;
    [Header("Wall")]
    [SerializeField] private float wallRunAcceleration = 70f;
    [SerializeField] private float wallRunDeceleration = 15f;
    [Header("Rail line")]
    [SerializeField] private float railLineAcceleration = 70f;
    [SerializeField] private float railLineDeceleration = 15f;
    
    [Header("Turn torque")]
    [SerializeField] private float turnTorque = 30f;
    
    [Header("Air Turning")]
    [SerializeField] private float airTurnDeadzone = 60f;
    [SerializeField] private float airTurnPow = 3f;
    [SerializeField] private float airTurnMaxLossPerFrame = 0.08f;
    
    private PlayerSensors _playerSensors;
    private PlayerFixedDirectionMovementController _playerFixedDirectionMovementController;
    private PlayerManager _playerManager;
    private PlayerForwardJumpingController _playerForwardJumpingController;
    private PlayerTargetLockController _playerTargetLockController;
    private MeleeTransformController _meleeTransformController;
    private Rigidbody rb;

    
    private float currentHorizontalSpeed = 0f;
    private float targetSpeed = 0f;
    private Vector3 targetDir = Vector3.zero;
    
    private void Awake()
    {
        _playerSensors = GetComponent<PlayerSensors>();
        _playerFixedDirectionMovementController = GetComponent<PlayerFixedDirectionMovementController>();
        _playerManager = GetComponent<PlayerManager>();
        _playerForwardJumpingController =  GetComponent<PlayerForwardJumpingController>();
        _playerTargetLockController = GetComponent<PlayerTargetLockController>();
        _meleeTransformController = GetComponent<MeleeTransformController>();
        rb = GetComponent<Rigidbody>();
    }
    
    private void FixedUpdate()
    {
        currentHorizontalSpeed = _playerSensors.HorizontalSpeed;
        targetSpeed = _playerManager.TargetMoveSpeed;
        targetDir = _playerManager.TargetMoveDirection;
        

        
        if (!_playerForwardJumpingController.IsInAir)
        {
            if(!_meleeTransformController.IsActive || !_meleeTransformController.IsControllingMovement)
                ChangeVelocityVectorToMatchTargetMoveVector();
            
            if(!_meleeTransformController.IsActive)
                ChangeLookDirection();
        }
    }




    

    
    private void ChangeVelocityVectorToMatchTargetMoveVector()
    {
        
        
        
        Vector3 newHorizontalVelocity;
        var newY = _playerSensors.Velocity.y;
        if (_playerManager.UseGroundPhysics)
        {
            if (_playerSensors.IsGrounded && _playerSensors.FoundGroundNormal)
            {
                var slopeRotation = Quaternion.FromToRotation(Vector3.up, _playerSensors.GroundNormal);
                targetDir = slopeRotation * _playerManager.TargetMoveDirection;
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
            if (!_playerFixedDirectionMovementController.IsStateNon)
            {
                newHorizontalVelocity = _playerSensors.Velocity;
                _playerFixedDirectionMovementController.SnapToPlace(targetSpeed, targetDir);
                if(_playerFixedDirectionMovementController.State == PlayerFixedDirectionMovementController.StateEnum.Line)
                    SmartVelocityChange(ref newHorizontalVelocity, railLineAcceleration, railLineDeceleration);
                else
                    SmartVelocityChange(ref newHorizontalVelocity, wallRunAcceleration, wallRunDeceleration);
                
                newY = newHorizontalVelocity.y;
            }
            else
            {
                newHorizontalVelocity = _playerSensors.HorizontalVelocity;
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
    
    private void SmartVelocityChange(ref Vector3 velocity, float acceleration, float deceleration)
    {
        if (currentHorizontalSpeed > targetSpeed && currentHorizontalSpeed > 0.001f)
        {
            var alignedVelocity = Vector3.MoveTowards(
                velocity, 
                targetDir * currentHorizontalSpeed, 
                acceleration * Time.fixedDeltaTime
            );
            var newSpeed = Mathf.MoveTowards(
                currentHorizontalSpeed, 
                targetSpeed, 
                deceleration * Time.fixedDeltaTime
            );

            velocity = alignedVelocity.normalized * newSpeed;
        }
        else
        {
            velocity = Vector3.MoveTowards(
                velocity,
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

        if (_playerManager.LookToLockedTarget)
        {
            targetLookDir = _playerTargetLockController.NormalizedHorizontalDirectionToLockedTarget;
        }
        else if (_playerSensors.HorizontalSpeed > 2f)
        {
            targetLookDir = _playerSensors.HorizontalVelocity;;
        }
        else if (targetDir != Vector3.zero)
        {
            targetLookDir =  _playerManager.TargetMoveDirection;
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
                var turnVelocity = axis * (angle * Mathf.Deg2Rad * turnTorque);
                rb.AddTorque(turnVelocity - rb.angularVelocity, ForceMode.VelocityChange);
            }
        }
    }
}
