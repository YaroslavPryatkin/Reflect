using System;
using UnityEngine;


public class PlayerSensors : Sensors
{
    [Header("Force slide ground")] 
    [SerializeField] private float forceSlideCheckDistance = 1.2f;
    [SerializeField] private float forceSlideCheckRadius = 0.4f;
    [SerializeField] private LayerMask forceSlideGroundLayer;
    
    [Header("Wall sensors for camera")]
    [SerializeField] private float wallCheckDistance = 1.5f;
    [SerializeField] private float wallMinimalAngle = 75f;
    [SerializeField] private LayerMask wallLayer;
    
    [Header("Wall sensors for wall run")]
    [SerializeField, Range(2, 10)] private int wallRunRayCount = 5;
    [SerializeField] private float wallRunCheckAngleAhead = 30f;
    [SerializeField] private float wallRunCheckAngleBackward = 30f;
    [SerializeField] private float wallRunCheckDistance = 0.7f;
    [SerializeField] private float wallRunMinimalWallAngle = 80f;
    [SerializeField] private LayerMask wallRunLayer;
    [SerializeField] private LayerMask blockingWallRunLayer;

    [Header("Rail line sensors")] 
    [SerializeField] private float railLineCheckDistanceStart = 0.7f;
    [SerializeField] private float railLineCheckDistanceEnd = 1.1f;
    [SerializeField] private float railLineCheckRadius = 0.4f;
    [SerializeField] private LayerMask railLayer;
    
    [Header("Ground sensors for front jumps")]
    [SerializeField, Range(2, 10)] private int frontGroundRayCount = 3;
    [SerializeField] private float frontGroundCheckAngle = 10f;
    [SerializeField] private float frontGroundMinimalCheckDistance = 0.7f;
    [SerializeField] private float speedAtMinimalCheckDistance = 2f;
    [SerializeField] private float frontGroundMaximalCheckDistance = 3f;
    [SerializeField] private float speedAtMaximalCheckDistance = 10f;
    [SerializeField] private float frontGroundMinimalHeight = -0.9f;
    [SerializeField] private float frontGroundMaximalHeight = 0.9f;
    [SerializeField] private float frontGroundHeightPrecision = 0.05f;
    [SerializeField] private float forceFrontGroundCheckHeight = -0.9f;
    [SerializeField] private float forceFrontGroundMinimalCheckDistance = 0.7f;
    [SerializeField] private float forceFrontGroundMaximalCheckDistance = 10f;
    
    [Header("Layer Masks for front jumps")]
    [SerializeField] private LayerMask frontGroundLayer;
    [SerializeField] private LayerMask forceFrontGroundLayer;
    [SerializeField] private LayerMask blockingFrontGroundLayer;
    
    [Header("Front jump overshoot")]
    [SerializeField] private float minOvershoot = 0.5f;
    [SerializeField] private float speedAtMinOvershoot = 5;
    [SerializeField] private float maxOvershoot = 2;
    [SerializeField] private float speedAtMaxOvershoot = 10;
    [SerializeField] private float rayCastDownDistance = 20f;
    
    /// <summary>
    /// 0 - nothing found, 1 - should jump, 2 - full wall, 3 - canopy
    /// </summary>
    public int FrontGroundState { get; private set; } = 0; 
    public float FrontGroundObstacleHeight { get; private set; } = 0f;
    
    private Vector3 frontGroundNormal  = Vector3.zero;
    private Vector3 frontGroundObstaclePoint = Vector3.zero;
    
    public bool isForceFrontGround { get; private set; } = false;
    
    public bool IsNearLeftWall { get; private set; }
    public bool IsNearRightWall { get; private set; }

    public float LeftWallDistance { get; private set; }
    public float LeftWallDistanceNormalized =>  1 - LeftWallDistance / wallCheckDistance;
    public float RightWallDistance { get; private set; }
    public float RightWallDistanceNormalized => 1 - RightWallDistance / wallCheckDistance;
    
    public bool IsLeftWallRun { get; private set; }
    public bool IsRightWallRun { get; private set; }
    public bool IsRailLine { get; private set; }
    
    
    public Vector3 LeftWallRunNormal { get; private set; }
    public Vector3 RightWallRunNormal { get; private set; }
    
    public Vector3 LeftWallRunPoint { get; private set; }
    public Vector3 RightWallRunPoint { get; private set; }

