using System.Collections.Generic;
using CustomAttributes;
using UnityEngine;

public class GunAnimationController : MonoBehaviour
{
    [Header("Hand rig")]
    [SerializeField] private int handRigIndex = 1;
    
    [Header("Head")]
    [SerializeField] private int headRigIndex = 0;
    [SerializeField] private Transform headPoint;
    [SerializeField] private bool turnHeadOffWhileNotActive=true;
    
    [SerializeField, EnableIfNot("turnHeadOffWhileNotActive")]
    private int headPassiveTransformSourceRigIndex = 0;
    [SerializeField, EnableIfNot("turnHeadOffWhileNotActive")]
    private int headPassiveRigTargetIndex = 1;
    
    [Header("Not active behavior")]
    [SerializeField] private bool turnHandOffWhileNotActive = false;
    [SerializeField,  EnableIf("turnHandOffWhileNotActive")] private GameObject pocketGun;
    [SerializeField,  EnableIf("turnHandOffWhileNotActive")] private GameObject handGun;

    [SerializeField, EnableIfNot("turnHandOffWhileNotActive")]
    private int handPassiveTransformSourceRigIndex = 0;
    [SerializeField, EnableIfNot("turnHandOffWhileNotActive")]
    private int handPassiveRigTargetIndex = 1;
    
    [Header("Clip")]
    [SerializeField] private bool useBaseClip = false;
    [SerializeField,  EnableIf("useBaseClip")] private AnimationClip baseClip;
    [SerializeField,  EnableIf("useBaseClip")] private AvatarMask baseMask;
    
    private AnimationLayerController _animationLayerController;
    private IkRigTargetController _ikRigTargetControllerHand;
    private IkRigTargetController _ikRigTargetControllerHead;

    protected GunController GunController;


    protected virtual void Awake()
    {
        if (!TryGetComponent(out AnimationController controller))
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
            _animationLayerController = controller.GetAnimationLayer(clips, 3, baseMask);
            _animationLayerController.SetPlayableWeight(baseClip, 1f);
        }
        
        _ikRigTargetControllerHead = controller.GetRig(headRigIndex);
        if(!turnHeadOffWhileNotActive)
            _ikRigTargetControllerHead.SetTransformTarget(headPassiveTransformSourceRigIndex, headPassiveRigTargetIndex);
    
        _ikRigTargetControllerHand = controller.GetRig(handRigIndex);
        if(!turnHandOffWhileNotActive)
            _ikRigTargetControllerHand.SetTransformTarget(handPassiveTransformSourceRigIndex, handPassiveRigTargetIndex);
        

        if (turnHandOffWhileNotActive)
        {
            handGun.SetActive(false);
            pocketGun.SetActive(true);
        }
    }
    
    protected virtual void Update()
    {
        
        if (GunController.GunStateValue == Utility.BaseActionTransitionsEnum.Base)
        {
            
            ZeroRigController(_ikRigTargetControllerHand, turnHandOffWhileNotActive, handPassiveRigTargetIndex);
            ZeroRigController(_ikRigTargetControllerHead, turnHeadOffWhileNotActive, headPassiveRigTargetIndex);

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
        _ikRigTargetControllerHand.SetOffsetTarget(targetGunPos, targetGunRot, 0, 0);
        ChangeRigController(_ikRigTargetControllerHand, turnHandOffWhileNotActive, handPassiveRigTargetIndex);
        
        var headPos = headPoint.position + GunController.TargetHeadDirection;
        _ikRigTargetControllerHead.SetTarget(headPos, Quaternion.identity,0);
        ChangeRigController(_ikRigTargetControllerHead, turnHeadOffWhileNotActive, headPassiveRigTargetIndex);
        
        if(useBaseClip)
            _animationLayerController.SetLayerWeight(GunController.GunState);
    }

    private void ZeroRigController(IkRigTargetController controller, bool turnOffWhileNotActive, int passiveTargetIndex)
    {
        if (turnOffWhileNotActive)
        {
            controller.SetRigWeight(0);
        }
        else
        {
            controller.BlendTargets(0,passiveTargetIndex,0, 1);
        }
    }

    private void ChangeRigController(IkRigTargetController controller, bool turnOffWhileNotActive, int passiveTargetIndex)
    {
        if (turnOffWhileNotActive)
        {
            controller.SetRigWeight(GunController.GunState);
        }
        else
        {
            controller.BlendTargetsInverse(0,passiveTargetIndex,0, GunController.GunState);
        }
    }
}
