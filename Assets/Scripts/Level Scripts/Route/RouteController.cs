using System;
using UnityEngine;
using System.Collections.Generic;
using CustomAttributes;
using Unity.VisualScripting;

public class RouteController : MonoBehaviour
{
    [SerializeField] private Transform body;
    [SerializeField] private float speed = 1f;
    [SerializeField] private float rotationSpeed = 1000f;

    [Header("Additional anchors")] 
    [SerializeField] private List<Transform> anchorPointsBefore = new();
    [SerializeField] private List<Transform> anchorPointsAfter = new();
    
    [Header("Flags")]
    [SerializeField] private bool isLoop;
    [SerializeField] private bool prewarm = true;
    
    [Header("Spawn settings")] 
    [SerializeField] private int amountOfObjects = 1;
    [SerializeField] private bool placeObjectsUniformly = true;
    [SerializeField, EnableIf("!placeObjectsUniformly")] 
    private float timeBetweenSpawns=1f;
    [SerializeField] private float delayFirstSpawnTime = 0f;
    
    
    private List<Transform> _points = new();
    private List<float> _times = new();

    public float Time(int index) => _times[index];
    public Transform Point(int index) => _points[index];
    public bool NextIndex(int index, out int nextIndex)
    {
        if (index != _points.Count - 1 || isLoop)
        {
            nextIndex = (index + 1) % _points.Count;
            return true;
        }

        nextIndex = _points.Count;
        return false;
    }

    private readonly UtilityTimers.TemporaryValue<bool> _canSpawnNext = new(true, false);
    
    private AutoPool<RouteObjectController> _pool;
    
    private void Awake()
    {
        GatherPoints();
        
        if (_points.Count < 2) 
        {
            Debug.LogWarning("Route requires at least 2 points!", this);
            enabled = false;
            return;
        }
        
        
        var total = SetTimes();
        
        if(placeObjectsUniformly && amountOfObjects > 0)
            timeBetweenSpawns = total/amountOfObjects;
        
        var controller = body.AddComponent<RouteObjectController>();
        controller.SetRouteController(this);
        
        _pool = new AutoPool<RouteObjectController>(controller, amountOfObjects, amountOfObjects*2, transform);
        
        body.gameObject.SetActive(false);
    }

    private void Start()
    {
        Restart();
    }

    public void Restart()
    {
        _pool.ReleaseAll();
        if (prewarm)
        {
            Prewarm(delayFirstSpawnTime);
        }
        else
        {
            _canSpawnNext.Activate(delayFirstSpawnTime);
        }
    }
    
    private void Update()
    {
        if (_canSpawnNext && _pool.ActiveItemsCount < amountOfObjects)
        {
            _canSpawnNext.Activate(timeBetweenSpawns);
            var obj = _pool.Get();
            obj.Initialize();
        }
    }
    
    
    private void Prewarm(float delay)
    {
        while(delay >= timeBetweenSpawns)
            delay -= timeBetweenSpawns;
        
        
        var time = timeBetweenSpawns - delay;
        var timeIndex = 0;
        
        while (timeIndex < _times.Count && time >= _times[timeIndex])
        {
            time -= _times[timeIndex];
            ++timeIndex;
        }
 
        while (timeIndex < _times.Count && _pool.ActiveItemsCount < amountOfObjects) {
            var fraction = time/_times[timeIndex];
            var obj = _pool.Get();
            obj.Initialize(timeIndex, fraction);
            time += timeBetweenSpawns;
            while (timeIndex < _times.Count && time >= _times[timeIndex])
            {
                time -= _times[timeIndex];
                ++timeIndex;
            }
            
        }
        
        _canSpawnNext.Activate(delay);
    }
    
    private void GatherPoints()
    {
        _points = new();
        foreach (var point in anchorPointsBefore)
        {
            if(point!=null)
                _points.Add(point);
        }
        foreach (Transform child in transform)
        {
            if (child != body)
            {
                _points.Add(child);
            }
        }
        foreach (var point in anchorPointsAfter)
        {
            if(point!=null)
                _points.Add(point);
        }
    }

    private float SetTimes()
    {
        var total = 0f;
        for (var i = 0; i < _points.Count - 1; i++)
        {
            total += AddTime(i, i+1);
        }
        if (isLoop)
        {
            total += AddTime(_points.Count - 1, 0);
        }
        return total;
    }

    private float AddTime(int i1, int i2)
    {
        var distance = Vector3.Distance(_points[i1].position, _points[i2].position);
        var angle = Quaternion.Angle(_points[i1].rotation, _points[i2].rotation);
        var time = Mathf.Max(distance / speed, angle / rotationSpeed);
        _times.Add(time);
        return time;
    }
    
    private void OnDrawGizmos()
    {
        if (Application.isPlaying) return;

        GatherPoints();
        if (_points.Count < 2) return;

        Gizmos.color = Color.green;
        var radius = 0.1f;
        var count = isLoop ? _points.Count : _points.Count - 1;

        for (var i = 0; i < count; i++)
        {
            var a = _points[i].position;
            var b = _points[(i + 1) % _points.Count].position;
            
            var center = (a + b) / 2f;
            var dir = (b - a).normalized;
            var dist = Vector3.Distance(a, b);

            if (dir != Vector3.zero)
            {
                
                Gizmos.matrix = Matrix4x4.TRS(center, Quaternion.LookRotation(dir), Vector3.one);
                
                
                Gizmos.DrawWireCube(Vector3.zero, new Vector3(radius, radius, dist));
            }
        }
        Gizmos.matrix = Matrix4x4.identity;
    }
}
