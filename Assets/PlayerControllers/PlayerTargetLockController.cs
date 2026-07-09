using UnityEngine;

[DefaultExecutionOrder(-50)]
public class PlayerTargetLockController : MonoBehaviour
{
    [Header("Locking")]
    [SerializeField] private float maximalLockDistance = 15f;
    [SerializeField] private float screenCenterWeight = 10f;
    [SerializeField] private float distanceWeight = 5f;
    
    [Header("During lock")]
    [SerializeField] private float unlockDistance = 15f;
    [SerializeField] private float deadZoneAngle = 15f;
    [SerializeField] private float unlockAngle = 60f;
    [SerializeField] private float deadZoneSpeed = 10f;
    [SerializeField] private float cameraMinRotationSpeed = 30f;
    [SerializeField] private float cameraMaxRotationSpeed = 80f;
    [SerializeField] private float speedCurvePower = 2f;

    private PlayerDamageController _playerDamageController;
    private PlayerSensors _playerSensors;
    private PlayerMovementController _playerMovementController;
    private PlayerGunController _playerGunController;

    private void Awake()
    {
        _playerDamageController = GetComponent<PlayerDamageController>();
        _playerSensors = GetComponent<PlayerSensors>();
        _playerMovementController = GetComponent<PlayerMovementController>();
        _playerGunController = GetComponent<PlayerGunController>();
        layerMask = _playerSensors.IgnoreMyLayerMask;
    }

    private Transform targetTransform;
    private HealthController targetHealthController;
    private bool foundHealthController = false;
    private int layerMask;

    public Vector3 TargetDirection { get; private set; } = Vector3.zero;
    public Vector3 TargetPosition { get; private set; } = Vector3.zero;

    public bool IsLocked { get; private set; } = false;

    public void TryLock(bool lockOnlyInScreen = true)
    {
        
        var candidates = Physics.OverlapSphere(transform.position, maximalLockDistance,  _playerDamageController.EnemyLayer);
        
        Transform bestTarget = null;
        var foundTarget = false;
        var minScore = float.MaxValue;

        var screenCenter = new Vector2(0.5f, 0.5f);

        foreach (var collider in candidates)
        {
            var viewportPos = GlobalCameraManager.PlayerCamera.WorldToViewportPoint(collider.transform.position);

            if (lockOnlyInScreen && (viewportPos.z <= 0 || viewportPos.x < -0.5 || viewportPos.x > 1.5 || viewportPos.y < 0 || viewportPos.y > 1))
            {
                continue;
            }
            
            if (!collider.TryGetComponent<HealthController>(out var health) || health.IsDead) 
                continue;

            var targetScreenPos = new Vector2(viewportPos.x, viewportPos.y);
            var distanceToScreenCenter = Vector2.Distance(targetScreenPos, screenCenter);

            var worldDistance = Vector3.Distance(transform.position, collider.transform.position);

            var score = (distanceToScreenCenter * screenCenterWeight) + (worldDistance / maximalLockDistance * distanceWeight);

            if (score < minScore)
            {
                if (Utility.HasLineOfSight(transform.position, collider.transform, maximalLockDistance, layerMask))
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
        }
        else
        {
            IsLocked = false;
        }
    }
    


    public void Unlock()
    {
        IsLocked = false;
    }

    public void ChangeLock()
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
                TryLock(false);
                if(!IsLocked)
                    return;
            }
            
            if (Vector3.Distance(GlobalCameraManager.GetPlayerCameraPosition(), targetTransform.position) >=
                unlockDistance)
            {
                IsLocked = false;
                return;
            }

            TargetPosition = targetTransform.position;
            var tmpDir = TargetPosition - transform.position;
            TargetDirection = new Vector3(tmpDir.x, 0, tmpDir.z).normalized;

            if (_playerGunController.GunState != GunController.GunStateEnum.Non)
                return;
            
            var lookDir = GlobalLookDirectionManager.CurrentLookDirection;
            var horizontalLookDir = new Vector3(lookDir.x, 0, lookDir.z);
            
            var signedInputAngle = Vector3.SignedAngle(TargetDirection, horizontalLookDir, Vector3.up);
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
