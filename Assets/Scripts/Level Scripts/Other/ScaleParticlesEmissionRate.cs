using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class ScaleParticlesEmissionRate : MonoBehaviour
{
    [SerializeField] private Vector3 axesMultipliers = Vector3.one;
    [SerializeField] private Transform baseTransform;
    
    
    private ParticleSystem _particleSys;
    private float _baseRateOverTime;
    private Vector3 _previousScale;
    
    void Start()
    {
        _particleSys = GetComponent<ParticleSystem>();
        var emission = _particleSys.emission;
        _baseRateOverTime = emission.rateOverTime.constant;
        _previousScale = Vector3.one;
    }

    void Update()
    {
        if (baseTransform.localScale != _previousScale)
        {
            _previousScale = baseTransform.localScale;
            
            var emission = _particleSys.emission;
            var rate = emission.rateOverTime;

            var scaleFactor = 1f;
            scaleFactor *= GetScale(axesMultipliers.x, _previousScale.x);
            scaleFactor *= GetScale(axesMultipliers.y, _previousScale.y);
            scaleFactor *= GetScale(axesMultipliers.z, _previousScale.z);
            
           rate.constantMin = _baseRateOverTime * scaleFactor;
            rate.constantMax = _baseRateOverTime * scaleFactor;
            emission.rateOverTime = rate;
        }
    }

    private float GetScale(float multiplier, float current)
    {
        return 1f + multiplier * (current - 1f);
    }
}
