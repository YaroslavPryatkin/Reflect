using UnityEngine;

public class PlayerAnimationEventCatcher : MonoBehaviour
{
    [SerializeField] private ParticleSystem finishHimThrustBloodParticles;
    [SerializeField] private ParticleSystem alongBladeBloodParticles;
    
    private PlayerMeleeController _playerMeleeController;
    private PlayerTargetLockController _playerTargetLockController;

    private void Awake()
    {
        _playerMeleeController = PlayerManager.Player.GetComponent<PlayerMeleeController>();
        _playerTargetLockController = PlayerManager.Player.GetComponent<PlayerTargetLockController>();
    }

    private int _finishHimLastFrame = 0;
    public void FinishHimEvent()
    {
        if (_finishHimLastFrame != Time.frameCount)
        {
            _playerMeleeController.OnFinishHimEvent();
        }
        _finishHimLastFrame = Time.frameCount;
    }
    
    private int _stabDuringFinishHimLastFrame = 0;
    public void StabDuringFinishHimEvent()
    {
        if (_stabDuringFinishHimLastFrame != Time.frameCount)
        {
            _playerTargetLockController.StubDuringFinishHimEvent();
        }
        _stabDuringFinishHimLastFrame = Time.frameCount;
    }

    private int _thrustParticlesLastFrame = 0;
    public void SpawnThrustParticlesEvent(float duration)
    {
        if (_thrustParticlesLastFrame != Time.frameCount)
        {
            PlayerParticles(finishHimThrustBloodParticles, duration);
        }
        _thrustParticlesLastFrame = Time.frameCount;
    }
    
    
    
    private int _alongBladeParticlesLastFrame = 0;
    public void SpawnAlongBlaseParticlesEvent(float duration)
    {
        if (_alongBladeParticlesLastFrame != Time.frameCount)
        {
            PlayerParticles(alongBladeBloodParticles, duration);
        }
        _alongBladeParticlesLastFrame = Time.frameCount;
    }

    private void PlayerParticles(ParticleSystem particles, float duration)
    {
        var mainModule = particles.main;
        mainModule.duration = duration;
        particles.Play();
    }
}
