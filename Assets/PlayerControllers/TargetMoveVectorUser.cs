using UnityEngine;

public class TargetMoveVectorUser : MonoBehaviour
{
    [Header("Acceleration")]
    [SerializeField] private float groundAcceleration = 50f;
    [SerializeField] private float groundDeceleration = 40f;
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
    
    
    private PlayerSensors _playerSensors;
    private MovementController _movementController;
    private PlayerForwardJumpingController _playerForwardJumpingController;
    private Rigidbody rb;

    
    private Vector3 targetMoveVector;
    private float currentHorizontalSpeed = 0f;
    private float targetSpeed = 0f;
    private Vector3 targetDir = Vector3.zero;
    
    private void Awake()
    {
        _playerSensors = GetComponent<PlayerSensors>();
        _movementController = GetComponent<MovementController>();
        _playerForwardJumpingController =  GetComponent<PlayerForwardJumpingController>();
        rb = GetComponent<Rigidbody>();
    }
    
    private void FixedUpdate()
    {
        targetMoveVector = _movementController.TargetMoveVector;
        currentHorizontalSpeed = _playerSensors.HorizontalSpeed;
        targetSpeed = targetMoveVector.magnitude;
        targetDir = targetMoveVector.normalized;
        
        if (!_playerForwardJumpingController.IsInAir)
        {
            ChangeVelocityVectorToMatchTargetMoveVector();
            ChangeLookDirection();
        }
    }

    public void SnapToWall()
    {
        var wallRunPoint = _movementController.wallRunPoint;
        var wallRunNormal = _movementController.wallRunNormal;
        var dist = Vector3.Distance(transform.position, wallRunPoint);
        var maxError = wallRunDistanceFromWall * wallRunDistanceFromWallError;
        if (Mathf.Abs(dist - wallRunDistanceFromWall) > maxError)
        {
            transform.position = wallRunPoint + wallRunNormal * wallRunDistanceFromWall;
            //Debug.Log("Snapped position to " + transform.position);
        }

        if ( Vector3.Dot(_playerSensors.NormalizedHorizontalVelocity, targetDir) < 1 - wallRunRotationFromWallError)
        {
            var realTargetDir = Vector3.ProjectOnPlane(_playerSensors.NormalizedHorizontalVelocity, wallRunNormal);
            realTargetDir.y = 0;
            rb.linearVelocity = realTargetDir * _playerSensors.Speed;
            //Debug.Log("Snapped valocity to " + rb.linearVelocity);
        }
    }


    

    
    private void ChangeVelocityVectorToMatchTargetMoveVector()
    {
        
        var newHorizontalVelocity = _playerSensors.HorizontalVelocity;
        var newY = _playerSensors.Velocity.y;
        
        if (_playerSensors.IsGrounded)
        {
            // if (_movementController.justJumped)
            // {
            //     _movementController.justJumped = false;
            //     Debug.Log("Type = ground, Previos speed = " + _playerSensors.HorizontalSpeed + ", target speed = " + targetSpeed + ", new speed = " + newHorizontalVelocity.magnitude);
            // }
            SmartVelocityChange(ref  newHorizontalVelocity, groundAcceleration, groundDeceleration);
        }
        else if(_movementController.WallRunningState != 0)
        {
            // if (_movementController.justJumped)
            // {
            //     _movementController.justJumped = false;
            //     Debug.Log("Type = wall, Previos speed = " + _playerSensors.HorizontalSpeed + ", target speed = " + targetSpeed + ", new speed = " + newHorizontalVelocity.magnitude);
            // }
            newY = 0;
            SnapToWall();
            SmartVelocityChange(ref newHorizontalVelocity, wallRunAcceleration, wallRunDeceleration);
        }
        else    
        {
            // if (_movementController.justJumped)
            // {
            //     _movementController.justJumped = false;
            //     Debug.Log("Type = air, Previos speed = " + _playerSensors.HorizontalSpeed  + ", target speed = " + targetSpeed+ ", new speed = " + newHorizontalVelocity.magnitude);
            // }
            if (targetMoveVector != Vector3.zero)
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
                            (airAcceleration + fraction * additionalAirAccelerationIfAngleIsBig) * Time.fixedDeltaTime, 0f
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
                        targetMoveVector,
                        groundAcceleration * Time.fixedDeltaTime
                    );
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
                targetMoveVector,
                acceleration * Time.fixedDeltaTime
            );
        }
    }

    private void ChangeLookDirection()
    {
        var targetLookDir = Vector3.zero;
    
        if (_playerSensors.IsLocked)
        {
            targetLookDir = _playerSensors.LockedTarget - rb.position;
        }
        else if (_playerSensors.HorizontalSpeed > 0.2f)
        {
            targetLookDir = _playerSensors.HorizontalVelocity;;
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
