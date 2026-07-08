using Unity.VisualScripting;
using UnityEngine;

public class PlayerSwordController : MonoBehaviour
{
    [Header("Sheathe")] 
    [SerializeField] private float unsheatheTime = 0.167f;
    [SerializeField] private float sheatheTime = 0.633f;

    public float UnsheatheTime => unsheatheTime;
    public float SheatheTime => sheatheTime;

    [Header("Basic Sword Movement")] 
    [SerializeField] private float closeTurnTime = 0.7f;
    [SerializeField] private float farTurnTime = 0.333f;
    [SerializeField] private float unsheatheToCloseTime = 0.267f;
    [SerializeField] private float farToSheatheTime = 0.867f;
    [SerializeField] private float farTurnToSheatheTime = 0.867f;
    [SerializeField] private float closeToSheatheTime = 0.367f;

    public float UnsheatheToCloseTime => unsheatheToCloseTime;
    public float FarToSheatheTime => farToSheatheTime;
    public float CloseTurnTime => closeTurnTime;
    public float FarTurnTime => farTurnTime;
    public float FarTurnToSheatheTime => farTurnToSheatheTime;
    public float CloseToSheatheTime => closeToSheatheTime;
    
    [Header("Blend times")]
    [SerializeField] private float swordBlendInTime = 0.5f;
    [SerializeField] private float swordBlendOutTime = 0.5f;
    
    public float SwordBlendInTime => swordBlendInTime;
    public float SwordBlendOutTime => swordBlendOutTime;
    


    [Header("Attack")] 
    [SerializeField] private float frontAttackTime = 0.233f;
    [SerializeField] private float backAttackTime  = 0.3f;

    public float FrontAttackTime => frontAttackTime;
    public float BackAttackTime => backAttackTime;

    [Header("Defence")] 
    [SerializeField] private float unsheatheToParryTime = 0.1f;
    [SerializeField] private float anyToParryTime = 0.1f;
    [SerializeField] private float parryTime = 0.4f;
    [SerializeField] private float blockEnterTime = 0.05f;
    [SerializeField] private float blockExitTime = 0.05f;
    [SerializeField] private float blockExitToSheatheTime = 0.1f;
    [SerializeField] private float blockExitToCloseTime = 0.1f;

    
    public float UnsheatheToParryTime => unsheatheToParryTime;
    public float AnyToParryTime => anyToParryTime;
    public float ParryTime => parryTime;
    
    public float BlockEnterTime => blockEnterTime;
    public float BlockExitTime => blockExitTime;
    public float BlockExitToSheatheTime => blockExitToSheatheTime;
    public float BlockExitToCloseTime => blockExitToCloseTime;

    
    [Header("Other")]
    [SerializeField] private float rechargeTime  = 0.2f;
    
    private PlayerInputController _playerInputController;
    private PlayerMovementController _playerMovementController;
    private PlayerJumpController _playerJumpController;
    private PlayerDashController _playerDashController;
    private PlayerForwardJumpingController _playerForwardJumpingController;
    private PlayerLandingController _playerLandingController;
    private PlayerSensors _playerSensors;
    private PlayerGunController _playerGunController;
    private PlayerDamageController _playerDamageController;
    private MeleWeaponHitboxController _swordHitboxController;
    
    public enum SwordStateEnum
    {
        Non,
        BlendIn,
        BlendOut,
        Unsheathe,
        Sheathe,
        FrontAttack,
        BackAttack,
        CloseTurn,
        FarTurn,
        UnsheatheToClose,
        FarToSheathe,
        FarTurnToSheathe,
        CloseToSheathe,
        SheatheToParry,
        AnyToParry,
        Parry,
        Block,
        BlockExit,
        BlockExitToClose,
        BlockExitToSheathe
    }

    private Utility.FractionBlockingValueTimer<SwordStateEnum> swordState = SwordStateEnum.Non;

    private bool canUseSword;
    private bool canInterruptCurrentStateToParry;
    

    
    public SwordStateEnum SwordState => swordState.Value;
    public float SwordStateFraction => swordState.TimeFraction;

