using UnityEngine;
using System.Collections.Generic;
using System;
using CustomAttributes;
using Unity.VisualScripting;
using UnityEngine.Animations.Rigging;
using BaseActionTransitionsEnum = UtilityFunctions.BaseActionTransitionsEnum;

[DefaultExecutionOrder(-50)]
public class IkRigTargetController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private List<RigController> rigs;
    [SerializeField] private Transform ikTarget;
    [SerializeField] private Transform controlledObject;
    [SerializeField] private int index;
    
    [Header("Sources")]
    [SerializeField] private int sourcesArrayCount = 2;
    
    [Header("Speeds")]
    [SerializeField] private float movementSpeed;
    [SerializeField] private float rotationSpeed;
    
    public int Index => index;
    private float _totalWeight;

    public void ChangeTotalWeight(float delta)
    {
        _totalWeight += delta;
    }

    [Serializable]
    public abstract class PositionAndRotationSource
    {
        protected abstract Vector3 PositionInternal { get; }
        protected abstract Quaternion RotationInternal { get; }

        public Vector3 Position => Origin.InverseTransformPoint(PositionInternal);
        public Quaternion Rotation => Quaternion.Inverse(Origin.rotation) * RotationInternal;

        private float _weight = 0f;
        public float Weight
        {
            get => _weight;
            set
            {
                value = Mathf.Clamp01(value);
                _controller.ChangeTotalWeight(value - _weight);
                _weight = value;
            }
        }

        protected Transform Origin;
        private IkRigTargetController _controller;

        public virtual void Initialize(IkRigTargetController controller, Transform origin)
        {
            Origin = origin;
            _controller = controller;
        }
    }
    
    [Serializable]
    public class OffsetSource : PositionAndRotationSource
    {
        [SerializeField] private Transform input;
        [SerializeField] private Transform output;

        public OffsetSource(Transform input, Transform output)
        {
            this.input = input;
            this.output = output;
        }
        
        private Vector3 _posOffset;
        private Quaternion _rotOffset;

        private Vector3 _position;
        private Quaternion _rotation;

        protected override Vector3 PositionInternal => _position;
        protected override Quaternion RotationInternal =>  _rotation;
        
        public override void Initialize(IkRigTargetController controller, Transform origin)
        {
            base.Initialize(controller, origin);
            if (input == null || output == null)
            {
                Debug.LogError("OffsetRigSource: Input or Output transform is missing!", origin);
                return;
            }
            _posOffset = output.InverseTransformPoint(input.position);
            _rotOffset = Quaternion.Inverse(output.rotation) * input.rotation;
        }

        public void SetTarget(Vector3 inputPosition, Quaternion inputRotation)
        {
            _rotation = inputRotation * Quaternion.Inverse(_rotOffset);
            _position = inputPosition - (_rotation * _posOffset);
        }
    }
    
    public class RawSource : PositionAndRotationSource
    {
        private Vector3 _position;
        private Quaternion _rotation;

        protected override Vector3 PositionInternal => _position;
        protected override Quaternion RotationInternal =>  _rotation;

        public void SetTarget(Vector3 inputPosition, Quaternion inputRotation)
        {
            _rotation = inputRotation;
            _position = inputPosition;
        }
    }

    [Serializable]
    public class TransformSource : PositionAndRotationSource
    {
        [SerializeField] private Transform source;

        public TransformSource(Transform source)
        {
            this.source = source;
        }
        
        protected override Vector3 PositionInternal => source.position;
        protected override Quaternion RotationInternal => source.rotation;

        public override void Initialize(IkRigTargetController controller, Transform origin)
        {
            base.Initialize(controller, origin);
            if (source == null)
            {
                Debug.LogError("TransformRigSource: Source transform is missing!", origin);
            }
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

        public void UseSource(PositionAndRotationSource target,  float blendWeight)
        {
            var weight = Mathf.Clamp01(blendWeight);
            Position = Vector3.Lerp(Position, target.Position, weight);
            Rotation = Quaternion.Slerp(Rotation, target.Rotation, weight);
        }

        public void Reset()
        {
            Position = Vector3.zero;
            Rotation = Quaternion.identity;
        }
    }
    
    private readonly PositionAndRotation _current = new ();
    private readonly PositionAndRotation _target = new();
    private PositionAndRotationSource[] _sources;

    private TransformSource _controlledObjectSource;
    
    private void Awake()
    {
        if (TryGetComponent(out AnimationAndRigManager controller))
        {
            controller.AddRig(this);
        }
        else
        {
            Debug.LogError("No AnimationController found!", this);
            enabled = false;
            return;
        }
        _sources = new PositionAndRotationSource[sourcesArrayCount];
        _controlledObjectSource = new TransformSource(controlledObject);
        _controlledObjectSource.Initialize(this, transform);
    }
    

    private void Update()
    {
        if (_totalWeight < 0.01f)
        {
            SetRigWeight(0f);
            _current.UseSource(_controlledObjectSource, 1f);
            return;
        }
        
        _target.Reset();
        for (var i = 0; i < _sources.Length; i++)
        {
            var src = _sources[i];
            
            if (src == null) continue; 
            _target.UseSource(src, src.Weight/_totalWeight);
        }
        
        SetRigWeight(_totalWeight);
        _current.MoveTowards(_target, movementSpeed, rotationSpeed);
        ikTarget.position = transform.TransformPoint(_current.Position);
        ikTarget.rotation = transform.rotation * _current.Rotation;
    }
    
    private void SetRigWeight(float weight)
    {
        weight = Mathf.Clamp01(weight);
        foreach (var rig in rigs)
        {
            rig.SetWeight(weight);
        }
    }
    
    public void SetSource(PositionAndRotationSource source, int sourceIndex)
    {
        if (sourceIndex < 0 || sourceIndex >= _sources.Length)
        {
            Debug.LogError($"Source index {sourceIndex} out of bounds! Array size is {sourcesArrayCount}", this);
            return;
        }

        if (_sources[sourceIndex] != null)
        {
            Debug.LogError($"Source index {sourceIndex} is already taken!", this);
            return;
        }
        source.Initialize(this, transform);
        _sources[sourceIndex] = source;
    }
    
    public void RemoveSource(int sourceIndex)
    {
        if (sourceIndex >= 0 && sourceIndex < _sources.Length)
        {
            _sources[sourceIndex] = null;
        }
    }
    
}
