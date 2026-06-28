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

    private Vector3 capsuleCenterToTop;
    private float capsuleRadius;
    private PlayerMovementInputController _playerMovementInputController;
    private Rigidbody rb;
    private Utility.ValueTimer<bool> isDashing = new(false);

    private float newSpeed = 0;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        _playerMovementInputController = GetComponent<PlayerMovementInputController>();
        CapsuleCollider capsule = GetComponent<CapsuleCollider>();
        capsuleRadius = capsule.radius;
        capsuleCenterToTop = Vector3.up * (capsule.height / 2);
    }

    private void Start()
    {
        ghost.SetActive(false);
    }
    
    private void OnEnable()
    {
        GlobalInputManager.Instance.OnDashPressEvent += HandleDashPress;
        GlobalInputManager.Instance.OnDashReleaseEvent += HandleDashRelease;
    }

    private void OnDisable()
    {
        GlobalInputManager.Instance.OnDashPressEvent -= HandleDashPress;
        GlobalInputManager.Instance.OnDashReleaseEvent -= HandleDashRelease;
    }

    private void HandleDashPress()
    {
        //Debug.Log("Dash Press");
        if (!isDashing && isDashing.CanBeChanged)
        {
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
            moveGhost();
        }
        
    }

    private void ChangeTimePace(float time)
    {
        //Debug.Log("Before: timescale = " + Time.timeScale + ", fixed delta time = " + Time.fixedDeltaTime);
        Time.timeScale = time;
        Time.fixedDeltaTime = 0.02f * time;
    }

    private void moveGhost()
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

        newSpeed = currentSpeed + speedChange;
        //Debug.Log("Moving ghost to position " + targetPosition);
        ghost.transform.position = targetPosition;
        var targetRotation = targetPosition - transform.position;
        Vector3 xzRotation = new Vector3(targetRotation.x, 0f, targetRotation.z);

        
        if (xzRotation.sqrMagnitude < 0.0001f)
        {
            ghost.transform.rotation =  Quaternion.identity;
        }
        else
        {
            ghost.transform.rotation = Quaternion.LookRotation(xzRotation.normalized, Vector3.up);
        }
    }

    private void finishDash()
    {
        //Debug.Log("dashing");
        transform.position = ghost.transform.position;
        transform.rotation = ghost.transform.rotation;
        rb.linearVelocity = ghost.transform.forward * newSpeed;
        rb.angularVelocity = Vector3.zero;
    }
}
