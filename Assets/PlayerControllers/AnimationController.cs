using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class AnimationController : MonoBehaviour
{
    
    [SerializeField] private Animator animator;
    private Rigidbody rb;
    private PlayerSensors playerSensors;
    private MovementController movementController;
    
    private int isDashingHash = Animator.StringToHash("IsDashing");
    private int JumpHash = Animator.StringToHash("isJumping");
    private int SpeedHash = Animator.StringToHash("Speed");
    private int wallRunningHash = Animator.StringToHash("WallRunning");
    private int isGroundedHash = Animator.StringToHash("IsGrounded");
    private int isHighJumpHash = Animator.StringToHash("isHighJump");
    private int isRollingHash = Animator.StringToHash("isRolling");
    // Start is called once before the first execution of Update after the MonoBehaviour is created



    private Utility.TemporaryValue<bool> jumpingAnimationBuffer = new(false, true);
    public void HandleDashPressed()
    {
        animator.SetBool(isDashingHash, true);
    }
    public void HandleDashReleased()
    {
        animator.SetBool(isDashingHash, false);
    }
    

    public void HandleJump()
    {
        jumpingAnimationBuffer.Activate(0.1f);
        //Debug.Log("Jump");
        
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerSensors = GetComponent<PlayerSensors>();
        movementController = GetComponent<MovementController>();
    }


    private void Update()
    {
        
        var flatVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        animator.SetFloat(SpeedHash, flatVelocity.magnitude);
        
        animator.SetInteger(wallRunningHash, movementController.WallRunningState);
        
        animator.SetBool(isGroundedHash, playerSensors.IsGrounded);
        
        animator.SetBool(isHighJumpHash, movementController.IsHighJump);
        
        animator.SetBool(isRollingHash, movementController.IsRolling);
        
        animator.SetBool(JumpHash, jumpingAnimationBuffer);
    }
}
