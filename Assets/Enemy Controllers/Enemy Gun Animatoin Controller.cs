using UnityEngine;

public class EnemyGunAnimationController : MonoBehaviour
{
    [Header("Gun")] 
    [SerializeField] private Transform gunBarrelPoint;
    [SerializeField] private Transform activeHandTarget;
    [SerializeField] private Transform passiveHandTarget;
    [Header("Head")]
    [SerializeField] private Transform headPoint;
    [SerializeField] private Transform activeHeadTarget;
    [SerializeField] private Transform passiveHeadTarget;

    [Header("Handling speed")]
    [SerializeField] private float handMovementSpeed;
    [SerializeField] private float handRotationSpeed;
    [SerializeField] private float headRotationSpeed;

    [Header("Telegraphing")] 
    [SerializeField] private GameObject telegraphingSphere;
    [SerializeField] private ParticleSystem telegraphingParticles;

    private Vector3 _startingTelegraphingObjectScale;
    
    private EnemyGunController _gunController;

    private Vector3 _gunLocalPosOffset;
    private Quaternion _gunLocalRotOffset;

    private Vector3 currentHandPos;
    private Quaternion currentHandRotation;
    private Vector3 currentHeadTargetDirection;
    

    private void Awake()
    {
        _gunController = GetComponent<EnemyGunController>();
    }

    private void Start()
    {
        _startingTelegraphingObjectScale = telegraphingSphere.transform.localScale;
        telegraphingSphere.SetActive(false);
        telegraphingParticles.Stop();
        
        _gunLocalPosOffset = activeHandTarget.InverseTransformPoint(gunBarrelPoint.position);
        _gunLocalRotOffset = Quaternion.Inverse(activeHandTarget.rotation) * gunBarrelPoint.rotation;
        
        currentHandPos = passiveHandTarget.position;
        currentHandRotation = passiveHandTarget.rotation;
        
        currentHeadTargetDirection = (passiveHeadTarget.position - headPoint.position).normalized;
    }
    
    private void Update()
    {
        UpdateGunIKTargetTransform();
        UpdateTelegraphingObject();
    }
    
    private void UpdateGunIKTargetTransform()
    {
        if (_gunController.GunState == GunController.GunStateEnum.Non)
        {
            activeHandTarget.position = passiveHandTarget.position;
            activeHandTarget.rotation = passiveHandTarget.rotation;
            activeHeadTarget.position = passiveHeadTarget.position;
            return;
        }
        
        var targetGunPos = _gunController.GunPosition;
        var targetGunRot = Quaternion.LookRotation(_gunController.TargetDirection);

        var targetHandRot = targetGunRot * Quaternion.Inverse(_gunLocalRotOffset);
        var targetHandPos = targetGunPos - (targetHandRot * _gunLocalPosOffset);

        currentHandPos =
            Vector3.MoveTowards(currentHandPos, targetHandPos,  handMovementSpeed*Time.deltaTime);
        currentHandRotation = Quaternion.RotateTowards(currentHandRotation, targetHandRot,  handRotationSpeed*Time.deltaTime);
        
        currentHeadTargetDirection = Vector3.RotateTowards(currentHeadTargetDirection, _gunController.TargetHeadDirection, headRotationSpeed*Time.deltaTime, 0.0f);
        
        
        if (_gunController.GunState == GunController.GunStateEnum.Aiming)
        {
            activeHandTarget.position = currentHandPos;
            activeHandTarget.rotation = currentHandRotation;
            activeHeadTarget.position = headPoint.position + currentHeadTargetDirection;
            return;
        }
        
        var fraction = 1f;
        switch (_gunController.GunState)
        {
            case GunController.GunStateEnum.Starting:
                fraction = Mathf.Clamp01(_gunController.CurrentBlendFraction);
                break;
            case GunController.GunStateEnum.Finishing:
                fraction = 1-Mathf.Clamp01(_gunController.CurrentBlendFraction);
                break;
        }
        
        
        activeHandTarget.position = Vector3.Lerp(passiveHandTarget.position, currentHandPos, fraction);
        activeHandTarget.rotation = Quaternion.Lerp(passiveHandTarget.rotation, currentHandRotation, fraction);
        activeHeadTarget.position = headPoint.position + Vector3.Lerp(passiveHeadTarget.position - headPoint.position, currentHeadTargetDirection, fraction);
    }

    private bool IsShowingTelegraph = false;
    
    private void UpdateTelegraphingObject()
    {
        if (_gunController.IsTelegraphingAttack)
        {
            if (!IsShowingTelegraph)
            {
                telegraphingSphere.SetActive(true);
                telegraphingParticles.Play();
                IsShowingTelegraph = true;
            }

            telegraphingSphere.transform.localScale = _startingTelegraphingObjectScale * _gunController.TelegraphFraction;
        }
        else
        {
            if (IsShowingTelegraph)
            {
                telegraphingSphere.SetActive(false);
                telegraphingParticles.Stop();
                IsShowingTelegraph = false;
            }
        }
    }
    
}
