using UnityEngine;



public class PlayerSensors : MonoBehaviour
{
    [SerializeField] private float groundCheckDistance = 1.1f;
    [SerializeField] private float groundCheckRadius = 0.4f;
    [SerializeField] private float farGroundCheckDistance = 1.5f;
    [SerializeField] private float farGroundCheckRadius = 0.4f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField]private float wallCheckDistance = 1.5f;
    [SerializeField] private float wallMinimalAngle = 75f;
    [SerializeField] private LayerMask wallLayer;

    [SerializeField, Range(2, 10)] private int wallRunRayCount = 4;
    [SerializeField] private float wallRunCheckAngle = 30f;
    [SerializeField] private float wallRunCheckDistance = 0.7f;
    [SerializeField] private float wallRunMinimalWallAngle = 80f;
    [SerializeField] private LayerMask wallRunLayer;
    

    public bool IsGrounded { get; private set; }
    public bool IsFarGrounded { get; private set; }
    public bool IsNearLeftWall { get; private set; }
    public bool IsNearRightWall { get; private set; }

    public float LeftWallDistance { get; private set; }
    public float LeftWallDistanceNormalized =>  1 - LeftWallDistance / wallCheckDistance;
    public float RightWallDistance { get; private set; }
    public float RightWallDistanceNormalized => 1 - RightWallDistance / wallCheckDistance;
    
    public bool IsLeftWallRun { get; private set; }
    public bool IsRightWallRun { get; private set; }

    public Vector3 LeftWallRunNormal { get; private set; }
    public Vector3 RightWallRunNormal { get; private set; }
    
    public Vector3 LeftWallRunPoint { get; private set; }
    public Vector3 RightWallRunPoint { get; private set; }
    
    public bool IsLocked { get; private set; } = false;
    public Vector3 LockedTarget { get; private set; } =  Vector3.zero;

    private PlayerMovementInputController _playerMovementInputController;

    private void Awake()
    {
        _playerMovementInputController = GetComponent<PlayerMovementInputController>();
    }
    
    private void Update()
    {
        GatherSensors();
    }

    private void GatherSensors()
    {
        var spherePosition = transform.position + Vector3.down * (groundCheckDistance - groundCheckRadius);
        IsGrounded = Physics.CheckSphere(spherePosition, groundCheckRadius, groundLayer);
        spherePosition = transform.position + Vector3.down * (farGroundCheckDistance - farGroundCheckRadius);
        IsFarGrounded = Physics.CheckSphere(spherePosition, farGroundCheckRadius, groundLayer) || IsGrounded;
        
        RaycastHit hitInfo;

        var cameraRight = GlobalLookDirectionManager.FromCameraLocalToGlobalByZX(Vector3.right);
        
        if (Physics.Raycast(transform.position, cameraRight, out hitInfo, wallCheckDistance, wallLayer)
            && Vector3.Angle(transform.up, hitInfo.normal) > wallMinimalAngle)
        {
            IsNearRightWall = true;
            RightWallDistance = Vector3.Distance(transform.position, hitInfo.point);
        }
        else
        {
            IsNearRightWall = false;
            RightWallDistance = 0;
        }

        if (Physics.Raycast(transform.position, -cameraRight, out hitInfo, wallCheckDistance, wallLayer)
            && Vector3.Angle(transform.up, hitInfo.normal) > wallMinimalAngle)
        {
            IsNearLeftWall = true;
            LeftWallDistance = Vector3.Distance(transform.position, hitInfo.point);
        }
        else
        {
            IsNearLeftWall = false;
            LeftWallDistance = 0;
        }
        
        IsRightWallRun = false;
        IsLeftWallRun = false;
        if (!IsGrounded)
        {
            bool foundRight = TryFindWallRunSector(transform.right, -1f, out RaycastHit rightHit);
            bool foundLeft = TryFindWallRunSector(-transform.right, 1f, out RaycastHit leftHit);

            bool isSameWall = foundRight && foundLeft && 
                              rightHit.collider == leftHit.collider && 
                              Vector3.Dot(rightHit.normal, leftHit.normal) > 0.5f;

            if (isSameWall)
            {
                IsRightWallRun = false;
                IsLeftWallRun = false;
            }
            else
            {
                if (foundRight)
                {
                    IsRightWallRun = true;
                    RightWallRunNormal = rightHit.normal;
                    RightWallRunPoint = rightHit.point;
                }
                
                if (foundLeft)
                {
                    IsLeftWallRun = true;
                    LeftWallRunNormal = leftHit.normal;
                    LeftWallRunPoint = leftHit.point;
                }
            }
        }
    }

    private bool TryFindWallRunSector(Vector3 baseDirection, float angleSign, out RaycastHit bestHit)
    {
        bestHit = new RaycastHit();
        int rays = Mathf.Max(1, wallRunRayCount);
        
        for (int i = 0; i < rays; i++)
        {
            float fraction = rays > 1 ? (float)i / (rays - 1) : 0;
            float currentAngle = Mathf.Lerp(0, wallRunCheckAngle, fraction);
            
            Vector3 rayDirection = Quaternion.AngleAxis(currentAngle * angleSign, transform.up) * baseDirection;
            
            if (Physics.Raycast(transform.position, rayDirection, out RaycastHit hit, wallRunCheckDistance, wallRunLayer))
            {
                if (Vector3.Angle(transform.up, hit.normal) > wallRunMinimalWallAngle)
                {
                    bestHit = hit;
                    return true;
                }
            }
        }
        return false;
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = IsGrounded ? Color.green : Color.red;
        var spherePosition = transform.position + Vector3.down * (groundCheckDistance - groundCheckRadius);
        Gizmos.DrawWireSphere(spherePosition, groundCheckRadius);

        Gizmos.color = IsNearRightWall ? Color.green : Color.red;
        Gizmos.DrawLine(transform.position, transform.position + transform.right * wallCheckDistance);

        Gizmos.color = IsNearLeftWall ? Color.green : Color.red;
        Gizmos.DrawLine(transform.position, transform.position - transform.right * wallCheckDistance);
        
        Gizmos.color = IsRightWallRun ? Color.yellow : Color.purple;
        DrawSectorGizmo(transform.right, -1f);

        Gizmos.color = IsLeftWallRun ? Color.yellow : Color.purple;
        DrawSectorGizmo(-transform.right, 1f);
    }

    private void DrawSectorGizmo(Vector3 baseDirection, float angleSign)
    {
        int rays = Mathf.Max(1, wallRunRayCount);
        for (int i = 0; i < rays; i++)
        {
            float fraction = rays > 1 ? (float)i / (rays - 1) : 0;
            float currentAngle = Mathf.Lerp(0, wallRunCheckAngle, fraction);
            Vector3 rayDirection = Quaternion.AngleAxis(currentAngle * angleSign, transform.up) * baseDirection;
            
            Gizmos.DrawLine(transform.position, transform.position + rayDirection * wallRunCheckDistance);
        }
    }
}
