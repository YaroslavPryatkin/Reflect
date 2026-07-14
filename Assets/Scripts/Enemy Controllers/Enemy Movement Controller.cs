using UnityEngine;

public class EnemyMovementController : MonoBehaviour
{
    [Header("Following player")] 
    [SerializeField] private float rotationSpeed = 180f;

    private EnemySensors _enemySensors;
    
    
    private float rotationSpeedRad;
    private Vector3 currentRotation;
    
    private void Awake()
    {
        currentRotation = transform.forward;
        rotationSpeedRad =  rotationSpeed * Mathf.Deg2Rad;
        _enemySensors = GetComponent<EnemySensors>();
    }

    private void Update()
    {
        if (_enemySensors.IsSeeingPlayer)
        {
            currentRotation = Vector3.RotateTowards(currentRotation, _enemySensors.HorizontalDirectionToPlayer,  rotationSpeedRad * Time.deltaTime, 0.0f);
            transform.rotation = Quaternion.LookRotation(currentRotation);
        }
    }
}
