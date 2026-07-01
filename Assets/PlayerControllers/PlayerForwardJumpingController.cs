using System;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerForwardJumpingController : MonoBehaviour
{
    [SerializeField] private float jumpHeigh = 1f;
    [SerializeField] private float speedMultiplier = 1f;
    [SerializeField] private float minSpeed = 1f;
    [SerializeField] private float minOvershoot = 0.5f;
    [SerializeField] private float speedAtMinOvershoot = 2;
    [SerializeField] private float maxOvershoot = 2;
    [SerializeField] private float speedAtMaxOvershoot = 10;
    [SerializeField] private float landingTime = 0.5f;
    [SerializeField] private float minSpeedToTrigger = 2;
    [SerializeField] private float rayCastDownDistance = 20f;
    [SerializeField] private float maximalFallingSpeed = 10f;
    [SerializeField] private LayerMask groundLayer;





    private float speedToOvershootMultiplier;

    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;
    private PlayerSensors  _playerSensors;

    private Vector3 startPos;
    private Vector3 endPos;
    private Vector3 endVelocity;
    private bool wasKinematic;

    

    public float TimeToJump { get; private set; } = 0f;

    public bool IsInAir { get; private set; } = false;
    private Utility.TemporaryValue<bool> isLanding = new(false, true);
    public bool IsLanding => isLanding.Value;
    public bool IsForwardJumping => IsInAir || IsLanding;
    
    public float MinSpeedToTrigger => minSpeedToTrigger;
    public float LandingTime => landingTime;
    
    private float jumpPercent = 0f;
    private float midPointY = 0f;
    private float midPointX = 0f;
    private float scale = 0f;
    private bool parabolaCalculationLerpMode;

    public void PerformForwardJump(Vector3 startPos, Vector3 endPos)
    {

        var overshoot =
            Mathf.Clamp(
                (_playerSensors.HorizontalSpeed - speedAtMinOvershoot) * speedToOvershootMultiplier + minOvershoot,
                minOvershoot, maxOvershoot);
        
        var xzEndPos =  endPos + _playerSensors.NormalizedHorizontalVelocity*overshoot;
        
        if (Physics.Raycast(xzEndPos, Vector3.down, out RaycastHit hit, rayCastDownDistance, groundLayer))
        {
            xzEndPos =  hit.point;
        }

        var realEndPos = xzEndPos + Vector3.up * (capsuleCollider.height / 2);

        var diff = realEndPos - startPos;
        var horizontalDiff = new Vector3(diff.x, 0f, diff.z);
        var horizontalDiffScalar = horizontalDiff.magnitude;
        
        this.endPos = realEndPos;
        this.startPos = startPos;

        CalculateParabola(endPos.y, horizontalDiffScalar);

        var normal = Vector3.Cross(horizontalDiff, Vector3.up).normalized;
        
        if (horizontalDiffScalar < 0.001f)
        {
            rb.position = realEndPos;
            return;
        }
        
        endVelocity = Vector3.ProjectOnPlane(_playerSensors.HorizontalVelocity, normal);
        
        rb.angularVelocity = Vector3.zero;
        rb.rotation = Quaternion.LookRotation(horizontalDiff, Vector3.up);
        
        wasKinematic = rb.isKinematic;
        rb.isKinematic = true; 
        
        IsInAir = true;
        jumpPercent = 0f;
    }

    private void CalculateParabola(float obstacleHeigh, float horizontalDiffScalar)
    {
        midPointY = Mathf.Max(startPos.y, endPos.y, obstacleHeigh + capsuleCollider.height / 2) + jumpHeigh;
        var startToMidX = Mathf.Sqrt(midPointY - startPos.y);
        var midToEndX = Mathf.Sqrt(midPointY - endPos.y);
        parabolaCalculationLerpMode = (startToMidX + midToEndX <= 0f);
        midPointX = startToMidX / (startToMidX + midToEndX); 
        scale = (startToMidX + midToEndX) * (startToMidX + midToEndX);
        var tangentPointX =  midPointX + maximalFallingSpeed / (2 * scale);
        
        var horizontalSpeed = Mathf.Max(_playerSensors.HorizontalSpeed * speedMultiplier, minSpeed);
        if (tangentPointX >= 1)
        {
            TimeToJump = horizontalDiffScalar / horizontalSpeed;
        }
        else
        {
            TimeToJump = horizontalDiffScalar * tangentPointX / horizontalSpeed + 
                         (GetParabolaY(tangentPointX) - endPos.y)/maximalFallingSpeed;
        }
    }
    
    private float GetParabolaY(float t)
    {
        if (parabolaCalculationLerpMode) 
            return Mathf.Lerp(startPos.y, endPos.y, t);
        
        var x = t - midPointX; 
        return midPointY - scale * x * x;
    }
    

    private void FinishForwardJump()
    {
        IsInAir = false;
        isLanding.Activate(landingTime);
        
        rb.isKinematic = wasKinematic;
        rb.linearVelocity = endVelocity;
        rb.angularVelocity = Vector3.zero;
    }

    private void FixedUpdate()
    {
        if (IsInAir)
        {
            jumpPercent += Time.fixedDeltaTime / TimeToJump;

            if (jumpPercent >= 1f)
            {
                rb.MovePosition(endPos);
                FinishForwardJump();
            }
            else
            {
                var targetPosition = Vector3.Lerp(startPos, endPos, jumpPercent);
                targetPosition.y = GetParabolaY(jumpPercent);
                
                rb.MovePosition(targetPosition);
            }
        }
    }
    
    public void Interrupt()
    {
        if (IsInAir)
        {
            IsInAir = false;
            rb.isKinematic = wasKinematic;
            rb.linearVelocity = endVelocity;
            rb.angularVelocity = Vector3.zero;
            _playerSensors.UpdateVelocity();
        }
        else if (isLanding.Value)
        {
            isLanding.Deactivate();
        }
    }



    private void Awake()
    {
        speedToOvershootMultiplier = (maxOvershoot - minOvershoot) / (speedAtMaxOvershoot - speedAtMinOvershoot);
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        _playerSensors =  GetComponent<PlayerSensors>();
    }
}
