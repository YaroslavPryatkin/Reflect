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
    private int JumpHash = Animator.StringToHash("Jump");
    private int SpeedHash = Animator.StringToHash("Speed");
    private int wallRunningHash = Animator.StringToHash("WallRunning");
    private int isGroundedHash = Animator.StringToHash("IsGrounded");
    // Start is called once before the first execution of Update after the MonoBehaviour is created

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
        //Debug.Log("Jump");
        animator.SetTrigger(JumpHash);
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
        
        animator.SetInteger(wallRunningHash, movementController.WallRunning);
        
        animator.SetBool(isGroundedHash, playerSensors.IsGrounded);
    }
}
