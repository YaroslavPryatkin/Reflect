using System;
using UnityEngine;

[DefaultExecutionOrder(-50)]
public class PlayerTargetLockController : MonoBehaviour
{
    [Header("Melee locking")] 
    [SerializeField] private float meleeLockingLockDistance = 5f;
    [SerializeField] private float meleeLockingCenterWeight = 10f;
    [SerializeField] private float meleeLockingDistanceWeight = 5f;
    
    [Header("Starting Locking")]
    [SerializeField] private float maximalStartLockDistance = 15f;
    [SerializeField] private float startScreenCenterWeight = 10f;
    [SerializeField] private float startDistanceWeight = 5f;

    [Header("Change Locking")] 
    [SerializeField] private float reLockDeadZoneAngle = 30f;
    [SerializeField, Range(0,1)] private float reLockAngleFactor = 0.6f;
    [SerializeField] private float maximalReLockDistance = 40f;
    [SerializeField] private float reLockCenterWeight = 10f;
    [SerializeField] private float reLockDistanceWeight = 5f;
    
    [Header("During lock")]
    [SerializeField] private float unlockDistance = 15f;
    [SerializeField] private float deadZoneAngle = 15f;
    [SerializeField] private float unlockAngle = 60f;
    [SerializeField] private float deadZoneSpeed = 10f;
    [SerializeField] private float cameraMinRotationSpeed = 30f;
    [SerializeField] private float cameraMaxRotationSpeed = 80f;
    [SerializeField] private float speedCurvePower = 2f;

    [Header("Enemy finishing")] 
    [SerializeField] private float enemyFinishClipDistance = 1f;
    [SerializeField] private float enemyFinishDistance = 4f;
    [SerializeField] private float enemyFinishMaxAngle = 20f;
    [SerializeField] private float enemyFinishCameraMinRotationSpeed = 30f;
    [SerializeField] private float enemyFinishCameraMaxRotationSpeed = 80f;

    private readonly Collider[] _candidates = new Collider[10];
    
    private PlayerSensors _playerSensors;
    private PlayerSlidingController _playerSlidingController;
    private PlayerGunController _playerGunController;
    private PlayerMeleeController _playerMeleeController;
    private PlayerInputController _playerInputController;
    private PlayerMeleeTransformController _playerMeleeTransformController;
    private PlayerHealthController _playerHealthController;
    private float _smoothFactor;
    
    
    
    private Transform _targetTransform;
    private HealthController _targetHealthController;
    private bool _foundHealthController = false;
    private int _ignoreMyLayerMask;
    
    public Vector3 NormalizedHorizontalDirectionToLockedTarget { get; private set; } = Vector3.zero;
    public Vector3 TargetCameraDirection => TargetPosition - GlobalCameraManager.PlayerCameraPosition;
    public Vector3 TargetPosition => _targetTransform.position;

    public bool HaveFinishHimTarget { get; private set; } = false;
    private EnemyHealthController _finishHimTarget;
    
    
    
    public bool IsLocked { get; private set; } = false;
    public bool IsMeleeLocked { get; private set; } = false;
    public bool IsFinishHimLocked { get;private set; } = false;
    
    private void Awake()
    {
        _playerSensors = GetComponent<PlayerSensors>();
        _playerSlidingController = GetComponent<PlayerSlidingController>();
        _playerGunController = GetComponent<PlayerGunController>();
        _playerMeleeController = GetComponent<PlayerMeleeController>();
        _playerInputController = GetComponent<PlayerInputController>();
        _playerMeleeTransformController = GetComponent<PlayerMeleeTransformController>();
        _playerHealthController = GetComponent<PlayerHealthController>();
        _ignoreMyLayerMask = _playerSensors.IgnoreMyLayerMask;
        _smoothFactor = 2 * reLockAngleFactor;
    }


