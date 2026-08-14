using UnityEngine;

public class ChainSegmentController : MonoBehaviour
{
    private bool _hasForward = false;
    private Transform _forward;
    private bool _hasBackward = false;
    private Transform _backward;
    
    public void SetForward(ChainSegmentController forward)
    {
        _forward = forward.transform;
        _hasForward = true;
    }

    public void SetBackward(ChainSegmentController backward)
    {
        _backward = backward.transform;
        _hasBackward = true;
    }

    public bool HasNext(out Transform next, float direction)
    {
        if (direction >= 0)
        {
            next = _hasForward ? _forward : null;
            return _hasForward;
        }
        else
        {
            next = _hasBackward ? _backward : null;
            return _hasBackward;
        }
    }
    public bool HasNext(float direction)
    {
        return direction >= 0 ?  _hasForward : _hasBackward;
    }
}
