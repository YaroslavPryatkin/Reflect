using System;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerForwardJumpingController : MonoBehaviour
{
    [SerializeField] private float jumpHeigh = 1f;
    [SerializeField] private float speedMultiplier = 1f;
    [SerializeField] private float minSpeed = 1f;
    [SerializeField] private float landingTime = 0.5f;
    [SerializeField] private float maximalFallingSpeed = 10f;






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
    public float LandingTime => landingTime;
    
    private float jumpPercent = 0f;
    private float midPointY = 0f;
    private float midPointX = 0f;
    private float scale = 0f;
    private bool parabolaCalculationLerpMode;

    public void PerformForwardJump(Vector3 startPos, Vector3 endPos, float obstacleHeight)
    {
        var diff = endPos - startPos;
        var horizontalDiff = new Vector3(diff.x, 0f, diff.z);
        var horizontalDiffScalar = horizontalDiff.magnitude;
        
        this.endPos = endPos;
        this.startPos = startPos;

        CalculateParabola(obstacleHeight, horizontalDiffScalar);

        var normal = Vector3.Cross(horizontalDiff, Vector3.up).normalized;
        
        if (horizontalDiffScalar < 0.001f)
        {
            rb.position = endPos;
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
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        _playerSensors =  GetComponent<PlayerSensors>();
    }
}
