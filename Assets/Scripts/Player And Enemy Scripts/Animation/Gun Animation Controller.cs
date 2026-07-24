using System.Collections.Generic;
using CustomAttributes;
using UnityEngine;

public class GunAnimationController : MonoBehaviour
{
    [Header("Hand rig")]
    [SerializeField] private int handRigIndex = 1;
    [SerializeField] private IkRigTargetController.OffsetSource barrelToHandIkOffset;
    
    [SerializeField] private bool turnHandOffWhileNotActive = false;
    [SerializeField,  EnableIf("turnHandOffWhileNotActive")] 
    private GameObject pocketGun;
    [SerializeField,  EnableIf("turnHandOffWhileNotActive")] 
    private GameObject handGun;

    [SerializeField, EnableIfNot("turnHandOffWhileNotActive")]
    private Transform gunPassiveTarget;
    
    [Header("Head")]
    [SerializeField] private int headRigIndex = 0;
    [SerializeField] private Transform headPoint;
    [SerializeField] private bool turnHeadOffWhileNotActive=true;

    [SerializeField, EnableIfNot("turnHeadOffWhileNotActive")]
    private Transform headPassiveTarget;
    
    [Header("Clip")]
    [SerializeField] private bool useBaseClip = false;
    [SerializeField,  EnableIf("useBaseClip")] private AnimationClip baseClip;
    [SerializeField,  EnableIf("useBaseClip")] private AvatarMask baseMask;
    
    private AnimationLayerController _animationLayerController;
    private IkRigTargetController _ikRigTargetControllerHand;
    private IkRigTargetController _ikRigTargetControllerHead;
    protected GunController GunController;

    private IkRigTargetController.TransformSource _handPassiveSource;
    private IkRigTargetController.TransformSource _headPassiveSource;
    private readonly IkRigTargetController.RawSource _headRawSource = new();
    

    protected virtual void Awake()
    {
        if (!TryGetComponent(out AnimationAndRigManager controller))
        {
            Debug.LogError("No AnimationController found!", this);
            enabled = false;
            return;
        }

        GunController = GetComponent<GunController>();

        if (useBaseClip)
        {
            var clips = new HashSet<AnimationClip>();
            clips.Add(baseClip);
            _animationLayerController = controller.GetAnimationLayer(clips, 3, baseMask, "Gun animation");
            _animationLayerController.SetPlayableWeight(baseClip, 1f);
        }
        
        
        _ikRigTargetControllerHead = controller.GetRig(headRigIndex);
        _ikRigTargetControllerHead.SetSource(_headRawSource, 1);
        if (!turnHeadOffWhileNotActive)
        {
            _headPassiveSource = new (headPassiveTarget);
            _ikRigTargetControllerHead.SetSource(_headPassiveSource, 0);
        }
        
        
        
        _ikRigTargetControllerHand = controller.GetRig(handRigIndex);
        _ikRigTargetControllerHand.SetSource(barrelToHandIkOffset, 1);
        if (!turnHandOffWhileNotActive)
        {
            _handPassiveSource = new (gunPassiveTarget);
            _ikRigTargetControllerHand.SetSource(_handPassiveSource, 0);
        }
    
        
        
        if (turnHandOffWhileNotActive)
        {
            handGun.SetActive(false);
            pocketGun.SetActive(true);
        }
    }
    
    protected virtual void Update()
    {
        
        if (GunController.GunStateValue == UtilityFunctions.BaseActionTransitionsEnum.Base)
        {
            barrelToHandIkOffset.Weight = 0f;
            _headRawSource.Weight = 0f;
           
            if (!turnHandOffWhileNotActive)
            {
                _handPassiveSource.Weight = 1f;
            }
            
            if (!turnHeadOffWhileNotActive)
            {
                _headPassiveSource.Weight = 1f;
            }
            
            if (useBaseClip)
                _animationLayerController.SetLayerWeight(0f);
            
            if (turnHandOffWhileNotActive)
            {
                handGun.SetActive(false);
                pocketGun.SetActive(true);
            }
            
            return;
        }

        if (turnHandOffWhileNotActive)
        {
            handGun.SetActive(true);
            pocketGun.SetActive(false);
        }

        var targetGunPos = GunController.GunPosition;
        var targetGunRot = Quaternion.LookRotation(GunController.TargetDirection);
        barrelToHandIkOffset.SetTarget(targetGunPos, targetGunRot);
        
        var weight = UtilityFunctions.GetTransitionFraction(GunController.GunState);
        barrelToHandIkOffset.Weight = weight;
        if (!turnHandOffWhileNotActive)
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


        if(useBaseClip)
            _animationLayerController.SetLayerWeight(UtilityFunctions.GetTransitionFraction(GunController.GunState));
    }
}
