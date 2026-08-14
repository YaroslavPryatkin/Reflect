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

    private PlayerSensors _playerSensors;
    private PlayerSlidingController _playerSlidingController;
    private PlayerGunController _playerGunController;
    private PlayerMeleeController _playerMeleeController;
    private PlayerInputController _playerInputController;
    private float _smoothFactor;
    
    
    private Transform _targetTransform;
    private HealthController _targetHealthController;
    private bool _foundHealthController = false;
    private int _ignoreMyLayerMask;
    
    public Vector3 NormalizedHorizontalDirectionToLockedTarget { get; private set; } = Vector3.zero;
    public Vector3 TargetCameraDirection { get; private set; } = Vector3.zero;
    public Vector3 TargetPosition { get; private set; } = Vector3.zero;

    
    public bool IsLocked { get; private set; } = false;
    
    
    public bool IsMeleeLocked { get; private set; } = false;
    
    private void Awake()
    {
        _playerSensors = GetComponent<PlayerSensors>();
        _playerSlidingController = GetComponent<PlayerSlidingController>();
        _playerGunController = GetComponent<PlayerGunController>();
        _playerMeleeController = GetComponent<PlayerMeleeController>();
        _playerInputController = GetComponent<PlayerInputController>();
        _ignoreMyLayerMask = _playerSensors.IgnoreMyLayerMask;
        _smoothFactor = 2 * reLockAngleFactor;
    }



    public bool FindMeleeTarget(out Transform bestTarget)
    {
        bestTarget = null;
        
        var candidates = Physics.OverlapSphere(transform.position, meleeLockingLockDistance,  _playerSensors.EnemyLayer);
        
        
        var foundTarget = false;
        var minScore = float.MaxValue;
        
        var camPos = GlobalCameraManager.GetPlayerCameraPosition();
        

        foreach (var collider in candidates)
        {
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
                    bestTarget = collider.transform;
                    foundTarget = true;
                }
            }
        }

        return foundTarget;
    }
    
    public bool FindTargetInFoV(out Transform bestTarget)
    {
        var candidates = Physics.OverlapSphere(transform.position, maximalStartLockDistance,  _playerSensors.EnemyLayer);
        
        bestTarget = null;
        var foundTarget = false;
        var minScore = float.MaxValue;
        var camPos = GlobalCameraManager.GetPlayerCameraPosition();
        
        var screenCenter = new Vector2(0.5f, 0.5f);

        foreach (var collider in candidates)
        {
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
                    bestTarget = collider.transform;
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
        
        var inputAngle = Vector3.Angle(TargetCameraDirection, GlobalLookDirectionManager.CurrentLookDirection);

        if (useDeadZone && inputAngle < reLockDeadZoneAngle)
            return FindTargetGeneralResult.DontChange;
        
        var lookdir = Vector3.Slerp(TargetCameraDirection,
            GlobalLookDirectionManager.CurrentLookDirection, GetShiftedSmooth(inputAngle/180f));
        
        
        var candidates = Physics.OverlapSphere(transform.position, maximalStartLockDistance,  _playerSensors.EnemyLayer);
        
        var foundTarget = false;
        var minScore = float.MaxValue;
        var camPos = GlobalCameraManager.GetPlayerCameraPosition();

        foreach (var collider in candidates)
        {
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
    
    
    private void TryMeleeLock()
    {
        IsMeleeLocked = FindMeleeTarget(out _targetTransform);
    }
    
    private void TryLock()
    {
        if (FindTargetInFoV(out _targetTransform))
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
        
        if (Vector3.Distance(GlobalCameraManager.GetPlayerCameraPosition(), _targetTransform.position) >=
            unlockDistance)
        {
            IsLocked = false;
        }
    }

    private bool CalculateFields()
    {
        TargetPosition = _targetTransform.position;
        
        TargetCameraDirection = TargetPosition - GlobalCameraManager.GetPlayerCameraPosition();
        
        
        var tmpDir = TargetPosition - transform.position;
        if (tmpDir.sqrMagnitude < 0.001f)
        {
            NormalizedHorizontalDirectionToLockedTarget=transform.forward;
            return false;
        }
        
        NormalizedHorizontalDirectionToLockedTarget = new Vector3(tmpDir.x, 0, tmpDir.z).normalized;
        return true;
    }
    
    private void MoveCamera()
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

            currentSpeed = Mathf.Lerp(cameraMinRotationSpeed, cameraMaxRotationSpeed, speedFactor);
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
    
    private void Update()
    {

        if (IsLocked)
        {
            DoLockedChecks();
            if (IsLocked)
            {
                if (CalculateFields())
                {
                    MoveCamera();
                }
            }
        }
        
        if(!IsLocked)
        {
            TryMeleeLock();
            if (IsMeleeLocked)
            {
                CalculateFields();
            }
        }
    }
}
