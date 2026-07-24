using UnityEngine;

public class PlayerMeleeTransformController : MeleeTransformController
{
    private PlayerTargetLockController _playerTargetLockController;
    private PlayerInputController _playerInputController;
    
    protected override void Awake()
    {
        base.Awake();
        _playerTargetLockController= GetComponent<PlayerTargetLockController>();
        _playerInputController = GetComponent<PlayerInputController>();
    }

    protected override void SetTargetPositionAndDirection(out Vector3 targetPosition, out Vector3 targetDirection)
    {
        targetPosition = _playerTargetLockController.TargetPosition;
        targetDirection = _playerTargetLockController.NormalizedHorizontalDirectionToLockedTarget;
    }

    protected override bool DoesHaveTarget()
    {
        return _playerTargetLockController.IsLocked;
    }

    protected override void SetTargetLookDirection(out Vector3 inputDirection)
    {
        if(_playerInputController.IsPlayerPressingWASD)
            inputDirection = _playerInputController.InputMoveVector;
        else if (_playerTargetLockController.IsLocked)
            inputDirection = _playerTargetLockController.NormalizedHorizontalDirectionToLockedTarget;
        else
            inputDirection = _playerInputController.NonZeroInputMoveVector;
    }
}