    private void FindFinishHimTarget()
    {
        var amount = Physics.OverlapSphereNonAlloc(transform.position, enemyFinishDistance, _candidates, _playerSensors.EnemyLayer, QueryTriggerInteraction.Collide);

        var pos = transform.position;
        var lastMaxAngle = float.MaxValue;
        HaveFinishHimTarget = false;

        for (var i = 0; i < amount; ++i)
        {
            var collider = _candidates[i];
            
            var dir = collider.transform.position - pos;

            if (dir.magnitude < enemyFinishClipDistance)
            {
                if (collider.TryGetComponent(out EnemyHealthController health))
                {
                    if (health.CanBeFinished)
                    {
                        _finishHimTarget = health;
                        HaveFinishHimTarget = true;
                        return;
                    }
                }
            }
            else
            {
                var angle = Vector3.Angle(_playerInputController.NonZeroInputMoveVector, dir);

                if (angle <= enemyFinishMaxAngle)
                {
                    if (collider.TryGetComponent(out EnemyHealthController health))
                    {
                        if (health.CanBeFinished)
                        {
                            if (angle < lastMaxAngle)
                            {
                                lastMaxAngle = angle;
                                _finishHimTarget = health;
                                HaveFinishHimTarget = true;
                            }
                        }
                    }
                }
            }
        }
    }
    
    
    private void FindMeleeTarget()
    {
        
        var amount = Physics.OverlapSphereNonAlloc(transform.position, meleeLockingLockDistance, _candidates, _playerSensors.EnemyLayer);
        
        
        IsMeleeLocked = false;
        var minScore = float.MaxValue;

        var camPos = GlobalCameraManager.PlayerCameraPosition;

        for (var i = 0; i < amount; ++i)
        {
            var collider = _candidates[i];
            if (!collider.TryGetComponent<HealthController>(out var health) || health.IsDead) 
                continue;

            var dir = collider.transform.position - transform.position;

            var angle = Vector3.Angle(_playerInputController.NonZeroInputMoveVector, dir);

            var worldDistance = dir.magnitude;

            var score = (angle * meleeLockingCenterWeight) + (worldDistance / meleeLockingLockDistance * meleeLockingDistanceWeight);

            if (score < minScore)
            {
                if (UtilityFunctions.HasLineOfSight(camPos, collider, _ignoreMyLayerMask))
                {
                    minScore = score;
                    _targetTransform = collider.transform;
                    IsMeleeLocked = true;
                }
            }
        }
    }
    
    private bool FindTargetInFoV()
    {
        var amount =  Physics.OverlapSphereNonAlloc(transform.position, maximalStartLockDistance,_candidates,  _playerSensors.EnemyLayer);
        
        var foundTarget = false;
        var minScore = float.MaxValue;
        var camPos = GlobalCameraManager.PlayerCameraPosition;
        
        var screenCenter = new Vector2(0.5f, 0.5f);

        for (var i = 0; i < amount; ++i)
        {
            var collider = _candidates[i];
            var viewportPos = GlobalCameraManager.PlayerCamera.WorldToViewportPoint(collider.transform.position);

            if (viewportPos.z <= 0 || viewportPos.x < -0.5 || viewportPos.x > 1.5 || viewportPos.y < 0 || viewportPos.y > 1)
            {
                continue;
            }
            
            if (!collider.TryGetComponent<HealthController>(out var health) || health.IsDead) 
                continue;

            var targetScreenPos = new Vector2(viewportPos.x, viewportPos.y);
            var distanceToScreenCenter = Vector2.Distance(targetScreenPos, screenCenter);

            var worldDistance = Vector3.Distance(camPos, collider.transform.position);

            var score = (distanceToScreenCenter * startScreenCenterWeight) + (worldDistance / maximalStartLockDistance * startDistanceWeight);

            if (score < minScore)
            {
                if (UtilityFunctions.HasLineOfSight(camPos, collider, _ignoreMyLayerMask))
                {
                    minScore = score;
                    _targetTransform = collider.transform;
                    foundTarget = true;
                }
            }
        }

        return foundTarget;
    }
    


    private float GetSmooth(float x)
    {
        var x2 = x * x;
        var x4 = x2 * x2;
        var x3 = x2 * x;
        var x5 = x4 * x;
        return 6 * x5 - 15 * x4 + 10 * x3;
    }

    private float GetShiftedSmooth(float x)
    {
        x -= 0.5f;
        return GetSmooth(x * (1f + _smoothFactor * (0.5f - Mathf.Abs(x))) + 0.5f);
    }

