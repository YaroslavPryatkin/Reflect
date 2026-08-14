using UnityEngine;
using System.Collections.Generic;
using System;
using CustomAttributes;

[DefaultExecutionOrder(-50)]
[RequireComponent(typeof(AnimationAndRigManager))]
public class IkRigTargetController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private List<RigController> rigs;
    [SerializeField] private Transform ikTarget;
    [SerializeField] private Transform controlledObject;
    [SerializeField] private int index;
    
    [Header("Speeds")]
    [SerializeField] private float movementSpeed;
    [SerializeField] private float rotationSpeed;
    
    [Header("Clip")]
    [SerializeField] private bool haveClip = false;
    [SerializeField, EnableIf("haveClip")]
    private uint animationLayer = 3;
    [SerializeField,  EnableIf("haveClip")] 
    private AnimationClip clip;
    [SerializeField,  EnableIf("haveClip")] 
    private AvatarMask layerMask;
    
    private AnimationLayerController _animationLayerController;
    
    public int Index => index;
    
    [Serializable]
    public abstract class PositionAndRotationSource
    {
        protected abstract Vector3 PositionInternal { get; }
        protected abstract Quaternion RotationInternal { get; }

        public Vector3 GetPosition(Transform origin) => origin.InverseTransformPoint(PositionInternal);
        public Quaternion GetRotation(Transform origin) => Quaternion.Inverse(origin.rotation) * RotationInternal;

        private float _weight = 0f;
        public float Weight
        {
            get => _weight;
            set
            {
                value = Mathf.Clamp01(value);
                _weight = value;
            }
        }

        public virtual void Initialize()
        {
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
        
        public override void Initialize()
        {
            if (input == null || output == null)
            {
                Debug.LogError("OffsetRigSource: Input or Output transform is missing!");
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

        public override void Initialize()
        {
            if (source == null)
            {
                Debug.LogError("TransformRigSource: Source transform is missing!");
            }
        }
    }
    
    
    
    private class PositionAndRotation
    {
        public Vector3 Position { get; set; } = Vector3.zero;
        public Quaternion Rotation{ get; set; } = Quaternion.identity;
        private readonly Transform _origin;

        public PositionAndRotation(Transform origin)
        {
            _origin = origin;
        }
        
        public void MoveTowards(PositionAndRotation target, float movementSpeed, float rotationSpeed)
        {
            Rotation = Quaternion.RotateTowards(Rotation, target.Rotation,  rotationSpeed*Time.deltaTime);
            Position = Vector3.MoveTowards(Position, target.Position,  movementSpeed*Time.deltaTime);
        }

        public void UseSource(PositionAndRotationSource target,  float blendWeight, ref Vector4 rotationAccumulator)
        {
            var weight = Mathf.Clamp01(blendWeight);
            Position += target.GetPosition(_origin) * weight;
            
            var targRot = target.GetRotation(_origin);
            
            var qTarget = new Vector4(targRot.x, targRot.y, targRot.z, targRot.w);

            if (Vector4.Dot(rotationAccumulator, qTarget) < 0f)
            {
                qTarget = -qTarget;
            }

            rotationAccumulator += qTarget * weight;
        }
        
        public void ApplyBlendedRotation(ref Vector4 rotationAccumulator)
        {
            float mag = rotationAccumulator.magnitude;
            if (mag > 0.0001f)
            {
                Rotation = new Quaternion(
                    rotationAccumulator.x / mag,
                    rotationAccumulator.y / mag,
                    rotationAccumulator.z / mag,
                    rotationAccumulator.w / mag
                );
            }
            else
            {
                Rotation = Quaternion.identity;
            }
        }

        public void UseSourceRaw(PositionAndRotationSource target)
        {
            Position = target.GetPosition(_origin);
            Rotation = target.GetRotation(_origin);
        }

        public void Reset()
        {
            Position = Vector3.zero;
            Rotation = Quaternion.identity;
        }
    }

    private PositionAndRotation _current;
    private PositionAndRotation _target;
    private TransformSource _controlledObjectSource;
    
    private readonly List<PositionAndRotationSource> _sources = new();

    
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

        if (haveClip && clip == null)
        {
            Debug.LogError("No grab clip found!", this);
            enabled = false;
            return;
        }
        
        _current = new (transform);
        _target = new (transform);
        _controlledObjectSource = new TransformSource(controlledObject);
        
        
        if (haveClip)
        {
            var clips = new HashSet<AnimationClip> { clip };
            _animationLayerController = controller.GetAnimationLayer(clips, animationLayer, layerMask, "Grab clip");
            _animationLayerController.SetPlayableWeight(clip, 1f);
        }
    }
    

    private void Update()
    {
        var totalWeight = 0f;
        
        foreach (var src in _sources)
        {
            totalWeight += src.Weight;
        }
        
        if (totalWeight < 0.01f)
        {
            if (haveClip)
                _animationLayerController.SetLayerWeight(0f);
            
            SetRigWeight(0f);
            _current.UseSourceRaw(_controlledObjectSource);
            return;
        }
        
        _target.Reset();
        var rotationAccumulator = Vector4.zero;
        foreach (var src in _sources)
        {
            _target.UseSource(src, src.Weight/totalWeight, ref rotationAccumulator);
        }
        _target.ApplyBlendedRotation(ref rotationAccumulator);
        
        
        
        SetRigWeight(totalWeight);
        
        if (haveClip)
            _animationLayerController.SetLayerWeight(totalWeight);
        
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
    
    public void SetSource(PositionAndRotationSource source)
    {
        if (!_sources.Contains(source)) _sources.Add(source);
        source.Initialize();
    }
    
    public void RemoveSource(PositionAndRotationSource source)
    {
        _sources.Remove(source);
    }
    
}
