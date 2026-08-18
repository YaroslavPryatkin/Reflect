using System;
using CustomAttributes;
using UnityEngine;

public class DoorController : MonoBehaviour
{

    public enum TypeEnum
    {
        Slide, Rotate
    }

    [Header("Door transform")] 
    [SerializeField] private Transform door;
    
    [Header("Settings")]
    [SerializeField] private TypeEnum type;
    [SerializeField] private float animationDuration=1f;
    [SerializeField] private AnimationCurve curve = new (new Keyframe(0, 0), new Keyframe(1, 1));
    [SerializeField] private Transform pointClose, pointOpen;
    [SerializeField, EnableIf("type == Rotate")] private Transform pivot;

    
    
    private enum StateEnum
    {
        Closed, Opening, Opened, Closing
    }

    private readonly UtilityTimers.FractionBlockingValueTimer<StateEnum> _state = 
        StateEnum.Closed;

    private float _startDistance;

    private Quaternion initialLocalRotation;

    private void Awake()
    {
        if (door.parent == transform)
        {
            door.SetParent(transform.parent, true);
        }

        if (type == TypeEnum.Rotate)
        {
            _startDistance = Vector3.Distance(pivot.position, door.position);
           
            var initDirClose = (door.position - pivot.position).normalized;

            if (initDirClose.sqrMagnitude == 0) initDirClose = transform.up;

            var initDirOpen = transform.up;
            var initXAxis = Vector3.Cross(initDirClose, initDirOpen);

            if (initXAxis.sqrMagnitude < 0.001f)
            {
                initDirOpen = transform.forward;
                initXAxis = Vector3.Cross(initDirClose, initDirOpen);
            }

            initXAxis.Normalize();
            var initYAxis = initDirClose;
            var initZAxis = Vector3.Cross(initXAxis, initYAxis).normalized;

            var initialBasisRotation = Quaternion.LookRotation(initZAxis, initYAxis);

            initialLocalRotation = Quaternion.Inverse(initialBasisRotation) * door.rotation;

        }
    }

    private void Update()
    {
        if (_state.Value == StateEnum.Closed)
        {
            switch (type)
            {
                case TypeEnum.Slide:
                    door.SetPositionAndRotation(pointClose.position, pointClose.rotation);
                    break;
                case TypeEnum.Rotate:
                    UpdateDoorRotation(0f);
                    break;
            }
            return;
        }
        if (_state.Value == StateEnum.Opened)
        {
            switch (type)
            {
                case TypeEnum.Slide:
                    door.SetPositionAndRotation(pointOpen.position, pointOpen.rotation);
                    break;
                case TypeEnum.Rotate:
                    UpdateDoorRotation(1f);
                    break;
            }
            return;
        }
        
        var fraction = 0f;
        switch (_state.Value)
        {
            case StateEnum.Opening:
                fraction = curve.Evaluate(Mathf.Clamp01(_state.TimeFraction));
                if(_state.CanBeChanged)
                    _state.SetForce(StateEnum.Opened);
                break;
            case StateEnum.Closing:
                fraction = curve.Evaluate(Mathf.Clamp01(1f - _state.TimeFraction));
                if(_state.CanBeChanged)
                    _state.SetForce(StateEnum.Closed);
                break;
        }

        switch (type)
        {
            case TypeEnum.Slide:
                door.SetPositionAndRotation(
                    Vector3.Lerp(pointClose.position, pointOpen.position, fraction),
                    Quaternion.Lerp(pointClose.rotation, pointOpen.rotation, fraction));
                break;
            case TypeEnum.Rotate:
                UpdateDoorRotation(fraction);
                break;
        }
    }
    
    private void UpdateDoorRotation(float fraction)
    {
        var dirClose = (pointClose.position - pivot.position).normalized;
        var dirOpen = (pointOpen.position - pivot.position).normalized;

        door.position = pivot.position + Vector3.Slerp(dirClose, dirOpen, fraction) * _startDistance;
        
        var xAxis = Vector3.Cross(dirClose, dirOpen);
        
        if (xAxis.sqrMagnitude < 0.001f) return; 
        
        xAxis.Normalize();
        var yAxis = dirClose;
        var zAxis = Vector3.Cross(xAxis, yAxis).normalized;

        var currentBasisRotation = Quaternion.LookRotation(zAxis, yAxis);

        var totalAngle = Vector3.Angle(dirClose, dirOpen);
        var currentAngle = totalAngle * fraction;

        var dynamicRotation = Quaternion.AngleAxis(currentAngle, xAxis);

        door.rotation = dynamicRotation * currentBasisRotation * initialLocalRotation;
    }
    
    public void SetOpen(bool isOpen)
    {
        switch (_state.Value)
        {
            case StateEnum.Closed:
                if(isOpen)
                    _state.SetForce(StateEnum.Opening, animationDuration);
                break;
            case StateEnum.Opened:
                if(!isOpen)
                    _state.SetForce(StateEnum.Closing, animationDuration);
                break;
            case StateEnum.Opening:
                if(!isOpen)
                    _state.SetForce(StateEnum.Closing, animationDuration, 1f - _state.TimeFraction);
                break;
            case StateEnum.Closing:
                if(isOpen)
                    _state.SetForce(StateEnum.Opening, animationDuration, 1f - _state.TimeFraction);
                break;
        }
    }
}