    public enum FindTargetGeneralResult{DontChange, Found, Nothing}
    public FindTargetGeneralResult FindTargetGeneral(bool useDeadZone, out Transform bestTarget)
    {
        bestTarget = null;

        var targCameraDirection = TargetCameraDirection;
        var inputAngle = Vector3.Angle(targCameraDirection, GlobalLookDirectionManager.CurrentLookDirection);

        if (useDeadZone && inputAngle < reLockDeadZoneAngle)
            return FindTargetGeneralResult.DontChange;
        
        var lookdir = Vector3.Slerp(targCameraDirection,
            GlobalLookDirectionManager.CurrentLookDirection, GetShiftedSmooth(inputAngle/180f));
        
        
        var amount =  Physics.OverlapSphereNonAlloc(transform.position, maximalStartLockDistance, _candidates, _playerSensors.EnemyLayer);
        
        var foundTarget = false;
        var minScore = float.MaxValue;
        var camPos = GlobalCameraManager.PlayerCameraPosition;

        for (var i = 0; i < amount; ++i)
        {
            var collider = _candidates[i];
            if (!collider.TryGetComponent<HealthController>(out var health) || health.IsDead) 
                continue;
            
            var dir = collider.transform.position - camPos;

            var angle = Vector3.Angle(dir, lookdir);

            var worldDistance = Vector3.Distance(camPos, collider.transform.position);

            var score = (angle * reLockCenterWeight) + (worldDistance / maximalReLockDistance * reLockDistanceWeight);

            if (score < minScore)
            {
                if (UtilityFunctions.HasLineOfSight(camPos, collider, _ignoreMyLayerMask))
                {
                    minScore = score;
                    bestTarget = collider.transform;
                    foundTarget = true;
                }

            }
        }

        return foundTarget ? FindTargetGeneralResult.Found : FindTargetGeneralResult.Nothing;
    }
    
    
    
    private void TryLock()
    {
        if (IsFinishHimLocked) return;
        
        if (FindTargetInFoV())
        {
            IsLocked = true;
            GetHealthControllerOnTarget();
        }
        else
        {
            IsLocked = false;
        }
    }
    
    private void TryReLock(bool useDeadZone)
    {
        if (IsFinishHimLocked) return;
        
        switch (FindTargetGeneral(useDeadZone, out var newT))
        {
            case FindTargetGeneralResult.Found:
                IsLocked = true;
                _targetTransform = newT;
                GetHealthControllerOnTarget();
                break;
            case FindTargetGeneralResult.Nothing:
                IsLocked = false;
                break;
        }
    }

    private void GetHealthControllerOnTarget()
    {
        if (_targetTransform.gameObject.TryGetComponent(out HealthController healthController))
        {
            _targetHealthController = healthController;
            _foundHealthController = true;
        }
        else
        {
            _foundHealthController = false;
            Debug.Log("No health controller on target object " + _targetTransform.gameObject.name);
        }
    }

    public void Unlock()
    {
        IsLocked = false; 
    }

    public void FlipLock()
    {
        
        if (IsLocked)
            Unlock();
        else
            TryLock();
    }

    private void DoLockedChecks()
    {
        IsMeleeLocked = false;
        if (!_targetTransform.gameObject.activeSelf || (_foundHealthController && _targetHealthController.IsDead))
        {
            TryReLock(false);
        }
        else if(!_playerMeleeController.ShouldBlockChangeTargetLock)
        {
            TryReLock(true);
        }

        if (!IsLocked)
            return;
        
        if (Vector3.Distance(GlobalCameraManager.PlayerCameraPosition, _targetTransform.position) >=
            unlockDistance)
        {
            IsLocked = false;
        }
    }

    private bool CalculateFields()
    {
        var tmpDir = TargetPosition - transform.position;
        var sqrMagn = tmpDir.sqrMagnitude;
        if (sqrMagn < 0.0001f || Mathf.Abs(tmpDir.y) > 0.995f * Mathf.Sqrt(sqrMagn))
        {
            NormalizedHorizontalDirectionToLockedTarget = transform.forward;
            return false;
        }
        
        NormalizedHorizontalDirectionToLockedTarget = new Vector3(tmpDir.x, 0, tmpDir.z).normalized;
        return true;
    }
    
