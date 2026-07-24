using System;
using UnityEngine;
using UnityEngine.AI;

public class EnemyRotationController : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 700f;
    [SerializeField] private float rotationSpeedWhileAiming = 90f;

    private EnemySensors _enemySensors;
    private EnemyAI _enemyAI;
    private NavMeshAgent _agent;
    private GunController _gunController;

    private float _fastRotationSpeedRad;
    private float _rotationSpeedWhileAimingRad;
    private Vector3 _currentRotation;

    private UtilityClasses.TemporaryValue<bool> _fastRotation = new(false, true);


    private Vector3 _specificDirection;
    private bool _useSpecificDirection = false;

    public void UseSpecificDirection(Vector3 specificDirection)
    {
        _specificDirection = specificDirection;
        _useSpecificDirection = true;
    }

    public void StopUsingSpecificDirection()
    {
        _useSpecificDirection = false;
    }
    
    private void Awake()
    {
        _currentRotation = transform.forward;
        _rotationSpeedWhileAimingRad = rotationSpeedWhileAiming * Mathf.Deg2Rad;
        
        _enemySensors = GetComponent<EnemySensors>();
        _enemyAI = GetComponent<EnemyAI>();
        _agent = GetComponent<NavMeshAgent>();
        _agent.angularSpeed = rotationSpeed;
    }

    public void FastRotate(float duration)
    {
        _fastRotationSpeedRad = Mathf.PI / duration;
        _fastRotation.Activate(duration);
    }
    
    
    
    private void Update()
    {
        if (_useSpecificDirection)
        {
            _agent.updateRotation = false;
            _currentRotation = _specificDirection;
            transform.rotation = Quaternion.LookRotation(_currentRotation, Vector3.up);
        }
        else if (_enemyAI.IsAiming)
        {
            _agent.updateRotation = false;
            
            if(_fastRotation)
                _currentRotation = Vector3.RotateTowards(_currentRotation,
                    _enemySensors.NormalizedHorizontalDirectionToPlayer, 
                    _fastRotationSpeedRad * Time.deltaTime,
                    0.0f);
            else
                _currentRotation = Vector3.RotateTowards(_currentRotation,
                    _enemySensors.NormalizedHorizontalDirectionToPlayer, 
                    _rotationSpeedWhileAimingRad * Time.deltaTime,
                    0.0f);
            transform.rotation = Quaternion.LookRotation(_currentRotation,Vector3.up);
        }
        else
        {
            _agent.updateRotation = true;
            _currentRotation = transform.forward;
        }
    }
}