    public bool IsForceSlide { get; private set; }
    
    
    public bool HasSomethingInTheCollider { get; private set; } = false;
    
    
    public CapsuleCollider ThisCollider { get; private set; } = null;
    public float ColliderRadius { get; private set; } = 0f;
    public float ColliderHeight { get; private set; } = 0f;
    public float ColliderHalfHeight { get; private set; } = 0f;
    public float ColliderCenterToTopDistance { get; private set; } = 0f;
    public Vector3 ColliderCenterToTop { get; private set; } = Vector3.zero;
    
    public Vector3 ColliderHalfHeightVector {get; private set;} = Vector3.up;

    private PlayerInputController _playerInputController;
    private PlayerSlidingController _playerSlidingController;
    private Rigidbody rb;
    private PlayerFixedDirectionMovementController _playerFixedDirectionMovementController;
    
    private float speedToDistanceFraction = 0f;
    private float forceSpeedToDistanceFraction;
    private float currentFrontGroundCheckDistance = 0f;
    private float forceCurrentFrontGroundCheckDistance = 0f;

    private int realAmountOfWallRunRays = 0;
    
    
    
    private float speedToOvershootFraction;
    
    protected override void Awake()
    {
        base.Awake();
        rb = GetComponent<Rigidbody>();
        
        speedToDistanceFraction = (frontGroundMaximalCheckDistance - frontGroundMinimalCheckDistance) /
                                  (speedAtMaximalCheckDistance - speedAtMinimalCheckDistance);
        forceSpeedToDistanceFraction = (forceFrontGroundMaximalCheckDistance - forceFrontGroundMinimalCheckDistance) /
                                       (speedAtMaximalCheckDistance - speedAtMinimalCheckDistance);
        speedToOvershootFraction = (maxOvershoot - minOvershoot) / (speedAtMaxOvershoot - speedAtMinOvershoot);
        
        realAmountOfWallRunRays = Math.Max(wallRunRayCount, 1);
        realAmountOfWallRunRays +=(1 - realAmountOfWallRunRays % 2);
        
        _playerInputController = GetComponent<PlayerInputController>();
        _playerSlidingController = GetComponent<PlayerSlidingController>();
        ThisCollider = GetComponent<CapsuleCollider>();
        _playerFixedDirectionMovementController = GetComponent<PlayerFixedDirectionMovementController>();
        
        
        ColliderRadius =  ThisCollider.radius;
        ColliderHeight =  ThisCollider.height;
        ColliderHalfHeight = ColliderHeight / 2;
        ColliderCenterToTopDistance = ColliderHalfHeight - ColliderRadius;
        ColliderCenterToTop = ColliderCenterToTopDistance * Vector3.up;
        ColliderHalfHeightVector = ColliderHalfHeight * Vector3.up;
    }
    
    
    protected override void Update()
    {
        base.Update();
        GatherForceSlideSensors();
        GatherWallSensors();
        GatherWallRunSensors();
        GatherRailSensors();
        GatherFrontGroundSensor();
        GatherInTheColliderSensors();
    }


    protected override Vector3 GetVelocity()
    {
        return rb.linearVelocity;
    }

    private void GatherForceSlideSensors()
    {
        var point = transform.position + Vector3.down * forceSlideCheckDistance;
        IsForceSlide = Physics.CheckCapsule(transform.position, point, forceSlideCheckRadius, forceSlideGroundLayer, QueryTriggerInteraction.Collide); 
    }
    
