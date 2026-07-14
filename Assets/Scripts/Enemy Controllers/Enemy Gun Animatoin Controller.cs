using UnityEngine;

public class EnemyGunAnimationController : GunAnimationController
{
    [Header("Telegraphing")] 
    [SerializeField] private GameObject telegraphingSphere;
    [SerializeField] private ParticleSystem telegraphingParticles;

    private Vector3 _startingTelegraphingObjectScale;
    
    private EnemyGunController _gunController;
    
    
    protected override void Awake()
    {
        base.Awake();
        _gunController = (EnemyGunController)GunController;
    }

    private void Start()
    {
        _startingTelegraphingObjectScale = telegraphingSphere.transform.localScale;
        telegraphingSphere.SetActive(false);
        telegraphingParticles.Stop();
    }
    
    protected override void Update()
    {
        base.Update();
        UpdateTelegraphingObject();
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
