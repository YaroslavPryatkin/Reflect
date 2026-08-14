using System.Collections.Generic;
using CustomAttributes;
using UnityEngine;

public class GunAnimationController : MonoBehaviour
{
    [Header("Hand rig")]
    [SerializeField] private int handRigIndex = 1;
    [SerializeField] private IkRigTargetController.OffsetSource barrelToHandIkOffset;
    
    [SerializeField] private bool switchGunWhileNotActive = false;
    [SerializeField] private bool switchGunWhileDead = false;
    [SerializeField,  EnableIf("switchGunWhileNotActive || switchGunWhileDead")] 
    private GameObject handGun;
    [SerializeField,  EnableIf("switchGunWhileNotActive")] 
    private GameObject pocketGun;
    [SerializeField, EnableIf("switchGunWhileDead")] 
    private GameObject deadGun;

    [SerializeField, EnableIf("!switchGunWhileNotActive")]
    private Transform gunPassiveTarget;
    
    [Header("Head")]
    [SerializeField] private int headRigIndex = 0;
    [SerializeField] private Transform headPoint;
    [SerializeField] private bool turnHeadOffWhileNotActive=true;

    [SerializeField, EnableIf("!turnHeadOffWhileNotActive")]
    private Transform headPassiveTarget;
    

    private IkRigTargetController _ikRigTargetControllerHand;
    private IkRigTargetController _ikRigTargetControllerHead;
    protected GunController GunController;

    private IkRigTargetController.TransformSource _handPassiveSource;
    private IkRigTargetController.TransformSource _headPassiveSource;
    private readonly IkRigTargetController.RawSource _headRawSource = new();
    
    private HealthController _healthController;

    protected virtual void Awake()
    {
        if (!TryGetComponent(out AnimationAndRigManager controller))
        {
            Debug.LogError("No AnimationController found!", this);
            enabled = false;
            return;
        }

        GunController = GetComponent<GunController>();
        _healthController = GetComponent<HealthController>();
        
        _ikRigTargetControllerHead = controller.GetRig(headRigIndex);
        _ikRigTargetControllerHead.SetSource(_headRawSource);
        if (!turnHeadOffWhileNotActive)
        {
            _headPassiveSource = new (headPassiveTarget);
            _ikRigTargetControllerHead.SetSource(_headPassiveSource);
        }
        
        
        
        _ikRigTargetControllerHand = controller.GetRig(handRigIndex);
        _ikRigTargetControllerHand.SetSource(barrelToHandIkOffset);
        
        if (!switchGunWhileNotActive)
        {
            _handPassiveSource = new (gunPassiveTarget);
            _ikRigTargetControllerHand.SetSource(_handPassiveSource);
        }

        if (switchGunWhileNotActive)
        {
            pocketGun.SetActive(true);
            handGun.SetActive(false);
        }
        else
        {
            handGun.SetActive(true);
        }
        
        if (switchGunWhileDead)
        {
            deadGun.SetActive(false);
        }
    }
    
    protected virtual void Update()
    {
        if (_healthController.IsDead)
        {
            barrelToHandIkOffset.Weight = 0f;
            _headRawSource.Weight = 0f;
            if (switchGunWhileDead)
            {
                deadGun.SetActive(true);
                handGun.SetActive(false);
                if(switchGunWhileNotActive)
                    pocketGun.SetActive(false);
            }
            else if (switchGunWhileNotActive)
            {
                handGun.SetActive(false);
                pocketGun.SetActive(true);
            }
            return;
        }

        if (GunController.GunStateValue == UtilityFunctions.BaseActionTransitionsEnum.Base)
        {
            barrelToHandIkOffset.Weight = 0f;
            _headRawSource.Weight = 0f;
           
            if (!switchGunWhileNotActive)
            {
                _handPassiveSource.Weight = 1f;
            }
            
            if (!turnHeadOffWhileNotActive)
            {
                _headPassiveSource.Weight = 1f;
            }
            
            SetUndeadGun(false);
            
            return;
        }

        SetUndeadGun(true);

        var targetGunPos = GunController.GunPosition;
        var targetGunRot = Quaternion.LookRotation(GunController.TargetDirection);
        barrelToHandIkOffset.SetTarget(targetGunPos, targetGunRot);
        
        var weight = UtilityFunctions.GetTransitionFraction(GunController.GunState);
        barrelToHandIkOffset.Weight = weight;
        if (!switchGunWhileNotActive)
        {
            _handPassiveSource.Weight = 1f - weight;
        }
        
        
        var headPos = headPoint.position + GunController.TargetHeadDirection;
        _headRawSource.SetTarget(headPos, Quaternion.identity);
        
        _headRawSource.Weight = weight;
        if (!turnHeadOffWhileNotActive)
        {
            _headPassiveSource.Weight = 1f - weight;
        }
    }

    private void SetUndeadGun(bool isActive)
    {
        if (switchGunWhileDead)
        {
            deadGun.SetActive(false);
            if (!switchGunWhileNotActive)
            {
                handGun.SetActive(true);
            }
        }
        
        if (switchGunWhileNotActive)
        {
            handGun.SetActive(isActive);
            pocketGun.SetActive(!isActive);
        }
    }
}
