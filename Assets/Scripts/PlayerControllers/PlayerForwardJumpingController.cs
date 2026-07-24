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


    private Vector3 endVelocity;
    private bool wasKinematic;

    

    public float TimeToJump { get; private set; } = 0f;

    private readonly UtilityClasses.FractionBlockingValueTimer<bool> _isInAir = false;
    public bool IsInAir => _isInAir.Value;
    private readonly UtilityClasses.TemporaryValue<bool> _isLanding = new(false, true);
    public bool IsLanding => _isLanding.Value;
    public bool IsForwardJumping => IsInAir || IsLanding;
    public float LandingTime => landingTime;
    

    private UtilityClasses.ParabolaCurve  _parabolaCurve;

    public void PerformForwardJump(Vector3 startPos, Vector3 endPos, float obstacleHeight)
    {
        var diff = endPos - startPos;
        var horizontalDiff = new Vector3(diff.x, 0f, diff.z);
        var horizontalDiffScalar = horizontalDiff.magnitude;
        _parabolaCurve.StartPos =  startPos;
        _parabolaCurve.EndPos = endPos;
        _parabolaCurve.MakeObstacleParabola(obstacleHeight + capsuleCollider.height / 2, jumpHeigh);
        
        var tangentPointX =  _parabolaCurve.MidPointX + maximalFallingSpeed / (2 * _parabolaCurve.Scale);
        var horizontalSpeed = Mathf.Max(_playerSensors.HorizontalSpeed * speedMultiplier, minSpeed);
        if (tangentPointX >= 1)
        {
            TimeToJump = horizontalDiffScalar / horizontalSpeed;
        }
        else
        {
            TimeToJump = horizontalDiffScalar * tangentPointX / horizontalSpeed + 
                         (_parabolaCurve.GetParabolaY(tangentPointX) - endPos.y)/maximalFallingSpeed;
        }

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
        
        _isInAir.SetForce(true, TimeToJump);
    }


    

    

    private void FinishForwardJump()
    {
        _isInAir.SetForce(false);
        _isLanding.Activate(landingTime);
        
        rb.isKinematic = wasKinematic;
        rb.linearVelocity = endVelocity;
        rb.angularVelocity = Vector3.zero;
    }

    private void FixedUpdate()
    {
        if (_isInAir.Value)
        {

            if (_isInAir.CanBeChanged)
            {
                rb.MovePosition(_parabolaCurve.EndPos);
                FinishForwardJump();
            }
            else
            {
                rb.MovePosition(_parabolaCurve.GetPosition(_isInAir.TimeFraction));
            }
        }
    }
    
    public void Interrupt()
    {
        if (IsInAir)
        {
            _isInAir.SetForce(false);
            rb.isKinematic = wasKinematic;
            rb.linearVelocity = endVelocity;
            rb.angularVelocity = Vector3.zero;
            _playerSensors.UpdateVelocity();
        }
        else if (_isLanding.Value)
        {
            _isLanding.Deactivate();
        }
    }



    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        _playerSensors =  GetComponent<PlayerSensors>();
    }
}
