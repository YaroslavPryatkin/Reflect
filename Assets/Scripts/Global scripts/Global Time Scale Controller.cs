using UnityEngine;
using System.Collections.Generic;

public static class GlobalTimeScaleController
{
    private static readonly Dictionary<object, float> TimeScales = new ();
    private static float _baseFixedDeltaTime = -1f; 

    public static void ChangeTimePace(object caller, float scale)
    {
        InitializeIfNeeded();
        
        TimeScales[caller] = scale;
        
        RecalculateAndApply();
    }

    public static void ReturnTimePace(object caller)
    {
        if (TimeScales.Remove(caller))
        {
            RecalculateAndApply();
        }
    }

    private static void RecalculateAndApply()
    {
        var totalScale = 1f;
        
        foreach (var scale in TimeScales.Values)
        {
            totalScale *= scale;
        }

        totalScale = Mathf.Max(0f, totalScale);

        Time.timeScale = totalScale;
        
        Time.fixedDeltaTime = _baseFixedDeltaTime * totalScale;
    }

    private static void InitializeIfNeeded()
    {
        if (_baseFixedDeltaTime < 0f)
        {
            _baseFixedDeltaTime = Time.fixedDeltaTime;
        }
    }

}