    private void GatherInTheColliderSensors()
    {
        if (!_playerSlidingController.IsActiveSlidingPhase)
        {
            HasSomethingInTheCollider = false;
            return;
        }

        var point1 = transform.position;
        var point2 = transform.position + transform.up * ColliderCenterToTopDistance;
        HasSomethingInTheCollider = Physics.CheckCapsule(point1, point2, ColliderRadius, IgnoreMyLayerMask);
    }
    private void GatherWallSensors()
    {
        var cameraRight = GlobalLookDirectionManager.FromCameraLocalToGlobalByZX(Vector3.right);
        
        if (Physics.Raycast(transform.position, cameraRight, out RaycastHit hitInfo, wallCheckDistance, wallLayer)
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
    }
    private void GatherWallRunSensors()
    {
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
        bool didHit = false;
        for (var i = 0; i < realAmountOfWallRunRays; i++)
        {
            var fraction = realAmountOfWallRunRays > 1 ? (float)i / (realAmountOfWallRunRays - 1) : 0;
            var currentAngle = Mathf.Lerp(-wallRunCheckAngleBackward, wallRunCheckAngleAhead, fraction);
            
            var rayDirection = Quaternion.AngleAxis(currentAngle * angleSign, transform.up) * baseDirection;
            
            if (Physics.Raycast(transform.position, rayDirection, out RaycastHit hit, wallRunCheckDistance, wallRunLayer | blockingWallRunLayer, QueryTriggerInteraction.Collide))
            {
                if ( ((1 << hit.collider.gameObject.layer) & blockingWallRunLayer) == 0 && Vector3.Angle(transform.up, hit.normal) > wallRunMinimalWallAngle)
                {
                    didHit = true;
                    bestHit = hit;
                    if (currentAngle >= 0)
                    {
                        return true;
                    }
                }
            }
        }
        return didHit;
    }

    
    public Transform RailLine { get; private set; }
    public Vector3 RailLineForward => RailLine.forward * _railMovementDirection;
    private RailSegmentController _railSegmentController;
    private float _railMovementDirection = 0f;
    
    private Collider[] railColliders = new Collider[3];
    private void GatherRailSensors()
    {
        if (IsGrounded)
        {
            IsRailLine = false;
            return;
        }
        
        var point1 = transform.position + Vector3.up * railLineCheckDistanceStart;
        var point2 = transform.position + Vector3.up * railLineCheckDistanceEnd;
        int res = Physics.OverlapCapsuleNonAlloc(point1, point2,
            railLineCheckRadius, railColliders, railLayer, QueryTriggerInteraction.Collide);
        if (res == 0)
        {
            IsRailLine = false;
            return;
        }

        if (IsRailLine)
        {
            bool foundThis = false;
            if (_railSegmentController.HasNext(out var next, _railMovementDirection))
            {
                for (var i = 0; i < res; ++i)
                {
                    var tr = railColliders[i].transform;
                    if (tr.gameObject == RailLine.gameObject)
                    {
                        foundThis = true;
                    }
                    else if (tr.gameObject == next.gameObject)
                    {
                        RailLine = next;
                        RailLine.TryGetComponent(out _railSegmentController);
                        _playerFixedDirectionMovementController.SnapSpeed(RailLineForward);
                        foundThis = true;
                        break;
                    }
                }
            }
            else
            {
                for (var i = 0; i < res; ++i)
                {
                    var tr = railColliders[i].transform;
                    if (tr.gameObject == RailLine.gameObject)
                    {
                        foundThis = true;
                        break;
                    }
                }
            }

            if (!foundThis)
            {
                IsRailLine = false;
            }
        }
        else
        {
            var bestDot = -1f;
            for (var i = 0; i < res; ++i)
            {
                var tr = railColliders[i].transform;
                var dot = Mathf.Abs(Vector3.Dot(tr.forward, _playerInputController.NonZeroInputMoveVector));
                if (dot > bestDot)
                {
                    bestDot = dot;
                    RailLine = tr;
                }
            }
            IsRailLine = true;
            RailLine.TryGetComponent(out _railSegmentController);
            _railMovementDirection = Mathf.Sign(Vector3.Dot(RailLine.forward,
                _playerInputController.NonZeroInputMoveVector));
        }
    }
    
    private Vector3 _inputForward;
    private void GatherFrontGroundSensor()
    {
        if (!_playerInputController.IsPlayerPressingWASD)
        {
            FrontGroundState = 0;
            FrontGroundObstacleHeight = 0f;
            frontGroundNormal = Vector3.zero;
            frontGroundObstaclePoint = Vector3.zero;
            return;
        }

        _inputForward = _playerInputController.InputMoveVector;
        
        currentFrontGroundCheckDistance = UtilityFunctions.ChangeMeasurementScaleFraction(HorizontalSpeed,
            speedAtMinimalCheckDistance, speedToDistanceFraction, frontGroundMinimalCheckDistance,
            frontGroundMaximalCheckDistance);

        if (TryCheckHeight(frontGroundMaximalHeight, currentFrontGroundCheckDistance, blockingFrontGroundLayer,
                QueryTriggerInteraction.Ignore, out var normalBlocking, out var pointBlocking))
        {
            FrontGroundState = 2;
            FrontGroundObstacleHeight = frontGroundMaximalHeight;
            frontGroundNormal = normalBlocking;
            frontGroundObstaclePoint = pointBlocking;
            return;
        }
        if (TryCheckHeight(frontGroundMinimalHeight, currentFrontGroundCheckDistance, blockingFrontGroundLayer, QueryTriggerInteraction.Ignore, out normalBlocking, out pointBlocking))
        {
            FrontGroundState = 2;
            FrontGroundObstacleHeight = 0f;
            frontGroundNormal = Vector3.zero;
            frontGroundObstaclePoint = Vector3.zero;
            return;
        }
        
        bool hitMin = TryCheckHeight(frontGroundMinimalHeight, currentFrontGroundCheckDistance, frontGroundLayer, QueryTriggerInteraction.Ignore, out var normalMin, out var pointMin);
        bool hitMax = TryCheckHeight(frontGroundMaximalHeight, currentFrontGroundCheckDistance, frontGroundLayer, QueryTriggerInteraction.Ignore, out var normalMax, out var pointMax);

        if (!hitMin && !hitMax)
        {
            FrontGroundState = 0;
            FrontGroundObstacleHeight = 0f;
            frontGroundNormal = Vector3.zero;
            frontGroundObstaclePoint = Vector3.zero;
        }
        else if (hitMin && hitMax)
        {
            FrontGroundState = 2;
            FrontGroundObstacleHeight = frontGroundMaximalHeight;
            frontGroundNormal = normalMax;
            frontGroundObstaclePoint = pointMax;
        }
        else if (!hitMin && hitMax)
        {
            FrontGroundState = 3;
            FrontGroundObstacleHeight = 0f;
            frontGroundNormal = Vector3.zero;
            frontGroundObstaclePoint = Vector3.zero;
        }
        else
        {
            FrontGroundState = 1;
            
            float low = frontGroundMinimalHeight;
            float high = frontGroundMaximalHeight;
            var bestNormal = normalMin;
            var bestPoint = pointMin;

            while ((high - low) > frontGroundHeightPrecision)
            {
                float mid = low + (high - low) / 2f;
                
                if (TryCheckHeight(mid, currentFrontGroundCheckDistance,frontGroundLayer, QueryTriggerInteraction.Ignore, out var midNormal, out var midPoint))
                {
                    low = mid;
                    bestNormal = midNormal; 
                    bestPoint = midPoint;
                }
                else
                {
                    high = mid;
                }
            }

            FrontGroundObstacleHeight = high;
            frontGroundNormal = bestNormal;
            frontGroundObstaclePoint = bestPoint;
        }
        
        forceCurrentFrontGroundCheckDistance = UtilityFunctions.ChangeMeasurementScaleFraction(HorizontalSpeed,
            speedAtMinimalCheckDistance, forceSpeedToDistanceFraction, forceFrontGroundMinimalCheckDistance,
            forceFrontGroundMaximalCheckDistance);

        if (TryCheckHeight(forceFrontGroundCheckHeight,forceCurrentFrontGroundCheckDistance,forceFrontGroundLayer, QueryTriggerInteraction.Collide,out var forceNormal, out var forcePoint))
        {
            if (FrontGroundState == 0)
            {
                FrontGroundState = 1;
                FrontGroundObstacleHeight = frontGroundMinimalHeight;
                frontGroundNormal = forceNormal;
                frontGroundObstaclePoint = forcePoint;
            }

            isForceFrontGround = true;
        }
        else
        {
            isForceFrontGround = false;
        }
    }

    private bool TryCheckHeight(float localHeight, float checkDistance, int layerMask, QueryTriggerInteraction triggerInteraction, out Vector3 closestNormal, out Vector3 closestPoint)
    {
        bool hasHit = false;
        closestNormal = Vector3.zero;
        closestPoint = Vector3.zero;
        
        float minAngleAbs = float.MaxValue;
        
        Vector3 origin = transform.position + Vector3.up * localHeight;

        for (int i = 0; i < frontGroundRayCount; i++)
        {
            float t = (float)i / (frontGroundRayCount - 1);
            
            float currentAngle = Mathf.Lerp(-frontGroundCheckAngle, frontGroundCheckAngle, t);
            
            Vector3 direction = Quaternion.AngleAxis(currentAngle, transform.up) * _inputForward;

            if (Physics.Raycast(origin, direction, out RaycastHit hit, checkDistance, layerMask, triggerInteraction))
            {
                float absAngle = Mathf.Abs(currentAngle);
                
                if (absAngle < minAngleAbs)
                {
                    minAngleAbs = absAngle;
                    closestNormal = hit.normal;
                    closestPoint = hit.point;
                    hasHit = true;
                }
            }
        }

        return hasHit;
    }

    public Vector3 GetForwardGroundEndPoint()
    {
        var overshoot = UtilityFunctions.ChangeMeasurementScaleFraction(HorizontalSpeed, speedAtMinOvershoot,
            speedToOvershootFraction, minOvershoot, maxOvershoot);

        var velocityOvershoot = NormalizedHorizontalVelocity * overshoot;
        
        var horizontalGroundNormal = new Vector3(frontGroundNormal.x,0,frontGroundNormal.z).normalized;
        var xzEndPos =  frontGroundObstaclePoint + velocityOvershoot - horizontalGroundNormal * 
            (ColliderRadius-Mathf.Min(Vector3.Dot(velocityOvershoot, -horizontalGroundNormal), ColliderRadius));
        
        xzEndPos = new Vector3(xzEndPos.x, transform.position.y + frontGroundMaximalHeight, xzEndPos.z);
        
        if (Physics.Raycast(xzEndPos, Vector3.down, out RaycastHit hit, rayCastDownDistance, groundLayer))
        {
            xzEndPos =  hit.point;
        }

        return xzEndPos + ColliderHalfHeightVector;
    }
    
    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        Gizmos.color = IsRailLine ? Color.yellow : Color.purple;
        var point = transform.position + Vector3.up * railLineCheckDistanceStart;
        Gizmos.DrawWireSphere(point, railLineCheckRadius);
        point = transform.position + Vector3.up * railLineCheckDistanceEnd;
        Gizmos.DrawWireSphere(point, railLineCheckRadius);
        
        
        Gizmos.color = IsNearRightWall ? Color.green : Color.red;
        Gizmos.DrawLine(transform.position, transform.position + transform.right * wallCheckDistance);

        Gizmos.color = IsNearLeftWall ? Color.green : Color.red;
        Gizmos.DrawLine(transform.position, transform.position - transform.right * wallCheckDistance);
        
        Gizmos.color = IsRightWallRun ? Color.yellow : Color.purple;
        DrawWallRunRaysGizmo(transform.right, -1f);

        Gizmos.color = IsLeftWallRun ? Color.yellow : Color.purple;
        DrawWallRunRaysGizmo(-transform.right, 1f);
        
        Gizmos.color = FrontGroundState == 1 ? Color.cornflowerBlue : Color.coral;
        DrawSectorRaysGizmo(transform.position + transform.up*frontGroundMinimalHeight, transform.forward, transform.up, frontGroundRayCount, frontGroundCheckAngle, frontGroundMinimalCheckDistance);
        
        DrawSectorRaysGizmo(transform.position + transform.up*frontGroundMaximalHeight, transform.forward, transform.up, frontGroundRayCount, frontGroundCheckAngle, frontGroundMaximalCheckDistance);
        
        DrawSectorRaysGizmo(transform.position, transform.forward, transform.up, frontGroundRayCount, frontGroundCheckAngle, currentFrontGroundCheckDistance);
        
        Gizmos.color = isForceFrontGround ? Color.cornflowerBlue : Color.coral;
        DrawSectorRaysGizmo(transform.position + transform.up * forceFrontGroundCheckHeight, transform.forward, transform.up, frontGroundRayCount, frontGroundCheckAngle, forceCurrentFrontGroundCheckDistance);
    }

    private void DrawWallRunRaysGizmo(Vector3 baseDirection, float angleSign)
    {
        var rays = Math.Max(wallRunRayCount, 1);
        rays += (1 - rays % 2);
        for (int i = 0; i < rays; i++)
        {
            float fraction = rays > 1 ? (float)i / (rays - 1) : 0;
            float currentAngle = Mathf.Lerp(-wallRunCheckAngleBackward, wallRunCheckAngleAhead, fraction);
            Vector3 rayDirection = Quaternion.AngleAxis(currentAngle * angleSign, transform.up) * baseDirection;
            
            Gizmos.DrawLine(transform.position, transform.position + rayDirection * wallRunCheckDistance);
        }
    }


}
