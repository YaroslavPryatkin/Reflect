using UnityEngine;

public class PlayerCameraController : MonoBehaviour
{
    [SerializeField] private float minSpeedToChangeFow = 10;
    [SerializeField] private float maxSpeedToChangeFow = 20;
    [Header("Left - plus, right - minus")]
    [Header("Normal Settings")]
    [SerializeField] private float normalCameraDistance = 3f;
    [SerializeField] private float normalSideShift = 0f;
    [SerializeField] private float normalUpShift = 0f;
    [Header("Target lock Settings")]
    [SerializeField] private float targetLockCameraDistance = 2.5f;
    [SerializeField] private float targetLockSideShift = 0.5f;
    [SerializeField] private float targetLockUpShift = 0.3f;
    [SerializeField] private float targetLockSmoothTime = 0.1f;
    [SerializeField] private float minVelocityDotToChangeSide = 0.3f;
    [SerializeField] private float minSpeedToChangeSide = 1f;
    [Header("Finish him")]
    [SerializeField] private float finishHimCameraDistance = 1.5f;
    [SerializeField] private float finishHimSideShift = 0.8f;
    [SerializeField] private float finishHimUpShift = 0.1f;
    [Header("Wall Settings")]
    [SerializeField] private float wallSideShift = 0.5f;
    [Header("Dash Settings")]
    [SerializeField] private float dashCameraDistance = 1.5f;
    [SerializeField] private float dashSideShift = 0.7f;
    [SerializeField] private float dashUpShift = 0.1f;
    [Header("Aiming Settings")]
    [SerializeField] private float aimingCameraDistance = 1f;
    [SerializeField] private float aimingSideShift = 1f;
    [SerializeField] private float aimingUpShift = 1f;
    [Header("Rail Settings")]
    [SerializeField] private float railCameraDistance = 3f;
    [SerializeField] private float railSideShiftCurveMultiplier = 4f;
    [SerializeField] private float railUpShift = 0.4f;
    [SerializeField] private AnimationCurve railSideShiftCurve =
        new AnimationCurve(new Keyframe(-1f, 1f), new Keyframe(-0.1f, 0.5f),new Keyframe(0f, 0f),new Keyframe(0.1f, 0.5f), new Keyframe(1f, 1f));
    
    private PlayerSensors _playerSensors;
    private PlayerDashController _playerDashController;
    private PlayerGunController _playerGunController;
    private PlayerTargetLockController _playerTargetLockController;
    private PlayerFixedDirectionMovementController _playerFixedDirectionMovementController;

    private float _lastTargetLockSideShiftSign = 1f;
    private float _targetLockSideShiftCurrent = 0f;
    private float _targetLockSideShiftVelocity = 0f;

    private void Awake()
    {
        _playerSensors = GetComponent<PlayerSensors>();
        _playerDashController = GetComponent<PlayerDashController>();
        _playerGunController = GetComponent<PlayerGunController>();
        _playerTargetLockController = GetComponent<PlayerTargetLockController>();
        _playerFixedDirectionMovementController = GetComponent<PlayerFixedDirectionMovementController>();
    }
    
