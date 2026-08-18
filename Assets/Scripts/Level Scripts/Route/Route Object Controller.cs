using System;
using UnityEngine;

public class RouteObjectController : Poolable<RouteObjectController>
{
    [SerializeField, HideInInspector] private RouteController _route;
    public void SetRouteController(RouteController routeController)
    {
        _route = routeController;
    }

    private Transform _previousTransform;
    private Transform _nextTransform;
    private readonly UtilityTimers.FractionBlockingValueTimer<int> _currentPoint = 0;

    private void Update()
    {
        if (_currentPoint.CanBeChanged)
        {
            var prevIndex = _currentPoint.Value;
            if (_route.NextIndex(prevIndex, out var nextIndex))
            {
                _currentPoint.SetForce(nextIndex, _route.Time(prevIndex));
                _previousTransform = _route.Point(prevIndex);
                _nextTransform = _route.Point(nextIndex);
            }
            else
            {
                ReturnToPool();
            }
        }


        SetTransform();
    }

    private void SetTransform()
    {
        var fraction = Mathf.Clamp01(_currentPoint.TimeFraction);
        transform.SetPositionAndRotation(
            Vector3.Lerp(_previousTransform.position, _nextTransform.position, fraction),
            Quaternion.Lerp(_previousTransform.rotation, _nextTransform.rotation, fraction));
    }

    public void Initialize(int startIndex=0, float fraction=0f)
    {
        _previousTransform = null;
        _nextTransform = null;
        if (_route.NextIndex(startIndex, out var nextIndex))
        {
            _currentPoint.SetForce(nextIndex, _route.Time(startIndex), fraction);
            _previousTransform = _route.Point(startIndex);
            _nextTransform = _route.Point(nextIndex);
        }
        else
        {
            ReturnToPool();
        }
    }

    protected override void OnReturnToPool()
    {
        
    }
}
