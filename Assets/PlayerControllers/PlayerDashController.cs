using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerDashController : MonoBehaviour
{

    [SerializeField] private GameObject ghost;
    [SerializeField] private float dashMaxDistance = 5;
    [SerializeField] private float dashSpeedGain = 5;
    [SerializeField] private float dashSpeedGainMaxSpeed = 15;
    [SerializeField] private float dashRechargeTime = 1f;
    [SerializeField] private float angleUp = 15f;
    [SerializeField] private LayerMask ignoreLayers;
    [SerializeField] private float slowMotionCoefficient = 0.2f;
    [SerializeField] private float ghostSmoothSpeed = 30f;

    private Vector3 capsuleCenterToTop;
    private float capsuleRadius;
    private PlayerSensors  _playerSensors;
    private PlayerMovementInputController _playerMovementInputController;
    private Rigidbody rb;
    private PlayerForwardJumpingController  _playerForwardJumpingController;
    private Utility.FractionValueTimer<bool> isDashing = new(false);

    
    public bool IsDashing => isDashing.Value;
    public float DashRechargeFraction => isDashing.Value ? 0 : isDashing.TimeFraction;
    
    private float newSpeed;
    private Vector3 newVelocityDirection = Vector3.zero;
    private Quaternion newRotation = Quaternion.identity;
    private bool firstUpdateAfterPressedDash = false;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        var capsule = GetComponent<CapsuleCollider>();
        _playerSensors = GetComponent<PlayerSensors>();
        capsuleRadius = capsule.radius;
        capsuleCenterToTop = Vector3.up * (capsule.height / 2 - capsule.radius);
        _playerForwardJumpingController = GetComponent<PlayerForwardJumpingController>();
        _playerMovementInputController = GetComponent<PlayerMovementInputController>();
        //Debug.Log(capsuleCenterToTop.magnitude + ", " + capsuleRadius);
    }

    private void Start()
    {
        ghost.SetActive(false);
    }


    void Update()
    {
        if (_playerMovementInputController.IsDashPressed)
        {
            if (!isDashing && isDashing.CanBeChanged)
            {
                if(_playerForwardJumpingController.IsForwardJumping)
                    _playerForwardJumpingController.Interrupt();
            
                firstUpdateAfterPressedDash = true;
                ChangeTimePace(slowMotionCoefficient);
                isDashing.SetForce(true, 0);
                ghost.SetActive(true);
                var currentSpeed = _playerSensors.Speed;
                var speedChange = Math.Clamp(dashSpeedGainMaxSpeed - currentSpeed,0, dashSpeedGain);
                newSpeed = currentSpeed + speedChange;
            }
        }
        else
        {
            if (isDashing)
            {
                firstUpdateAfterPressedDash = false;
                ChangeTimePace(1);
                isDashing.SetForce(false, dashRechargeTime);
                ghost.SetActive(false);
                finishDash();
            }
        }
        
        if (isDashing)
        {
            MoveGhost();
            firstUpdateAfterPressedDash = false;
        }
        
    }

    private void ChangeTimePace(float time)
    {
        //Debug.Log("Before: timescale = " + Time.timeScale + ", fixed delta time = " + Time.fixedDeltaTime);
        Time.timeScale = time;
        Time.fixedDeltaTime = 0.02f * time;
    }

    private void MoveGhost()
    {
        Vector3 lookDir = GlobalLookDirectionManager.CurrentLookDirection;
        Vector3 right = Vector3.Cross(lookDir, Vector3.up);
        Quaternion rotation = Quaternion.AngleAxis(angleUp, right);
        lookDir = rotation * lookDir;

        var targetPosition = Utility.GetCapsuleRayCastPoint(
            transform.position, capsuleCenterToTop, capsuleRadius,
            lookDir, dashMaxDistance, ignoreLayers);


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
            newVelocityDirection = Vector3.zero;
        }
        else
        {
            newVelocityDirection = xzRotation.normalized;
            newRotation = Quaternion.LookRotation(xzRotation.normalized, Vector3.up);
        }

        //HandleDashRelease();
    }

    private void finishDash()
    {
        //Debug.Log("dashing");
        transform.position = ghost.transform.position;
        transform.rotation = newRotation;
        rb.linearVelocity = newVelocityDirection * newSpeed;
        rb.angularVelocity = Vector3.zero;
        _playerSensors.UpdateVelocity();
    }
}
