using System;
using UnityEngine;

[DefaultExecutionOrder(-50)]
public class PlayerTargetLockController : MonoBehaviour
{
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
    private PlayerMovementController _playerMovementController;
    private PlayerGunController _playerGunController;
    private MeleeToTargetMoveController _meleeToTargetMoveController;
    private float _smoothFactor;
    
    private void Awake()
    {
        _playerSensors = GetComponent<PlayerSensors>();
        _playerMovementController = GetComponent<PlayerMovementController>();
        _playerGunController = GetComponent<PlayerGunController>();
        _meleeToTargetMoveController = GetComponent<MeleeToTargetMoveController>();
        layerMask = _playerSensors.IgnoreMyLayerMask;
        _smoothFactor = 2 * reLockAngleFactor;
    }

    private Transform targetTransform;
    private HealthController targetHealthController;
    private bool foundHealthController = false;
    private int layerMask;

    public Vector3 TargetDirection { get; private set; } = Vector3.zero;
    public Vector3 TargetCameraDirection { get; private set; } = Vector3.zero;
    public Vector3 TargetPosition { get; private set; } = Vector3.zero;

    public bool IsLocked { get; private set; } = false;

    public void TryLock()
    {
        
        var candidates = Physics.OverlapSphere(transform.position, maximalStartLockDistance,  _playerSensors.EnemyLayer);
        
        Transform bestTarget = null;
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
                if (Utility.HasLineOfSight(camPos, collider.transform, maximalStartLockDistance, layerMask))
                {
                    minScore = score;
                    bestTarget = collider.transform;
                    foundTarget = true;
                }
            }
        }

        if (foundTarget)
        {
            IsLocked = true;
            targetTransform = bestTarget;
            if (bestTarget.gameObject.TryGetComponent(out HealthController healthController))
            {
                targetHealthController = healthController;
                foundHealthController = true;
            }
            else
            {
                foundHealthController = false;
                Debug.Log("No health controller on target object " + bestTarget.gameObject.name);
            }
            _meleeToTargetMoveController.SetTarget(bestTarget);
        }
        else
        {
            IsLocked = false;
            _meleeToTargetMoveController.UnsetTarget();
        }
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
        x = x - 0.5f;
        return GetSmooth(x * (1f + _smoothFactor * (0.5f - Mathf.Abs(x))) + 0.5f);
    }
    
    public void TryReLock(bool useDeadZone)
    {
        var inputAngle = Vector3.Angle(TargetCameraDirection, GlobalLookDirectionManager.CurrentLookDirection);

        if (useDeadZone && inputAngle < reLockDeadZoneAngle)
            return;
        
        var lookdir = Vector3.Slerp(TargetCameraDirection,
            GlobalLookDirectionManager.CurrentLookDirection, GetShiftedSmooth(inputAngle/180f));
        
        
        var candidates = Physics.OverlapSphere(transform.position, maximalStartLockDistance,  _playerSensors.EnemyLayer);
        
        Transform bestTarget = null;
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
                if (Utility.HasLineOfSight(camPos, collider.transform, maximalStartLockDistance, layerMask))
                {
                    minScore = score;
                    bestTarget = collider.transform;
                    foundTarget = true;
                }
            }
        }

        if (foundTarget)
        {
            IsLocked = true;
            targetTransform = bestTarget;
            if (bestTarget.gameObject.TryGetComponent(out HealthController healthController))
            {
                targetHealthController = healthController;
                foundHealthController = true;
            }
            else
            {
                foundHealthController = false;
                Debug.Log("No health controller on target object " + bestTarget.gameObject.name);
            }
            _meleeToTargetMoveController.SetTarget(bestTarget);
        }
        else
        {
            IsLocked = false;
            _meleeToTargetMoveController.UnsetTarget();
        }
    }

    public void Unlock()
    {
        IsLocked = false;
        _meleeToTargetMoveController.UnsetTarget();
    }

    public void FlipLock()
    {
        if (IsLocked)
            Unlock();
        else
            TryLock();
    }

    private void Update()
    {

        if (IsLocked)
        {
            if (!targetTransform.gameObject.activeSelf || (foundHealthController && targetHealthController.IsDead))
            {
                TryReLock(false);
            }
            else
            {
                TryReLock(true);
            }
            
            
            if(!IsLocked) 
                return;
            
            if (Vector3.Distance(GlobalCameraManager.GetPlayerCameraPosition(), targetTransform.position) >=
                unlockDistance)
            {
                IsLocked = false;
                return;
            }

            TargetPosition = targetTransform.position;
            var tmpDir = TargetPosition - transform.position;
            TargetDirection = new Vector3(tmpDir.x, 0, tmpDir.z).normalized;

            TargetCameraDirection = TargetPosition - GlobalCameraManager.GetPlayerCameraPosition();
            
            if (_playerGunController.GunStateValue != Utility.BaseActionTransitionsEnum.Base)
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

            var targetYaw = Vector3.SignedAngle(Vector3.forward, TargetDirection, Vector3.up);
            
            var currentYaw = GlobalLookDirectionManager.CurrentYaw;
            
            var newCameraYaw = Mathf.MoveTowardsAngle(currentYaw, targetYaw, currentSpeed * Time.unscaledDeltaTime);
            
            GlobalLookDirectionManager.SetNewYaw(newCameraYaw);
        }
    }
}
