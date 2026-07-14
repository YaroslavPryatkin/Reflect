using UnityEngine;
using System.Collections.Generic;
using System;
using CustomAttributes;
using Unity.VisualScripting;
using UnityEngine.Animations.Rigging;
using BaseActionTransitionsEnum = Utility.BaseActionTransitionsEnum;

[DefaultExecutionOrder(-50)]
public class IkRigTargetController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private List<RigController> rigs;
    [SerializeField] private Transform ikTarget;
    [SerializeField] private int index;
    
    [Header("Targets")]
    [SerializeField] private List<Transform> positions;
    [SerializeField] private List<Offset> offsets;
    [SerializeField] private int targetArrayCount = 2;
    
    [Header("Speeds")]
    [SerializeField] private float movementSpeed;
    [SerializeField] private float rotationSpeed;
    
    public int Index => index;
    
    [Serializable]
    public class Offset
    {
        [SerializeField] private Transform input;
        [SerializeField] private Transform output;

        private Vector3 _posOffset;
        private Quaternion _rotOffset;
        
        public void Initialize()
        {
            _posOffset = output.InverseTransformPoint(input.position);
            _rotOffset = Quaternion.Inverse(output.rotation) * input.rotation;
        }

        public void SetPositionAndRotation(PositionAndRotation target, Vector3 inputPosition, Quaternion inputRotation)
        {
            target.Rotation = inputRotation * Quaternion.Inverse(_rotOffset);
            target.Position = inputPosition - (target.Rotation * _posOffset);
        }
    }

    public class PositionAndRotation
    {
        public Vector3 Position { get; set; } = Vector3.zero;
        public Quaternion Rotation{ get; set; } = Quaternion.identity;
        
        public void MoveTowards(PositionAndRotation target, float movementSpeed, float rotationSpeed)
        {
            Rotation = Quaternion.RotateTowards(Rotation, target.Rotation,  rotationSpeed*Time.deltaTime);
            Position = Vector3.MoveTowards(Position, target.Position,  movementSpeed*Time.deltaTime);
        }

        public void Lerp(PositionAndRotation a, PositionAndRotation b, float fraction)
        {
            Position = Vector3.Lerp(a.Position, b.Position, fraction);
            Rotation = Quaternion.Lerp(a.Rotation, b.Rotation, fraction);
        }
    }
    
    private readonly PositionAndRotation _current = new ();
    private PositionAndRotation[] _targets;
    
    private void Awake()
    {
        if (TryGetComponent(out AnimationController controller))
        {
            controller.AddRig(this);
        }
        else
        {
            Debug.LogError("No AnimationController found!", this);
            enabled = false;
            return;
        }

        foreach (var offset in offsets)
        {
            offset.Initialize();
        }

        _targets = new  PositionAndRotation[targetArrayCount];
        for(var i=0;i<targetArrayCount;i++)
            _targets[i]= new PositionAndRotation();
    }
    

    private void Update()
    {
        _current.MoveTowards(_targets[0], movementSpeed, rotationSpeed);
        ikTarget.position = transform.TransformPoint(_current.Position);
        ikTarget.rotation = transform.rotation * _current.Rotation;
    }


    
    public void SetOffsetTarget(Vector3 worldPosition, Quaternion worldRotation, int offsetIndex, int targetIndex)
    {
        offsets[offsetIndex].SetPositionAndRotation(_targets[targetIndex], worldPosition, worldRotation);
        
        _targets[targetIndex].Position = transform.InverseTransformPoint(_targets[targetIndex].Position);
        _targets[targetIndex].Rotation = Quaternion.Inverse(transform.rotation) * _targets[targetIndex].Rotation;
    }

    public void SetTransformTarget(int positionTransformIndex, int targetIndex)
    {
        var targetT = positions[positionTransformIndex];
        _targets[targetIndex].Position = transform.InverseTransformPoint(targetT.position);
        _targets[targetIndex].Rotation = Quaternion.Inverse(transform.rotation) * targetT.rotation;
        
    }

    public void SetTarget(Vector3 worldPosition, Quaternion worldRotation, int targetIndex)
    {
        _targets[targetIndex].Position = transform.InverseTransformPoint(worldPosition);
        _targets[targetIndex].Rotation = Quaternion.Inverse(transform.rotation) * worldRotation;
    }

    public void BlendTargets(int a, int b, int result, float fraction)
    {
        _targets[result].Lerp(_targets[a], _targets[b], fraction);
    }
    
    public void BlendTargetsInverse(int a, int b, int result, Utility.IFractionTimer<BaseActionTransitionsEnum> baseActionTransitionsState)
    {
        _targets[result].Lerp(_targets[a], _targets[b], 1f - Utility.GetTransitionFraction(baseActionTransitionsState));
    }
    

    public void SetRigWeight(float weight)
    {
        SetRigWeightInternal( Mathf.Clamp01(weight));
    }
    
    
    public void SetRigWeight(float fraction, BaseActionTransitionsEnum baseActionTransitionsState)
    {
        SetRigWeightInternal(Utility.GetTransitionFraction(fraction, baseActionTransitionsState));
    }
    
    public void SetRigWeight(Utility.IFractionTimer<BaseActionTransitionsEnum> baseActionTransitionsState)
    {
        SetRigWeightInternal(Utility.GetTransitionFraction(baseActionTransitionsState));
    }

    private void SetRigWeightInternal(float weight)
    {
        foreach (var rig in rigs)
        {
            rig.SetWeight(weight);
        }
    }
}
