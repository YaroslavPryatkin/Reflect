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
    
    private float _rotationSpeedWhileAimingRad;
    private Vector3 _currentRotation;
    
    private void Awake()
    {
        _currentRotation = transform.forward;
        _rotationSpeedWhileAimingRad = rotationSpeedWhileAiming * Mathf.Deg2Rad;
        _enemySensors = GetComponent<EnemySensors>();
        _enemyAI = GetComponent<EnemyAI>();
        _agent = GetComponent<NavMeshAgent>();
        _agent.angularSpeed = rotationSpeed;
    }

    private void Update()
    {
        if (_enemyAI.IsAiming)
        {
            _agent.updateRotation = false;
            _currentRotation = Vector3.RotateTowards(_currentRotation,
                _enemySensors.NormalizedHorizontalDirectionFromSeePointToPlayer, 
                _rotationSpeedWhileAimingRad * Time.deltaTime,
                0.0f);
            transform.rotation = Quaternion.LookRotation(_currentRotation);
        }
        else
        {
            _agent.updateRotation = true;
            _currentRotation = transform.forward;
        }
    }
}
