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
    
    [Header("Front ground and ledge climb")]
    [SerializeField, Range(2, 10)] private int frontGroundRayCount = 3;
    [SerializeField] private float frontGroundCheckAngle = 10f;
    [SerializeField] private float frontGroundHeightPrecision = 0.05f;
    
    [Header("Ground sensors for front jumps")]
    [SerializeField] private float frontGroundMinimalCheckDistance = 0.7f;
    [SerializeField] private float speedAtMinimalCheckDistance = 2f;
    [SerializeField] private float frontGroundMaximalCheckDistance = 3f;
    [SerializeField] private float speedAtMaximalCheckDistance = 10f;
    [SerializeField] private float frontGroundMinimalHeight = -0.9f;
    [SerializeField] private float frontGroundMaximalHeight = 0.9f;
    [SerializeField] private float forceFrontGroundCheckHeight = -0.9f;
    [SerializeField] private float forceFrontGroundMinimalCheckDistance = 0.7f;
    [SerializeField] private float forceFrontGroundMaximalCheckDistance = 10f;
    
    [Header("Front jump overshoot")]
    [SerializeField] private float minOvershoot = 0.5f;
    [SerializeField] private float speedAtMinOvershoot = 5;
    [SerializeField] private float maxOvershoot = 2;
    [SerializeField] private float speedAtMaxOvershoot = 10;
    [SerializeField] private float rayCastDownDistance = 20f;
    
    [Header("Layer Masks for front jumps")]
    [SerializeField] private LayerMask frontGroundLayer;
    [SerializeField] private LayerMask forceFrontGroundLayer;
    [SerializeField] private LayerMask forbiddenFrontGroundLayer;
    [SerializeField] private LayerMask blockingFrontGroundLayer;
    
    [Header("Ground sensors for ledge climb")] 
    [SerializeField] private float ledgeCheckDistance = 1f;
    [SerializeField] private float ledgeCheckMinimalHeight = -0.1f;
    [SerializeField] private float ledgeCheckMaximalHeight = 1f;
    [SerializeField] private LayerMask ledgeLayer;
    [SerializeField] private LayerMask blockingLedgeLayer;

    
    [Header("Has something in the collider")] 
    [SerializeField] private float hasSomethingInTheColliderCheckRadius = 0.5f;

    
    
    [Header("Dash mask")] 
    [SerializeField] private LayerMask playerCanDashTroughLayers;


    public enum FrontGroundStateEnum
    {
        Nothing, Ledge, Wall, Canopy
    }


    public int DashLayerMask => IgnoreMyLayerMask -  (IgnoreMyLayerMask & playerCanDashTroughLayers);
    
    private Vector3 _frontGroundNormal  = Vector3.zero;
    private Vector3 _frontGroundObstaclePoint = Vector3.zero;
    private float _frontGroundObstacleHeight;
    private FrontGroundStateEnum _frontGroundState;
    
    private Vector3 _ledgeNormal  = Vector3.zero;
    private Vector3 _ledgeObstaclePoint = Vector3.zero;
    private float _ledgeObstacleHeight;
    private FrontGroundStateEnum _ledgeState;

    public FrontGroundStateEnum LedgeState => _ledgeState;
    public float LedgeObstacleHeight => _ledgeObstacleHeight;
    public Vector3 LedgeNormal => _ledgeNormal;
    public Vector3 LedgeObstaclePoint => _ledgeObstaclePoint;
    
    
    public bool IsForceFrontGround { get; private set; } = false;
    
    public FrontGroundStateEnum FrontGroundState => _frontGroundState;

    public float FrontGroundObstacleHeight => _frontGroundObstacleHeight;
    
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
    
    
    
    

    private PlayerInputController _playerInputController;
    private PlayerSlidingController _playerSlidingController;
    private Rigidbody _rb;
    private PlayerFixedDirectionMovementController _playerFixedDirectionMovementController;
    
    private float _speedToDistanceFraction = 0f;
    private float _forceSpeedToDistanceFraction;
    private float _currentFrontGroundCheckDistance = 0f;
    private float _forceCurrentFrontGroundCheckDistance = 0f;

    private int _realAmountOfWallRunRays = 0;
    
    
    
    private float _speedToOvershootFraction;
    private Vector3 _inputForward;
    
    protected override void Awake()
    {
        base.Awake();
        _rb = GetComponent<Rigidbody>();
        
        _speedToDistanceFraction = (frontGroundMaximalCheckDistance - frontGroundMinimalCheckDistance) /
                                  (speedAtMaximalCheckDistance - speedAtMinimalCheckDistance);
        _forceSpeedToDistanceFraction = (forceFrontGroundMaximalCheckDistance - forceFrontGroundMinimalCheckDistance) /
                                       (speedAtMaximalCheckDistance - speedAtMinimalCheckDistance);
        _speedToOvershootFraction = (maxOvershoot - minOvershoot) / (speedAtMaxOvershoot - speedAtMinOvershoot);
        
        _realAmountOfWallRunRays = Math.Max(wallRunRayCount, 1);
        _realAmountOfWallRunRays +=(1 - _realAmountOfWallRunRays % 2);
        
        _playerInputController = GetComponent<PlayerInputController>();
        _playerSlidingController = GetComponent<PlayerSlidingController>();
        _playerFixedDirectionMovementController = GetComponent<PlayerFixedDirectionMovementController>();
        
        
    }
    
    
    public override void Update()
    {
        base.Update();
        GatherForceSlideSensors();
        GatherWallSensors();
        GatherWallRunSensors();
        GatherRailSensors();
        
        _inputForward = _playerInputController.InputMoveVector;
        GatherFrontGroundSensor();
        GatherLedgeSensors();
        
        GatherInTheColliderSensors();
    }


    protected override Vector3 GetVelocity()
    {
        return _rb.linearVelocity;
    }

    public void GatherFinishEnemySensors()
    {
        
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
        var point2 = transform.position + transform.up * (ColliderHalfHeight-hasSomethingInTheColliderCheckRadius);
        HasSomethingInTheCollider = Physics.CheckCapsule(point1, point2, hasSomethingInTheColliderCheckRadius, IgnoreMyLayerMask);
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
    
    
    public void GatherWallRunSensors()
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
        for (var i = 0; i < _realAmountOfWallRunRays; i++)
        {
            var fraction = _realAmountOfWallRunRays > 1 ? (float)i / (_realAmountOfWallRunRays - 1) : 0;
            var currentAngle = Mathf.Lerp(-wallRunCheckAngleBackward, wallRunCheckAngleAhead, fraction);
            
            var rayDirection = Quaternion.AngleAxis(currentAngle * angleSign, transform.up) * baseDirection;
            
            if (Physics.Raycast(_rb.position, rayDirection, out RaycastHit hit, wallRunCheckDistance, wallRunLayer | blockingWallRunLayer, QueryTriggerInteraction.Collide))
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
    private ChainSegmentController _chainSegmentController;
    private float _railMovementDirection = 0f;
    
    private Collider[] _railColliders = new Collider[3];
    public void GatherRailSensors()
    {
        if (IsGrounded)
        {
            IsRailLine = false;
            return;
        }
        
        var point1 = _rb.position + Vector3.up * railLineCheckDistanceStart;
        var point2 = _rb.position + Vector3.up * railLineCheckDistanceEnd;
        int res = Physics.OverlapCapsuleNonAlloc(point1, point2,
            railLineCheckRadius, _railColliders, railLayer, QueryTriggerInteraction.Collide);
        if (res == 0)
        {
            IsRailLine = false;
            return;
        }

        if (IsRailLine)
        {
            bool foundThis = false;
            if (_chainSegmentController.HasNext(out var next, _railMovementDirection))
            {
                for (var i = 0; i < res; ++i)
                {
                    var tr = _railColliders[i].transform;
                    if (tr.gameObject == RailLine.gameObject)
                    {
                        foundThis = true;
                    }
                    else if (tr.gameObject == next.gameObject)
                    {
                        RailLine = next;
                        RailLine.TryGetComponent(out _chainSegmentController);
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
                    var tr = _railColliders[i].transform;
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
                var tr = _railColliders[i].transform;
                var dot = Mathf.Abs(Vector3.Dot(tr.forward, _playerInputController.NonZeroInputMoveVector));
                if (dot > bestDot)
                {
                    bestDot = dot;
                    RailLine = tr;
                }
            }
            IsRailLine = true;
            RailLine.TryGetComponent(out _chainSegmentController);
            _railMovementDirection = Mathf.Sign(Vector3.Dot(RailLine.forward,
                _playerInputController.NonZeroInputMoveVector));
        }
    }

    private void GatherLedgeSensors()
    {
        CheckFrontGround(out _ledgeState, out _ledgeObstacleHeight, out _ledgeNormal, out _ledgeObstaclePoint,
            ledgeCheckMinimalHeight, ledgeCheckMaximalHeight, ledgeCheckDistance, 
            blockingLedgeLayer | ledgeLayer,  ledgeLayer);
    }
    
    private void GatherFrontGroundSensor()
    {
        if (!_playerInputController.IsPlayerPressingWASD)
        {
            _frontGroundState = FrontGroundStateEnum.Nothing;
            _frontGroundObstacleHeight = 0f;
            _frontGroundNormal = Vector3.zero;
            _frontGroundObstaclePoint = Vector3.zero;
            return;
        }

        
        
        _currentFrontGroundCheckDistance = UtilityFunctions.ChangeMeasurementScaleFraction(HorizontalSpeed,
            speedAtMinimalCheckDistance, _speedToDistanceFraction, frontGroundMinimalCheckDistance,
            frontGroundMaximalCheckDistance);
        
        if (TryCheckHeight(frontGroundMaximalHeight, _currentFrontGroundCheckDistance, 
                forbiddenFrontGroundLayer,forbiddenFrontGroundLayer,
                QueryTriggerInteraction.Ignore, out var normalBlocking, out var pointBlocking))
        {
            _frontGroundState = FrontGroundStateEnum.Wall;
            _frontGroundObstacleHeight = frontGroundMaximalHeight;
            _frontGroundNormal = normalBlocking;
            _frontGroundObstaclePoint = pointBlocking;
            return;
        }
        if (TryCheckHeight(frontGroundMinimalHeight, _currentFrontGroundCheckDistance,
                forbiddenFrontGroundLayer,forbiddenFrontGroundLayer,
                QueryTriggerInteraction.Ignore, out normalBlocking, out pointBlocking))
        {
            _frontGroundState = FrontGroundStateEnum.Wall;
            _frontGroundObstacleHeight = 0f;
            _frontGroundNormal = Vector3.zero;
            _frontGroundObstaclePoint = Vector3.zero;
            return;
        }
        
        CheckFrontGround(out _frontGroundState, out _frontGroundObstacleHeight, out _frontGroundNormal,
            out _frontGroundObstaclePoint, frontGroundMinimalHeight, frontGroundMaximalHeight, 
            _currentFrontGroundCheckDistance, blockingFrontGroundLayer | frontGroundLayer, frontGroundLayer);
        
        _forceCurrentFrontGroundCheckDistance = UtilityFunctions.ChangeMeasurementScaleFraction(HorizontalSpeed,
            speedAtMinimalCheckDistance, _forceSpeedToDistanceFraction, forceFrontGroundMinimalCheckDistance,
            forceFrontGroundMaximalCheckDistance);

        if (TryCheckHeight(
                forceFrontGroundCheckHeight,_forceCurrentFrontGroundCheckDistance,
                blockingFrontGroundLayer | forceFrontGroundLayer,forceFrontGroundLayer, QueryTriggerInteraction.Collide,out var forceNormal, out var forcePoint))
        {
            if (FrontGroundState == 0)
            {
                _frontGroundState = FrontGroundStateEnum.Ledge;
                _frontGroundObstacleHeight = frontGroundMinimalHeight;
                _frontGroundNormal = forceNormal;
                _frontGroundObstaclePoint = forcePoint;
            }

            IsForceFrontGround = true;
        }
        else
        {
            IsForceFrontGround = false;
        }
    }

    private void CheckFrontGround(
        out FrontGroundStateEnum state, 
        out float obstacleHeight, 
        out Vector3 groundNormal, 
        out Vector3 obstaclePoint, 
        float minHeight, 
        float maxHeight, 
        float checkDistance, 
        int layerMask,
        int targetLayerMask)
    {
        var hitMin = TryCheckHeight(minHeight, checkDistance, layerMask, targetLayerMask, QueryTriggerInteraction.Ignore, out var normalMin, out var pointMin);
        var hitMax = TryCheckHeight(maxHeight, checkDistance, layerMask, targetLayerMask,QueryTriggerInteraction.Ignore, out var normalMax, out var pointMax);

        if (!hitMin && !hitMax)
        {
            state = FrontGroundStateEnum.Nothing;
            obstacleHeight = 0f;
            groundNormal = Vector3.zero;
            obstaclePoint = Vector3.zero;
        }
        else if (hitMin && hitMax)
        {
            state = FrontGroundStateEnum.Wall;
            obstacleHeight = maxHeight;
            groundNormal = normalMax;
            obstaclePoint = pointMax;
        }
        else if (!hitMin && hitMax)
        {
            state = FrontGroundStateEnum.Canopy;
            obstacleHeight = 0f;
            groundNormal = Vector3.zero;
            obstaclePoint = Vector3.zero;
        }
        else
        {
            state = FrontGroundStateEnum.Ledge;
            
            float low = minHeight;
            float high = maxHeight;
            var bestNormal = normalMin;
            var bestPoint = pointMin;

            while ((high - low) > frontGroundHeightPrecision)
            {
                float mid = low + (high - low) / 2f;
                
                if (TryCheckHeight(mid, checkDistance,layerMask,targetLayerMask, QueryTriggerInteraction.Ignore, out var midNormal, out var midPoint))
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

            obstacleHeight = high;
            groundNormal = bestNormal;
            obstaclePoint = bestPoint;
        }
    }

    private bool TryCheckHeight(float localHeight, float checkDistance, int layerMask, int targetLayerMask, QueryTriggerInteraction triggerInteraction, out Vector3 closestNormal, out Vector3 closestPoint)
    {
        closestNormal = Vector3.zero;
        closestPoint = Vector3.zero;
        
        var origin = transform.position + Vector3.up * localHeight;

        // if (Physics.CheckSphere(origin, 0.04f, layerMask, triggerInteraction))
        // {
        //     // if (Physics.CheckSphere(origin, 0.04f, targetLayerMask, triggerInteraction))
        //     // {
        //     //     closestNormal = Vector3.zero;
        //     //     closestPoint = Vector3.zero;
        //     //     return true;
        //     // }
        //     Debug.Log("origin hit");
        //
        //     return false;
        // }
        
        var hasHit = false;
        var minAngleAbs = float.MaxValue;
        
        for (var i = 0; i < frontGroundRayCount; i++)
        {
            float t = (float)i / (frontGroundRayCount - 1);
            
            float currentAngle = Mathf.Lerp(-frontGroundCheckAngle, frontGroundCheckAngle, t);
            
            var direction = Quaternion.AngleAxis(currentAngle, transform.up) * _inputForward;

            if (Physics.Raycast(origin, direction, out var hit, checkDistance, layerMask, triggerInteraction))
            {
                if (targetLayerMask.Contains(hit))
                {
                    var absAngle = Mathf.Abs(currentAngle);

                    if (absAngle < minAngleAbs)
                    {
                        minAngleAbs = absAngle;
                        closestNormal = hit.normal;
                        closestPoint = hit.point;
                        hasHit = true;
                    }
                }
            }
        }

        return hasHit;
    }

    public bool TryGetForwardGroundEndPoint(out Vector3 endPos)
    {
        var overshoot = UtilityFunctions.ChangeMeasurementScaleFraction(HorizontalSpeed, speedAtMinOvershoot,
            _speedToOvershootFraction, minOvershoot, maxOvershoot);

        var velocityOvershoot = NormalizedHorizontalVelocity * overshoot;
        
        var horizontalGroundNormal = new Vector3(_frontGroundNormal.x,0,_frontGroundNormal.z).normalized;
        var xzEndPos =  _frontGroundObstaclePoint + velocityOvershoot - horizontalGroundNormal * 
            (ColliderRadius-Mathf.Min(Vector3.Dot(velocityOvershoot, -horizontalGroundNormal), ColliderRadius));
        
        xzEndPos = new Vector3(xzEndPos.x, transform.position.y + frontGroundMaximalHeight, xzEndPos.z);
        
        if (Physics.Raycast(xzEndPos, Vector3.down, out RaycastHit hit, rayCastDownDistance, groundLayer))
        {
            endPos = hit.point + ColliderHalfHeightVector;
            return true;
        }
        endPos=Vector3.zero;
        return false;
    }
    
    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        Gizmos.color = IsRailLine ? Color.yellow : Color.purple;
        var point = transform.position + Vector3.up * railLineCheckDistanceStart;
        Gizmos.DrawWireSphere(point, railLineCheckRadius);
        point = transform.position + Vector3.up * railLineCheckDistanceEnd;
        Gizmos.DrawWireSphere(point, railLineCheckRadius);
        
        var col = GetComponent<CapsuleCollider>();
        var halfHeight = col.height / 2 - hasSomethingInTheColliderCheckRadius;
        
        Gizmos.color = HasSomethingInTheCollider ? Color.red : Color.green;
        Gizmos.DrawWireSphere(transform.position + transform.up * halfHeight, hasSomethingInTheColliderCheckRadius);
        
        Gizmos.color = IsNearRightWall ? Color.green : Color.red;
        Gizmos.DrawLine(transform.position, transform.position + transform.right * wallCheckDistance);

        Gizmos.color = IsNearLeftWall ? Color.green : Color.red;
        Gizmos.DrawLine(transform.position, transform.position - transform.right * wallCheckDistance);
        
        Gizmos.color = IsRightWallRun ? Color.yellow : Color.purple;
        DrawWallRunRaysGizmo(transform.right, -1f);

        Gizmos.color = IsLeftWallRun ? Color.yellow : Color.purple;
        DrawWallRunRaysGizmo(-transform.right, 1f);
        
        Gizmos.color = FrontGroundState == FrontGroundStateEnum.Ledge ? Color.cornflowerBlue : Color.coral;
        DrawSectorRaysGizmo(transform.position + transform.up*frontGroundMinimalHeight, transform.forward, transform.up, frontGroundRayCount, frontGroundCheckAngle, frontGroundMinimalCheckDistance);
        
        DrawSectorRaysGizmo(transform.position + transform.up*frontGroundMaximalHeight, transform.forward, transform.up, frontGroundRayCount, frontGroundCheckAngle, frontGroundMaximalCheckDistance);
        
        DrawSectorRaysGizmo(transform.position, transform.forward, transform.up, frontGroundRayCount, frontGroundCheckAngle, _currentFrontGroundCheckDistance);
        
        Gizmos.color = IsForceFrontGround ? Color.cornflowerBlue : Color.coral;
        DrawSectorRaysGizmo(transform.position + transform.up * forceFrontGroundCheckHeight, transform.forward, transform.up, frontGroundRayCount, frontGroundCheckAngle, _forceCurrentFrontGroundCheckDistance);
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
