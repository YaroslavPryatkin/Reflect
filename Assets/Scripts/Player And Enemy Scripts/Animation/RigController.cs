using System;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class RigController : MonoBehaviour
{
    private Rig _rig;
    private float _maxWeightInThisFrame = 0;
    private bool _wasUpdated = false;

    private void Awake()
    {
        _rig = GetComponent<Rig>();
    }
    
    public void SetWeight(float weight)
    {
        _wasUpdated = true;
        if(_maxWeightInThisFrame <  weight)
            _maxWeightInThisFrame = weight;
    }
    
    private void LateUpdate()
    {
        if(_wasUpdated)
            _rig.weight = _maxWeightInThisFrame;
        _maxWeightInThisFrame = 0;
        _wasUpdated = false;
    }
}