    public bool CanBeInterruptedByGun => swordState.Value == SwordStateEnum.Non ||
                                         swordState.Value == SwordStateEnum.Sheathe ||
                                         swordState.Value == SwordStateEnum.Unsheathe || 
                                         swordState.Value == SwordStateEnum.BlendIn ||
                                         swordState.Value == SwordStateEnum.BlendOut;
    
    
    private void ChangeFlags()
    {
        canUseSword = _playerMovementController.WallRunningState == 0 && _playerMovementController.SlidingPhase == 0 &&
                    !_playerDashController.IsDashing && !_playerForwardJumpingController.IsForwardJumping &&
                    _playerLandingController.CanBeInterrupted;
        
        canInterruptCurrentStateToParry = swordState.Value == SwordStateEnum.CloseToSheathe ||
                                          swordState.Value == SwordStateEnum.FarTurnToSheathe ||
                                          swordState.Value == SwordStateEnum.FarToSheathe ||
                                          swordState.Value == SwordStateEnum.FarTurn ||
                                          swordState.Value == SwordStateEnum.CloseTurn || 
                                          swordState.Value == SwordStateEnum.BlockExit;
    }
    
    
   
    private void ChangeSwordState()
    {
        //Debug.Log("Can attack = " + canAttack + ", attack pressed = " + _playerInputController.IsAttackPressed + ", state = " + attackState.Value);
        if (!canUseSword)
        {
            swordState.SetForce(SwordStateEnum.Non, 0);   
        }
        else 
        {
            if (_playerInputController.SwordInputState == PlayerInputController.SwordInputEnum.Parry && canInterruptCurrentStateToParry)
            {
                swordState.SetForce(SwordStateEnum.AnyToParry, anyToParryTime);
            }

            switch (swordState.Value)
            {
                case SwordStateEnum.Non:
                    if (_playerInputController.SwordInputState!=PlayerInputController.SwordInputEnum.Non)
                    {
                        swordState.SetForce(SwordStateEnum.BlendIn, SwordBlendInTime);
                    }
                    break;
                case SwordStateEnum.BlendOut:
                    if (_playerInputController.SwordInputState!=PlayerInputController.SwordInputEnum.Non)
                    {
                        swordState.SetForce(SwordStateEnum.BlendIn, SwordBlendInTime, 1-swordState.TimeFraction);
                    }
                    break;
            }
            
            if (swordState.CanBeChanged)
            {
                switch (swordState.Value)
                {
                    case SwordStateEnum.BlendOut:
                        swordState.SetForce(SwordStateEnum.Non, rechargeTime);
                        break;
                    case SwordStateEnum.BlendIn:
                        swordState.SetForce(SwordStateEnum.Unsheathe, unsheatheTime);
                        break;
                    case SwordStateEnum.Unsheathe:
                        ChangeStateForwardAnyway(SwordStateEnum.SheatheToParry, unsheatheToParryTime,
                            SwordStateEnum.UnsheatheToClose, unsheatheToCloseTime);
                        break;
                    case SwordStateEnum.UnsheatheToClose:
                        ChangeStateForwardAnyway(SwordStateEnum.AnyToParry, anyToParryTime,
                            SwordStateEnum.FrontAttack, frontAttackTime);
                        CheckTransitionToAttackAnyway();
                        break;
                    case SwordStateEnum.FrontAttack:
                        ChangeStateWithOpportunityToParryAndAttack(SwordStateEnum.FarTurn, farTurnTime,
                            SwordStateEnum.FarTurn, farTurnTime, SwordStateEnum.FarToSheathe, farToSheatheTime);
                        _playerDamageController.FinishSwordSwing();
                        break;
                    case SwordStateEnum.FarTurn:
                        ChangeStateWithOpportunityToParryAndAttack(SwordStateEnum.AnyToParry, anyToParryTime,
                            SwordStateEnum.BackAttack, backAttackTime, SwordStateEnum.FarTurnToSheathe,
                            farTurnToSheatheTime);
                        CheckTransitionToAttack();
                        break;
                    case SwordStateEnum.BackAttack:
                        swordState.SetForce(SwordStateEnum.CloseTurn, closeTurnTime);
                        _playerDamageController.FinishSwordSwing();
                        break;
                    case SwordStateEnum.CloseTurn or SwordStateEnum.BlockExitToClose:
                        ChangeStateWithOpportunityToParryAndAttack(SwordStateEnum.AnyToParry, anyToParryTime,
                            SwordStateEnum.FrontAttack, frontAttackTime, SwordStateEnum.CloseToSheathe,
                            closeToSheatheTime);
                        CheckTransitionToAttack();
                        break;
                    case SwordStateEnum.CloseToSheathe or SwordStateEnum.FarToSheathe or SwordStateEnum.FarTurnToSheathe
                        or SwordStateEnum.BlockExitToSheathe:
                        ChangeStateWithOpportunityToParryAndAttack(SwordStateEnum.SheatheToParry, unsheatheToParryTime,
                            SwordStateEnum.UnsheatheToClose, unsheatheToCloseTime, SwordStateEnum.Sheathe, sheatheTime);
                        break;
                    case SwordStateEnum.Sheathe:
                        ChangeStateWithOpportunityToParryAndAttack(SwordStateEnum.Unsheathe, unsheatheTime,
                            SwordStateEnum.Unsheathe, unsheatheTime, SwordStateEnum.BlendOut, SwordBlendOutTime);
                        break;
                    case SwordStateEnum.SheatheToParry or SwordStateEnum.AnyToParry:
                        swordState.SetForce(SwordStateEnum.Parry, parryTime);
                        break;
                    case SwordStateEnum.Parry or SwordStateEnum.Block:
                        if (_playerInputController.IsParryPressed)
                        {
                            swordState.SetForce(SwordStateEnum.Block, 0);
                        }
                        else
                        {
                            swordState.SetForce(SwordStateEnum.BlockExit, blockExitTime);
                        }
                        break;
                    case SwordStateEnum.BlockExit:
                        ChangeStateWithOpportunityToParryAndAttack(SwordStateEnum.BlockExitToClose,
                            blockExitToCloseTime,
                            SwordStateEnum.BlockExitToClose, blockExitToCloseTime, SwordStateEnum.BlockExitToSheathe,
                            blockExitToSheatheTime);
                        break;
                }
            }

            if (swordState.Value != SwordStateEnum.Non)
            {
                _playerLandingController.InterruptLanding();
            }
        }
            
    }