    void Update()
    {
        ChangeFoWFromSpeed();
        MakeCameraSettings();
    }

    
    private void MakeCameraSettings()
    {
        var cameraTransformController = GlobalCameraManager.TransformController;
        
        var wallSideShiftBase = 0f;
        if (_playerSensors.IsNearLeftWall)
        {
            wallSideShiftBase += _playerSensors.LeftWallDistanceNormalized;
        }
        if (_playerSensors.IsNearRightWall)
        {
            wallSideShiftBase -= _playerSensors.RightWallDistanceNormalized;
        }
        var scaledWallSideShift = wallSideShift * wallSideShiftBase;
        
        
        var wantedSideShift = 0f;
        if (_playerDashController.IsDashing)
        {
            cameraTransformController.TargetCameraDistance = dashCameraDistance;
            wantedSideShift = dashSideShift;
            cameraTransformController.TargetUpShift = dashUpShift;
        }
        else if (_playerGunController.GunStateValue!=UtilityFunctions.BaseActionTransitionsEnum.Base)
        {
            cameraTransformController.TargetCameraDistance = aimingCameraDistance;
            wantedSideShift = aimingSideShift;
            cameraTransformController.TargetUpShift = aimingUpShift;
        }
        else if (_playerFixedDirectionMovementController.State == PlayerFixedDirectionMovementController.StateEnum.Line)
        {
            var lookDir = GlobalLookDirectionManager.CurrentLookDirection;
            var velocity = _playerSensors.NormalizedHorizontalVelocity;
            var dot = Vector3.Dot(lookDir, Vector3.Cross(Vector3.up, velocity));
            cameraTransformController.TargetCameraDistance = railCameraDistance;
            wantedSideShift = railSideShiftCurveMultiplier * railSideShiftCurve.Evaluate(dot) * Mathf.Sign(dot);
            cameraTransformController.TargetUpShift = railUpShift;
        }
        else if (_playerTargetLockController.IsFinishHimLocked)
        {
            cameraTransformController.TargetCameraDistance = finishHimCameraDistance;
            cameraTransformController.TargetUpShift = finishHimUpShift;
            SetTargetLockState( finishHimSideShift,false, out wantedSideShift);
        }
        else if (_playerTargetLockController.IsLocked || _playerTargetLockController.IsMeleeLocked || _playerTargetLockController.HaveFinishHimTarget)
        {
            cameraTransformController.TargetCameraDistance = targetLockCameraDistance;
            cameraTransformController.TargetUpShift = targetLockUpShift;
            SetTargetLockState( targetLockSideShift,true, out wantedSideShift);
        }
        else
        {
            _targetLockSideShiftCurrent = 0f;
            _targetLockSideShiftVelocity = 0f;
            _lastTargetLockSideShiftSign = 1f;
            cameraTransformController.TargetCameraDistance = normalCameraDistance;
            wantedSideShift = normalSideShift;
            cameraTransformController.TargetUpShift = normalUpShift;
        }

        if (wallSideShiftBase <= -0.01f)
        {
            cameraTransformController.TargetSideShift = Mathf.Min(scaledWallSideShift, -Mathf.Abs(wantedSideShift));
        }
        else if (wallSideShiftBase >= 0.01f)
        {
            cameraTransformController.TargetSideShift = Mathf.Max(scaledWallSideShift, Mathf.Abs(wantedSideShift));
        }
        else
        {
            cameraTransformController.TargetSideShift = -wantedSideShift;
        }

        cameraTransformController.TargetDirection = GlobalLookDirectionManager.CurrentLookDirection;
    }

    private void SetTargetLockState(float sideShift, bool canChangeSign, out float wantedSideShift)
    {
        if (canChangeSign && _playerSensors.HorizontalSpeed > minSpeedToChangeSide)
        {
            var dir = _playerTargetLockController.NormalizedHorizontalDirectionToLockedTarget;
            var dot = Vector3.Dot(_playerSensors.NormalizedHorizontalVelocity, Vector3.Cross(dir, Vector3.up));
            if (Mathf.Abs(dot) > minVelocityDotToChangeSide)
                _lastTargetLockSideShiftSign = Mathf.Sign(dot);
        }

        _targetLockSideShiftCurrent = Mathf.SmoothDamp(_targetLockSideShiftCurrent,_lastTargetLockSideShiftSign  * sideShift, ref _targetLockSideShiftVelocity,targetLockSmoothTime);
        wantedSideShift = _targetLockSideShiftCurrent;
    }
    

    private void ChangeFoWFromSpeed()
    {
        if (_playerGunController.GunStateValue!=UtilityFunctions.BaseActionTransitionsEnum.Base)
        {
            GlobalCameraManager.FoWController.SetSpeedFactor(0f);
        }
        else if (_playerDashController.IsDashing)
        {
            GlobalCameraManager.FoWController.SetSpeedFactor(1f);
        }
        else
        {
            var speedFraction = (_playerSensors.Speed - minSpeedToChangeFow) /
                                (maxSpeedToChangeFow - minSpeedToChangeFow);
            GlobalCameraManager.FoWController.SetSpeedFactor(speedFraction);
        }
    }
}