    private void MoveCamera(float minSpeed, float maxSpeed)
    {
        if (_playerGunController.GunStateValue != UtilityFunctions.BaseActionTransitionsEnum.Base)
            return;
        
        var lookDir = GlobalLookDirectionManager.CurrentLookDirection;
        var horizontalLookDir = new Vector3(lookDir.x, 0, lookDir.z);
        
        var signedInputAngle = Vector3.SignedAngle(TargetCameraDirection, horizontalLookDir, Vector3.up);
        var inputAngle = Mathf.Abs(signedInputAngle);

        if (inputAngle >= unlockAngle)
        {
            IsLocked = false;
            return;
        }

        float currentSpeed;
        if (inputAngle > deadZoneAngle)
        {
            var angleRatio = Mathf.Clamp01(inputAngle / unlockAngle);

            var speedFactor = Mathf.Pow(angleRatio, speedCurvePower);

            currentSpeed = Mathf.Lerp(minSpeed, maxSpeed, speedFactor);
        }
        else
        {
            currentSpeed = deadZoneSpeed;
        }

        var targetYaw = Vector3.SignedAngle(Vector3.forward, NormalizedHorizontalDirectionToLockedTarget, Vector3.up);
        
        var currentYaw = GlobalLookDirectionManager.CurrentYaw;
        
        var newCameraYaw = Mathf.MoveTowardsAngle(currentYaw, targetYaw, currentSpeed * Time.unscaledDeltaTime);
        
        GlobalLookDirectionManager.SetNewYaw(newCameraYaw);
    }

    public void LockFinishHimTarget()
    {
        if (IsFinishHimLocked)
        {
            Debug.LogError("LockFinishHimTarget is already locked: " + _targetHealthController.gameObject.name);
            UnlockFinishHimUnsuccessful();
        }
        // Debug.Log("LockFinishHimTarget on " +  _finishHimTarget.gameObject.name);
        IsFinishHimLocked = true;
        _targetTransform = _finishHimTarget.transform;
        _targetHealthController = _finishHimTarget;
        _finishHimTarget.GettingHitController.IsBeingFinished = true;
        CalculateFields();
    }

    public void UnlockFinishHimUnsuccessful()
    {
        _finishHimTarget.GettingHitController.IsBeingFinished = false;
        IsFinishHimLocked = false;
    }
    
    public void UnlockFinishHimSuccessful()
    {
        //Debug.Log("UnlockFinishHim from " + _finishHimTarget.gameObject.name + ", reason: "+(shouldRegen ? "event" : "interrupt"));
        UnlockFinishHimUnsuccessful();
        _playerHealthController.ChangeHealth(_finishHimTarget.PlayerRegenHpAmount);
        _playerGunController.EarnBullet(_finishHimTarget.PlayerBulletRegenAmount);
        FindFinishHimTarget();
    }

    public void StubDuringFinishHimEvent()
    {
        if (IsFinishHimLocked)
        {
            _finishHimTarget.GettingHitController.GetStabbedDuringDeath(transform.position);
        }
    }

    public void UnlockEverything()
    {
        IsLocked = false;
        IsMeleeLocked = false;
        if (IsFinishHimLocked)
        {
            UnlockFinishHimUnsuccessful();
        }
    }
    
    private void Update()
    {
        if (IsFinishHimLocked)
        {
            // if (!_finishHimTarget.CanBeFinished)
            // {
            //     UnlockFinishHimInternal();
            //     return;
            // }
            
            if (CalculateFields())
            {
                MoveCamera(enemyFinishCameraMinRotationSpeed, enemyFinishCameraMaxRotationSpeed);
            }
        }
        else
        {
            FindFinishHimTarget();
            
            if (IsLocked)
            {
                DoLockedChecks();
                if (IsLocked)
                {
                    if (CalculateFields())
                    {
                        MoveCamera(cameraMinRotationSpeed, cameraMaxRotationSpeed);
                    }
                }
            }
        
            if(!IsLocked)
            {
                FindMeleeTarget();
                if (IsMeleeLocked)
                {
                    CalculateFields();
                }
            }
        }
    }
}