    private void ChangeStateForwardAnyway(SwordStateEnum nextParryState, float nextParryTime, 
        SwordStateEnum nextAttackState, float nextAttackTime)
    {
        if (_playerInputController.LastActiveSwordInputState == PlayerInputController.SwordInputEnum.Parry)
        {
            swordState.SetForce(nextParryState, nextParryTime);
        }
        else
        {
            swordState.SetForce(nextAttackState, nextAttackTime);
        }
    }
    private void ChangeStateWithOpportunityToParryAndAttack(SwordStateEnum nextParryState, float nextParryTime, 
        SwordStateEnum nextAttackState, float nextAttackTime, SwordStateEnum nextIdleState, float nextIdleTime)
    {
        if (_playerInputController.SwordInputState == PlayerInputController.SwordInputEnum.Parry)
        {
            swordState.SetForce(nextParryState, nextParryTime);
        }
        else if(_playerInputController.SwordInputState == PlayerInputController.SwordInputEnum.Attack)
        {
            swordState.SetForce(nextAttackState, nextAttackTime);
        }
        else
        {
            swordState.SetForce(nextIdleState, nextIdleTime);
        }
    }

    private void CheckTransitionToAttackAnyway()
    {
        if (_playerInputController.LastActiveSwordInputState == PlayerInputController.SwordInputEnum.Attack)
        {
            TransitionToAttack();
        }
    }

    private void CheckTransitionToAttack()
    {
        if(_playerInputController.SwordInputState == PlayerInputController.SwordInputEnum.Attack)
        {
            TransitionToAttack();
        }
    }

    private void TransitionToAttack()
    {
        _playerDamageController.StartSwordSwing();
        _playerInputController.ConsumeAttack();
    }
    

    private void Update()
    {
        ChangeFlags();
        ChangeSwordState();
    }

    private void Awake()
    {
        _playerInputController = GetComponent<PlayerInputController>();
        _playerMovementController = GetComponent<PlayerMovementController>();
        _playerJumpController =  GetComponent<PlayerJumpController>();
        _playerDashController =  GetComponent<PlayerDashController>();
        _playerForwardJumpingController = GetComponent<PlayerForwardJumpingController>();
        _playerLandingController = GetComponent<PlayerLandingController>();
        _playerSensors = GetComponent<PlayerSensors>();
        _playerGunController = GetComponent<PlayerGunController>();
        _playerDamageController = GetComponent<PlayerDamageController>();
    }
    
}
