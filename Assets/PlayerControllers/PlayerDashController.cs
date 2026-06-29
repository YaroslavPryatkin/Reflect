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
    private AnimationController animationController;
    private MovementController movementController;
    private Rigidbody rb;
    private Utility.ValueTimer<bool> isDashing = new(false);

    
    public bool Dashing => isDashing.Value;
    
    private Vector3 newVelocity = Vector3.zero;
    private Quaternion newRotation = Quaternion.identity;
    private bool firstUpdateAfterPressedDash = false;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        animationController = GetComponent<AnimationController>();
        CapsuleCollider capsule = GetComponent<CapsuleCollider>();
        movementController = GetComponent<MovementController>();
        capsuleRadius = capsule.radius;
        capsuleCenterToTop = Vector3.up * (capsule.height / 2 - capsule.radius);
        //Debug.Log(capsuleCenterToTop.magnitude + ", " + capsuleRadius);
    }

    private void Start()
    {
        ghost.SetActive(false);
    }
    
    private void OnEnable()
    {
        PlayerMapInputManager.Instance.OnDashPressEvent += HandleDashPress;
        PlayerMapInputManager.Instance.OnDashReleaseEvent += HandleDashRelease;
    }

    private void OnDisable()
    {
        PlayerMapInputManager.Instance.OnDashPressEvent -= HandleDashPress;
        PlayerMapInputManager.Instance.OnDashReleaseEvent -= HandleDashRelease;
    }

    private void HandleDashPress()
    {
        //Debug.Log("Dash Press");
        if (!isDashing && isDashing.CanBeChanged)
        {
            firstUpdateAfterPressedDash = true;
            animationController.HandleDashPressed();
            ChangeTimePace(slowMotionCoefficient);
            isDashing.SetForce(true, 0);
            ghost.SetActive(true);
        }
    }


    private void HandleDashRelease()
    {
        //Debug.Log("Dash Release");
        if (isDashing)
        {
            firstUpdateAfterPressedDash = false;
            animationController.HandleDashReleased();
            ChangeTimePace(1);
            isDashing.SetForce(false, dashRechargeTime);
            ghost.SetActive(false);
            finishDash();
        }
    }

    void Update()
    {
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

        var currentSpeed = rb.linearVelocity.magnitude;
        var speedChange = Math.Min(dashSpeedGainMaxSpeed - currentSpeed, dashSpeedGain);

        var newSpeed = currentSpeed + speedChange;
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
            newVelocity = Vector3.zero;
        }
        else
        {
            newVelocity = xzRotation.normalized * newSpeed;
            newRotation = Quaternion.LookRotation(xzRotation.normalized, Vector3.up);
        }

        //HandleDashRelease();
    }

    private void finishDash()
    {
        //Debug.Log("dashing");
        transform.position = ghost.transform.position;
        transform.rotation = newRotation;
        rb.linearVelocity = newVelocity;
        rb.angularVelocity = Vector3.zero;
    }
}
