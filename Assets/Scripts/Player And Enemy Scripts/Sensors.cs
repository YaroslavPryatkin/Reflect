using UnityEngine;

[DefaultExecutionOrder(-75)]
public abstract class Sensors : MonoBehaviour
{
    [Header("Ground sensors")]
    [SerializeField] private float groundCheckDistance = 1.2f;
    [SerializeField] private float groundCheckRadius = 0.4f;
    [SerializeField] private float groundNormalCheckDistance = 1.4f;
    [SerializeField] protected LayerMask groundLayer;
    
    [Header("Layer masks")]
    [SerializeField] private LayerMask myLayer;
    [SerializeField] private LayerMask enemyLayer;

    protected abstract Vector3 GetVelocity();
    
    public bool IsGrounded { get; private set; }
    public bool FoundGroundNormal { get; private set; } = true;
    public Vector3 GroundNormal { get; private set; } = Vector3.up;
    
    public Vector3 Velocity { get; private set; } = Vector3.zero;
    public Vector3 VelocityAlignedWithGround { get; private set; } = Vector3.zero;
    public float SpeedAlignedWithGround { get; private set; } = 0f;
    public float Speed { get; private set; } = 0f;
    public Vector3 HorizontalVelocity { get; private set; } =  Vector3.zero;
    public Vector3 NormalizedHorizontalVelocity { get; private set; } =  Vector3.zero;
    public float HorizontalSpeed { get; private set; } = 0f;

    public int MyLayerMask => myLayer;
    public int IgnoreMyLayerMask { get; private set; } = 0;

    public int EnemyLayer => enemyLayer;

    protected virtual void Awake()
    {
        IgnoreMyLayerMask = ~myLayer;
    }

    protected virtual void Update()
    {
        GatherGroundSensors();
        UpdateVelocity();
    }
    
    private void FixedUpdate()
    {
        UpdateVelocity();
    }
    
    public void UpdateVelocity()
    {
        Velocity = GetVelocity();
        //NormalizedVelocity = Velocity.normalized;
        Speed = Velocity.magnitude;
        HorizontalVelocity = new Vector3(Velocity.x, 0f, Velocity.z);
        NormalizedHorizontalVelocity = HorizontalVelocity.normalized;
        HorizontalSpeed = HorizontalVelocity.magnitude;
        
        if (FoundGroundNormal)
        {
            VelocityAlignedWithGround = Vector3.ProjectOnPlane(Velocity, GroundNormal);
            SpeedAlignedWithGround = VelocityAlignedWithGround.magnitude;
        }
        else
        {
            SpeedAlignedWithGround = HorizontalSpeed;
            VelocityAlignedWithGround = HorizontalVelocity;
        }
    }
    private void GatherGroundSensors()
    {
        var point = transform.position + Vector3.down * groundCheckDistance;
        IsGrounded = Physics.CheckCapsule(transform.position, point, groundCheckRadius, groundLayer); 
        if (Physics.Raycast(transform.position, Vector3.down, out var hit,
                groundNormalCheckDistance, groundLayer))
        {
            FoundGroundNormal = true;
            GroundNormal = hit.normal;
        }
        else
        {
            FoundGroundNormal = false;
            GroundNormal = Vector3.up;
        }
    }

    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = IsGrounded ? Color.green : Color.deepPink;
        var point = transform.position + Vector3.down * groundCheckDistance;
        Gizmos.DrawWireSphere(point, groundCheckRadius);
    }
    
    protected void DrawSectorRaysGizmo(in Vector3 origin, in Vector3 baseDirection, in Vector3 upDirection, int amountOfRays, float angle, float distance)
    {
        for (int i = 0; i < amountOfRays; i++)
        {
            float t = (float)i / (amountOfRays - 1);
        
            float currentAngle = Mathf.Lerp(-angle, angle, t);
        
            Vector3 direction = (Quaternion.AngleAxis(currentAngle, upDirection) * baseDirection).normalized * distance;

            Gizmos.DrawLine(origin, origin + direction);
        }
    }
}
