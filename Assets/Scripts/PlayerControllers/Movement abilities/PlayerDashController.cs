using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerDashController : MonoBehaviour
{

    [SerializeField] private GameObject ghost;
    [Header("Speed and distance")]
    [SerializeField] private float dashMaxDistance = 5;
    [SerializeField] private float dashSpeedGain = 5;
    [SerializeField] private float dashSpeedGainMaxSpeed = 15;
    [SerializeField] private float dashMaxVerticalVelocityChange = 3f;
    [Header("Timings")]
    [SerializeField] private float dashRechargeTime = 1f;
    [SerializeField] private float maxDashDurationTime = 1f;
    [Header("Other")]
    [SerializeField] private float angleUp = 15f;
    [SerializeField] private float slowMotionCoefficient = 0.2f;
    [SerializeField] private float ghostSmoothSpeed = 30f;

    private Vector3 capsuleCenterToTop;
    private float capsuleRadius;
    
    private PlayerSensors  _playerSensors;
    private PlayerInputController _playerInputController;
    private Rigidbody _rb;
    private PlayerForwardJumpingController  _playerForwardJumpingController;
    private PlayerFixedDirectionMovementController _playerFixedDirectionMovementController;
    
    
    
    private UtilityClasses.FractionBlockingValueTimer<bool> isDashing = new(false);

    
    public bool IsDashing => isDashing.Value;
    public float DashRechargeFraction => isDashing.TimeFraction;
    
    private Vector3 newHorizontalVelocityDirection = Vector3.zero;
    private Vector3 newVelocityDirection = Vector3.zero;
    private bool newHorizontalVelocityIsZero = true;
    private Quaternion newRotation = Quaternion.identity;
    private bool firstUpdateAfterPressedDash = false;
    private int layerMask;
    
    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _playerSensors = GetComponent<PlayerSensors>();
        capsuleRadius = _playerSensors.ColliderRadius;
        capsuleCenterToTop = _playerSensors.ColliderCenterToTop;
        _playerForwardJumpingController = GetComponent<PlayerForwardJumpingController>();
        _playerInputController = GetComponent<PlayerInputController>();
        _playerFixedDirectionMovementController = GetComponent<PlayerFixedDirectionMovementController>();
        layerMask = _playerSensors.DashLayerMask;
        //Debug.Log(capsuleCenterToTop.magnitude + ", " + capsuleRadius);
    }

    private void Start()
    {
        ghost.SetActive(false);
    }


    void Update()
    {
        if (_playerInputController.IsDashPressed)
        {
            if (isDashing.CanBeChanged)
            {
                if (isDashing)
                {
                    FinishDash();
                }
                else
                {
                    if (_playerForwardJumpingController.IsForwardJumping)
                        _playerForwardJumpingController.Interrupt();

                    firstUpdateAfterPressedDash = true;
                    GlobalTimeScaleController.ChangeTimePace(this, slowMotionCoefficient);
                    isDashing.SetForce(true, maxDashDurationTime);
                    ghost.SetActive(true);
                }
            }
        }
        else
        {
            if (isDashing)
            {
                FinishDash();
            }
        }
        
        if (isDashing)
        {
            MoveGhost();
            firstUpdateAfterPressedDash = false;
        }
        
    }



    private void MoveGhost()
    {
        Vector3 lookDir = GlobalLookDirectionManager.CurrentLookDirection;
        Vector3 right = Vector3.Cross(lookDir, Vector3.up);
        Quaternion rotation = Quaternion.AngleAxis(angleUp, right);
        lookDir = rotation * lookDir;

        var targetPosition = UtilityFunctions.GetCapsuleRayCastPoint(
            transform.position, capsuleCenterToTop, capsuleRadius,
            lookDir, dashMaxDistance, layerMask, QueryTriggerInteraction.Collide);


        //Debug.Log("Moving ghost to position " + targetPosition);
        if (firstUpdateAfterPressedDash)
            ghost.transform.position = targetPosition;
        else
            ghost.transform.position = Vector3.Lerp(
                ghost.transform.position, 
                targetPosition, 
                ghostSmoothSpeed * Time.unscaledDeltaTime
            );
        
        
        
        var targetRotation = targetPosition - transform.position;
        var xzRotation = new Vector3(targetRotation.x, 0f, targetRotation.z);
        
        
        if (xzRotation.sqrMagnitude < 0.0001f)
        {
            newRotation =  Quaternion.identity;
            newHorizontalVelocityDirection = Vector3.zero;
            newHorizontalVelocityIsZero = true;
        }
        else
        {
            newHorizontalVelocityDirection = xzRotation;
            newRotation = Quaternion.LookRotation(xzRotation.normalized, Vector3.up);
            newHorizontalVelocityIsZero = false;
        }

        newVelocityDirection = targetRotation;

        //HandleDashRelease();
    }

    public void InterruptDash()
    {
        if (IsDashing)
        {
            firstUpdateAfterPressedDash = false;
            GlobalTimeScaleController.ReturnTimePace(this);
            isDashing.SetForce(false, dashRechargeTime);
            ghost.SetActive(false);
        }
    }
    
    private void FinishDash()
    {
        InterruptDash();
        
        transform.position = ghost.transform.position;
        transform.rotation = newRotation;
        
        var currentHorizontalSpeed = _playerSensors.HorizontalSpeed; 
        var horizontalSpeedChange = Mathf.Clamp(dashSpeedGainMaxSpeed - currentHorizontalSpeed, 0f, dashSpeedGain);
        var newHorizontalSpeed = currentHorizontalSpeed + horizontalSpeedChange;

        var currentVerticalSpeed = _playerSensors.Velocity.y;
        
        
        if (newHorizontalVelocityIsZero)
        {
            var currentHorizontalVelocity = _playerSensors.HorizontalVelocity;
            if(newVelocityDirection.y < 0)
                _rb.linearVelocity = new Vector3(currentHorizontalVelocity.x, currentVerticalSpeed - dashMaxVerticalVelocityChange,
                    currentHorizontalVelocity.z);
            else
                _rb.linearVelocity = new Vector3(currentHorizontalVelocity.x, currentVerticalSpeed + dashMaxVerticalVelocityChange,
                currentHorizontalVelocity.z);
        }
        else
        {
            var newPotentialVerticalSpeed = newVelocityDirection.y/newHorizontalVelocityDirection.magnitude * newHorizontalSpeed;
            var newVerticalSpeed = Mathf.Clamp(newPotentialVerticalSpeed,
                currentVerticalSpeed - dashMaxVerticalVelocityChange,
                currentVerticalSpeed + dashMaxVerticalVelocityChange);
            var normalized = newHorizontalVelocityDirection.normalized;
            _rb.linearVelocity = new Vector3(normalized.x * newHorizontalSpeed, newVerticalSpeed,
                normalized.z * newHorizontalSpeed);
        }
        _rb.angularVelocity = Vector3.zero;
        Physics.SyncTransforms();
        _playerSensors.UpdateVelocity();
        _playerInputController.ChangeLastNonZeroInputToCurrentHorizontalVelocity();
        _playerFixedDirectionMovementController.SuppressAfterJump(0f);
    }
}
